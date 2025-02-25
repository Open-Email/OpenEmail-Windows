using System.Text.RegularExpressions;
using OpenEmail.Contracts.Clients;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Models.Discovery;

namespace OpenEmail.Core.Services
{
    public class DiscoveryService : IDiscoveryService
    {
        private const int MaxMailAgentsConsidered = 3;
        private const string DefaultMailSubdomain = "mail";
        private const string WELL_KNOWN_PATH = "/.well-known/mail.txt";

        private List<DiscoveryHost> cachedHosts = new List<DiscoveryHost>();

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
            var validAgents = await DiscoverMailAgentsAsync(hostPart);
            return validAgents.Select(agent => new DiscoveryHost(agent, hostPart)).ToList();
        }

        public async Task<List<string>> DiscoverMailAgentsAsync(string domain)
        {
            var validAgents = new List<string>();

            // Try well-known file on main domain first
            var mainDomainAgents = await GetWellKnownAgentsAsync($"https://{domain}{WELL_KNOWN_PATH}");
            if (mainDomainAgents.Any())
            {
                var validMainAgents = await ValidateAgentsAsync(mainDomainAgents, domain);
                validAgents.AddRange(validMainAgents);
            }

            // Try well-known file on mail subdomain
            if (!validAgents.Any())
            {
                var mailDomainAgents = await GetWellKnownAgentsAsync($"https://mail.{domain}{WELL_KNOWN_PATH}");
                if (mailDomainAgents.Any())
                {
                    var validMailAgents = await ValidateAgentsAsync(mailDomainAgents, domain);
                    validAgents.AddRange(validMailAgents);
                }
            }

            // Fall back to default mail subdomain if no agents found
            if (!validAgents.Any())
            {
                var defaultMailAgent = $"mail.{domain}";
                if (await ValidateMailAgentAsync(defaultMailAgent, domain))
                {
                    validAgents.Add(defaultMailAgent);
                }
            }

            // Take max 3 valid agents per spec
            return validAgents.Take(3).ToList();
        }

        private async Task<List<string>> GetWellKnownAgentsAsync(string wellKnownUrl)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetStringAsync(wellKnownUrl);

                return response
                    .Split('\n')
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                    .Where(IsValidHostname)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private async Task<List<string>> ValidateAgentsAsync(List<string> agents, string domain)
        {
            var validAgents = new List<string>();
            foreach (var agent in agents)
            {
                if (await ValidateMailAgentAsync(agent, domain))
                {
                    validAgents.Add(agent);
                }
            }
            return validAgents;
        }

        private async Task<bool> ValidateMailAgentAsync(string agentHostname, string domain)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.SendAsync(new HttpRequestMessage(
                    HttpMethod.Head,
                    $"https://{agentHostname}/mail/{domain}"
                ));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidHostname(string hostname)
        {
            // Basic hostname validation
            return !string.IsNullOrEmpty(hostname) &&
                   hostname.Length <= 255 &&
                   Uri.CheckHostName(hostname) != UriHostNameType.Unknown;
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
