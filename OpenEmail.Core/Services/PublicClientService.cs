using OpenEmail.Contracts.Clients;
using OpenEmail.Contracts.Services;
using OpenEmail.Core.API.Refit;
using OpenEmail.Domain.Exceptions;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Core.Services
{
    public class PublicClientService : IPublicClientService
    {
        private readonly IClientFactory _clientFactory;
        private readonly IDiscoveryService _discoveryService;

        public PublicClientService(IClientFactory clientFactory, IDiscoveryService discoveryService)
        {
            _clientFactory = clientFactory;
            _discoveryService = discoveryService;
        }

        public async Task<ProfileData> FetchProfileDataAsync(UserAddress address, CancellationToken cancellationToken = default)
        {
            var host = await _discoveryService.GetDiscoveryHostAsync(address.HostPart) ?? throw new ServiceException("Host not found.");

            var client = _clientFactory.CreateClient<IPublicClient>($"https://{host.AgentUrl}");

            var dataResponse = await client.GetProfileAsync(address, cancellationToken);

            if (dataResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new ServiceException("Profile not found.");

            try
            {
                var content = await dataResponse.Content.ReadAsStringAsync();

                return new ProfileData(content);
            }
            catch (Exception ex)
            {
                throw new ServiceException(ex.Message);
            }
        }

        public async Task<byte[]> FetchProfileImageDataAsync(UserAddress address, CancellationToken cancellationToken = default)
        {
            var host = await _discoveryService.GetDiscoveryHostAsync(address.HostPart);
            var client = _clientFactory.CreateClient<IPublicClient>($"https://{host.AgentUrl}");

            var dataResponse = await client.GetProfileImageAsync(address, cancellationToken);

            // If the image is not found, return an empty array.
            if (!dataResponse.IsSuccessStatusCode) return [];

            return await dataResponse.Content.ReadAsByteArrayAsync();

        }
    }
}
