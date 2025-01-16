using OpenEmail.Domain.Models.Cryptography;
using OpenEmail.Domain.Models.Messages;

namespace OpenEmail.Contracts.Clients
{
    /// <summary>
    /// Client factory for Refit.
    /// </summary>
    public interface IClientFactory
    {
        /// <summary>
        /// Creates a client for the given base URL without any nonce signature.
        /// </summary>
        /// <typeparam name="TClient">Client type.</typeparam>
        /// <param name="baseUrl">Base url</param>
        /// <returns>API client.</returns>
        TClient CreateClient<TClient>(string baseUrl);
        TClient CreateClient<TClient>(string baseUrl, Nonce nonce);

        /// <summary>
        /// Creates a client for the given base URL with the active profile nonce signature.
        /// Throws if no active profile is set.
        /// </summary>
        /// <typeparam name="TClient">Client type.</typeparam>
        /// <returns>API client with each request to be signed with the active user profile.</returns>
        TClient CreateProfileClient<TClient>();


        /// <summary>
        /// Creates a new client for attachment downloading with progress support.
        /// Downloaded stream will be updated in the given model.
        /// </summary>
        TClient CreateProfileClientWithProgress<TClient>(AttachmentProgress attachmentProgress);
    }
}
