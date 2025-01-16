using OpenEmail.Domain.Models.Accounts;

namespace OpenEmail.Contracts.Application
{
    /// <summary>
    /// An interface to store and retrieve account private keys.
    /// </summary>
    public interface IKeyStoreService
    {
        /// <summary>
        /// Stores created account's private key in a secure manner.
        /// </summary>
        /// <param name="localUser">Local user to be saved keys for.</param>
        Task StoreAccountKeysAsync(LocalUser localUser, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes saved account keys.
        /// </summary>
        /// <param name="address">Address of the account</param>
        /// <returns></returns>
        Task DeleteAccountKeysAsync(UserAddress address, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves private key for given account address.
        /// </summary>
        /// <param name="address">Acc address.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Account private key.</returns>
        Task<string> RetrieveAccountEncryptionPrivateKeyBase64(UserAddress address, CancellationToken cancellationToken = default);
        Task<string> RetrieveAccountSigningPrivateKeyBase64(UserAddress address, CancellationToken cancellationToken = default);
    }
}
