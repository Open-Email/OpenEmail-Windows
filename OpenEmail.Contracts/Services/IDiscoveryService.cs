using OpenEmail.Domain.Models.Discovery;

namespace OpenEmail.Contracts.Services
{
    public interface IDiscoveryService
    {
        /// <summary>
        /// Get the discovery host for the given host part.
        /// If the host is not cached, it will be loaded from the discovery service and cached.
        /// </summary>
        /// <param name="hostPart">Host part of the address.</param>
        /// <returns>First host that matches the host part.</returns>
        Task<DiscoveryHost> GetDiscoveryHostAsync(string hostPart);
    }
}
