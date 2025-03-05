using System.Diagnostics;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Clients;
using OpenEmail.Contracts.DataServices;
using OpenEmail.Contracts.Services;
using OpenEmail.Core.API.Refit;
using OpenEmail.Domain;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Exceptions;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Cryptography;
using OpenEmail.Domain.Models.Discovery;
using OpenEmail.Domain.Models.Profile;
using SQLite;

namespace OpenEmail.Core.DataServices
{
    public class AccountService : BaseDataService, IAccountService
    {
        private const int MinimumNameLength = 6;
        private readonly static string[] InitialContactAddresses = ["support@ping.works"];

        public List<string> GetAvailableDomains() => ["open.email"];
        public List<string> GetExcludedNames() => ["aaa", "xxx"];


        private readonly IClientFactory _clientFactory;
        private readonly IKeyStoreService _keyStoreService;
        private readonly IDiscoveryService _discoveryService;
        private readonly IProfileDataManager _profileDataManager;

        public AccountService(IClientFactory clientFactory,
                              IKeyStoreService keyStoreService,
                              IDiscoveryService discoveryService,
                              IProfileDataManager profileDataManager,
                              IDatabaseService<ISQLiteAsyncConnection> databaseService) : base(databaseService)
        {
            _clientFactory = clientFactory;
            _keyStoreService = keyStoreService;
            _discoveryService = discoveryService;
            _profileDataManager = profileDataManager;
        }

        public async Task DeleteAccountAsync(Guid accountId)
        {
            var account = await Connection.FindAsync<Account>(a => a.Id == accountId)
                ?? throw new AccountLoadException("Account not found.");

            // Delete database.
            await DisposeConnectionAsync();

            // Delete private keys
            var userAddress = new UserAddress(account.LocalPart, account.HostPart);
            await _keyStoreService.DeleteAccountKeysAsync(userAddress).ConfigureAwait(false);
        }

        public async Task<Account> CreateAccountAsync(string localPart, string fullName, DiscoveryHost host, CancellationToken cancellationToken = default)
        {
            var address = new UserAddress(localPart, host.HostPart);

            var encryptionKeys = CryptoUtils.GenerateEncryptionKeys();
            var signingKeys = CryptoUtils.GenerateSigningKeys();

            var localUser = new LocalUser(fullName, address, encryptionKeys.privateKey, encryptionKeys.publicKey, encryptionKeys.keyId, signingKeys.privateKey, signingKeys.publicKey);

            // https://github.com/dejanstrbac/Email2/blob/main/Email2Core/Sources/Email2Core/Client.swift#L273
            // Get auth nonce with account and host.

            var nonce = new Nonce(localUser, host);
            nonce.GetSignAuth();

            // Send data
            // https://{nonce.DiscoveryHost.AgentUrl}
            var accountClient = _clientFactory.CreateClient<IAccountClient>($"https://{nonce.DiscoveryHost.AgentUrl}", nonce);

            var dateString = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);

            // Prepare post data.
            string postData = @$"
            Name: {fullName}
            Public-Access: Yes
            Last-Seen-Public: Yes
            Encryption-Key: id=1; algorithm={CryptoConstants.ANONYMOUS_ENCRYPTION_CIPHER}; value={localUser.PublicEncryptionKeyBase64}
            Signing-Key: algorithm={CryptoConstants.SIGNING_ALGORITHM}; value={localUser.PublicSigningKeyBase64}    
            Updated: {dateString}";

            var response = await accountClient.PostCreateAccountAsync(host.HostPart, localPart, postData).ConfigureAwait(false);

            // 409 existing account.
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                throw new AccountCreationFailedException($"Account with address {address.FullAddress} already exists.");

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"Created new account {localPart}@{host.HostPart}");

                // Print keys

                Debug.WriteLine($"Encryption key: {localUser.PrivateEncryptionKeyBase64}");
                Debug.WriteLine($"Signing key: {localUser.PrivateSigningKeyBase64}");
                Debug.WriteLine("Encryption key id: " + localUser.PublicEncryptionKeyId);

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new AccountCreationFailedException($"({response.StatusCode}) Account provisioning failed.\n{errorContent}");
                }

                throw new AccountCreationFailedException($"({response.StatusCode}) Account provisioning failed.");
            }

            return await SaveAccountAsync(localUser, host, cancellationToken).ConfigureAwait(false);
        }

        public async Task<AccountProfile> GetAccountProfileAsync(Guid accountId, CancellationToken cancellationToken = default)
        {
            var accountModel = await Connection.FindAsync<Account>(a => a.Id == accountId)
                ?? throw new AccountLoadException("Account not found.");

            var userAddress = new UserAddress(accountModel.LocalPart, accountModel.HostPart);

            var encryptionPrivateKeyBase64 = await _keyStoreService.RetrieveAccountEncryptionPrivateKeyBase64(userAddress, cancellationToken).ConfigureAwait(false)
                ?? throw new AccountLoadException("Account encryption private key found.");

            var signingPrivateKeyBase64 = await _keyStoreService.RetrieveAccountSigningPrivateKeyBase64(userAddress, cancellationToken).ConfigureAwait(false)
                ?? throw new AccountLoadException("Account signing private key found.");

            var host = await _discoveryService.GetDiscoveryHostAsync(accountModel.HostPart).ConfigureAwait(false)
                ?? throw new AccountLoadException("Host not found.");

            var profileData = await _profileDataManager.GetProfileDataAsync(userAddress, cancellationToken).ConfigureAwait(false)
                ?? throw new AccountLoadException("Profile data not found.");

            return new AccountProfile(accountModel,
                                      profileData,
                                      CryptoUtils.Base64Decode(encryptionPrivateKeyBase64),
                                      CryptoUtils.Base64Decode(signingPrivateKeyBase64),
                                      host);
        }

        public async Task<AccountProfile> GetStartAccountProfileAsync()
        {
            var account = await Connection.Table<Account>().ToListAsync();

            if (account.Count == 0) return null;
            return await GetAccountProfileAsync(account.Last().Id);
        }

        public async Task<Account> SaveAccountAsync(LocalUser localUser, DiscoveryHost host, CancellationToken cancellationToken = default)
        {
            var localAccount = new Account()
            {
                DisplayName = localUser.Name,
                HostPart = host.HostPart,
                LocalPart = localUser.Address.LocalPart,
                Id = Guid.NewGuid(),
                PublicEncryptionKey = localUser.PublicEncryptionKeyBase64,
                PublicSigningKey = localUser.PublicSigningKeyBase64,
                PublicEncryptionKeyId = localUser.PublicEncryptionKeyId
            };

            try
            {
                // Save user to database.
                await Connection.InsertAsync(localAccount).ConfigureAwait(false);

                // Store keys in the vault.
                await _keyStoreService.StoreAccountKeysAsync(localUser, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await DeleteAccountAsync(localAccount.Id).ConfigureAwait(false);

                // TODO: Logging
                throw;
            }

            return localAccount;
        }
    }
}
