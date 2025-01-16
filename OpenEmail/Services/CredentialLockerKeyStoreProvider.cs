using System;
using System.Threading;
using System.Threading.Tasks;
using OpenEmail.Contracts.Application;
using OpenEmail.Domain.Models.Accounts;
using Windows.Security.Credentials;

namespace OpenEmail.Services
{
    /// <summary>
    /// Key store service that uses Credential Locker API for packaged Windows apps.
    /// There are no other providers at the moment, but later can be implemented via this interface.
    /// </summary>
    public class CredentialLockerKeyStoreProvider : IKeyStoreService
    {
        private readonly PasswordVault _passwordVault = new();

        private const string EncryptionKeyStoreIdentifier = "OpenEmailEncryption";
        private const string SigningKeyStoreIdentifier = "OpenEmailSigning";

        public Task StoreAccountKeysAsync(LocalUser localUser, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(localUser, nameof(localUser));

            var address = localUser.Address;
            var privateEncryptionKeyBase64 = localUser.PrivateEncryptionKeyBase64;
            var privateSigningKeyBase64 = localUser.PrivateSigningKeyBase64;

            _passwordVault.Add(new PasswordCredential(EncryptionKeyStoreIdentifier, address.FullAddress, privateEncryptionKeyBase64));
            _passwordVault.Add(new PasswordCredential(SigningKeyStoreIdentifier, address.FullAddress, privateSigningKeyBase64));

            return Task.CompletedTask;
        }

        public Task<string> RetrieveAccountEncryptionPrivateKeyBase64(UserAddress address, CancellationToken cancellationToken = default)
        {
            var credential = _passwordVault.Retrieve(EncryptionKeyStoreIdentifier, address.FullAddress);
            return Task.FromResult(credential.Password);
        }

        public Task<string> RetrieveAccountSigningPrivateKeyBase64(UserAddress address, CancellationToken cancellationToken = default)
        {
            var credential = _passwordVault.Retrieve(SigningKeyStoreIdentifier, address.FullAddress);
            return Task.FromResult(credential.Password);
        }

        public Task DeleteAccountKeysAsync(UserAddress address, CancellationToken cancellationToken = default)
        {
            var encryptionKey = _passwordVault.Retrieve(EncryptionKeyStoreIdentifier, address.FullAddress);
            var signingKey = _passwordVault.Retrieve(SigningKeyStoreIdentifier, address.FullAddress);

            _passwordVault.Remove(encryptionKey);
            _passwordVault.Remove(signingKey);

            return Task.CompletedTask;
        }
    }
}
