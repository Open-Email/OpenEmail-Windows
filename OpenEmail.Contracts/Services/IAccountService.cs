using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Discovery;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Contracts.Services
{
    /// <summary>
    /// Service that handles all account related operations.
    /// </summary>
    public interface IAccountService
    {

        Task<Account> CreateAccountAsync(string localPart, string fullName, DiscoveryHost host, CancellationToken cancellationToken = default);

        Task<Account> SaveAccountAsync(LocalUser localUser, DiscoveryHost host, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the available domains for the application.
        /// </summary>
        public List<string> GetAvailableDomains();

        /// <summary>
        /// Gets the excluded names for the application.
        /// </summary>
        public List<string> GetExcludedNames();

        /// <summary>
        /// Loads profile information for the given account.
        /// </summary>
        /// <param name="accountId">Account id</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Profile model with private encryption key for the account.</returns>
        Task<AccountProfile> GetAccountProfileAsync(Guid accountId, CancellationToken cancellationToken = default);

        /// <summary>
        /// TEST: Returns the latest created account to start with for now.
        /// </summary>
        /// <returns></returns>
        Task<AccountProfile> GetStartAccountProfileAsync();
        Task DeleteAccountAsync(Guid accountId);
    }
}
