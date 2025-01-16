using System.Collections.Concurrent;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Clients;
using OpenEmail.Core.API;
using OpenEmail.Domain.Models.Cryptography;
using OpenEmail.Domain.Models.Messages;
using Refit;

namespace OpenEmail.Core.Services
{
    public class ClientFactory : IClientFactory
    {
        // Clients without authorization getter are cached based on base url.
        private readonly ConcurrentDictionary<string, object> clientCache = new();
        private readonly IApplicationStateService _applicationStateService;

        public ClientFactory(IApplicationStateService applicationStateService)
        {
            _applicationStateService = applicationStateService;
        }

        public TClient CreateClient<TClient>(string baseUrl)
        {
            if (clientCache.ContainsKey(baseUrl) && clientCache[baseUrl].GetType() == typeof(TClient)) return (TClient)clientCache[baseUrl];

            var client = RestService.For<TClient>(new HttpClient()
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            });

            clientCache.TryAdd(baseUrl, client);

            return client;
        }

        public TClient CreateClient<TClient>(string baseUrl, Nonce nonce)
        {
            if (clientCache.ContainsKey(baseUrl) && clientCache[baseUrl].GetType() == typeof(TClient)) return (TClient)clientCache[baseUrl];

            var service = RestService.For<TClient>(baseUrl, new RefitSettings()
            {
                AuthorizationHeaderValueGetter = (a, b) =>
                {
                    return Task.FromResult(nonce.GetSignAuth());
                },
            });

            return service;
        }

        public TClient CreateProfileClient<TClient>()
        {
            var service = RestService.For<TClient>($"https://{_applicationStateService.ActiveProfile.Nonce.DiscoveryHost.AgentUrl}", new RefitSettings()
            {
                AuthorizationHeaderValueGetter = (a, b) =>
                {
                    return Task.FromResult(_applicationStateService.ActiveProfile.Nonce.GetSignAuth());
                },

            });

            return service;
        }

        public TClient CreateProfileClientWithProgress<TClient>(AttachmentProgress attachmentProgress)
        {
            var handler = new ProgressHandler(attachmentProgress, _applicationStateService.ActiveProfile.Nonce.GetSignAuth);

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri($"https://{_applicationStateService.ActiveProfile.Nonce.DiscoveryHost.AgentUrl}")
            };

            return RestService.For<TClient>(httpClient);
        }
    }
}
