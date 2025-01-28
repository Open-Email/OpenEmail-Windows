using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using OpenEmail.Contracts.Clients;
using OpenEmail.Contracts.Services;
using OpenEmail.Core.Services.Refit;
using OpenEmail.Domain.Models.Discovery;
using Refit;

namespace OpenEmail.Core.Services
{
    public class DiscoveryService : IDiscoveryService
    {
        private const int MaxMailAgentsConsidered = 3;
        private const string DefaultMailSubdomain = "mail";

        private List<DiscoveryHost> cachedHosts = [];

        private readonly IClientFactory _clientFactory;

        public DiscoveryService(IClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<DiscoveryHost> GetDiscoveryHostAsync(string hostPart)
        {
            // Check if the host is cached before and valid.
            var cachedHost = cachedHosts.Find(a => a != null && a.AgentUrl == hostPart && !a.IsExpired());

            if (cachedHost != null) return cachedHost;

            // There are no local hosts, load from the discovery service.
            var hosts = await LoadDiscoveryHostsAsync(hostPart).ConfigureAwait(false);

            if (hosts.Count != 0) cachedHosts.AddRange(hosts);

            return hosts.FirstOrDefault(a => a.HostPart == hostPart);
        }

        public async Task<List<DiscoveryHost>> LoadDiscoveryHostsAsync(string hostPart)
        {
            // 1. Try out the host part directly.
            var discoveryClient = _clientFactory.CreateClient<IDiscoveryClient>($"https://{hostPart}");

            string discoveryContent = string.Empty;

            bool shouldAttemptDefaultMailSubdomain = false;

            try
            {
                discoveryContent = await discoveryClient.GetMailAgentsAsync().ConfigureAwait(false);
            }
            catch (ApiException apiException) when (apiException.StatusCode == HttpStatusCode.NotFound)
            {
                shouldAttemptDefaultMailSubdomain = true;
            }

            if (shouldAttemptDefaultMailSubdomain)
            {
                Debug.WriteLine($"Failed to load mail agents for {hostPart}. Trying out the default mail subdomain.");

                // In case of failure, try out the default mail subdomain.
                discoveryClient = _clientFactory.CreateClient<IDiscoveryClient>($"https://{DefaultMailSubdomain}.{hostPart}");
                discoveryContent = await discoveryClient.GetMailAgentsAsync().ConfigureAwait(false);
            }

            // Parse hosts from the content.
            return ParseHostContent(discoveryContent).Select(hostAddress => new DiscoveryHost(hostAddress, hostPart)).ToList();
        }

        public List<string> ParseHostContent(string content)
        {
            var hosts = new List<string>();

            var splittedContent = content.Split(Environment.NewLine);

            foreach (var line in splittedContent)
            {
                // Don't consider more than max mail agents.
                if (hosts.Count >= MaxMailAgentsConsidered) break;

                // Ignore empty or whitespace lines.
                if (string.IsNullOrWhiteSpace(line) || string.IsNullOrEmpty(line)) continue;

                // Ignore comments.
                if (line.StartsWith("#")) continue;

                // Ignore invalid hosts.
                if (!IsValidHost(line)) continue;

                hosts.Add(line.Trim());
            }

            // Distinct and return the list.
            return hosts.Distinct().ToList();
        }

        public bool IsValidHost(string host)
        {
            if (string.IsNullOrEmpty(host)) return false;

            string pattern = @"^(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}$";

            return Regex.IsMatch(host, pattern);
        }
    }
}
