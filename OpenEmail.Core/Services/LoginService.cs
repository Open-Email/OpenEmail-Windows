using OpenEmail.Contracts.Clients;
using OpenEmail.Contracts.Services;
using OpenEmail.Core.API.Refit;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Cryptography;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Core.Services
{
    public class LoginService : ILoginService
    {
        private readonly IPublicClientService _publicClientService;
        private readonly IDiscoveryService _discoveryService;
        private readonly IProfileDataManager _profileDataManager;
        private readonly IAccountService _accountService;
        private readonly IClientFactory _clientFactory;

        public LoginService(IPublicClientService publicClientService,
                            IDiscoveryService discoveryService,
                            IProfileDataManager profileDataManager,
                            IAccountService accountService,
                            IClientFactory clientFactory)
        {
            _publicClientService = publicClientService;
            _discoveryService = discoveryService;
            _profileDataManager = profileDataManager;
            _accountService = accountService;
            _clientFactory = clientFactory;
        }

        // TODO: Multi-host support.
        public async Task<Account> AuthenticateAsync(UserAddress address, string privateEncryptionKey, string privateSigningKey)
        {
            // Public profile must be available.
            var profileData = await _publicClientService.FetchProfileDataAsync(address).ConfigureAwait(false);

            // Get the host.
            var host = await _discoveryService.GetDiscoveryHostAsync(address.HostPart);

            // Create nonce and try the authentication.
            var publicEncryptionKey = Convert.FromBase64String(profileData.EncryptionKey);
            var publicSigningKey = Convert.FromBase64String(profileData.SigningKey);

            var localUser = new LocalUser(profileData.Name, address, privateEncryptionKey, profileData.EncryptionKey, profileData.EncryptionKeyId, privateSigningKey, profileData.SigningKey);

            var nonce = new Nonce(localUser, host);

            // Create the login client with the nonce.
            var loginClient = _clientFactory.CreateClient<ILoginClient>($"https://{host.AgentUrl}", nonce);

            // Try the authentication.
            var response = await loginClient.TryAuthenticationAsync(address).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                throw new Exception("Authentication failed.");

            response.EnsureSuccessStatusCode();

            // Success. Save profile data and create account.
            var profileImage = await _publicClientService.FetchProfileImageDataAsync(address).ConfigureAwait(false);

            await _profileDataManager.SaveProfileDataAsync(profileData, address).ConfigureAwait(false);
            await _profileDataManager.SaveProfileImageAsync(profileImage, address).ConfigureAwait(false);

            return await _accountService.SaveAccountAsync(localUser, host).ConfigureAwait(false);
        }

        public async Task LogoutAsync(AccountProfile profile)
        {
            // Remove account data.
            await _accountService.DeleteAccountAsync(profile.Account.Id);
        }
    }
}
