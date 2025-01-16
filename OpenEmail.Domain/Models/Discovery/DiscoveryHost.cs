namespace OpenEmail.Domain.Models.Discovery
{
    /// <summary>
    /// Host that is discovered by the discovery service.
    /// Retrieved from well known URIs.
    /// </summary>
    /// <param name="AgentUrl">Agent url eg. mail.ping.works</param>
    /// <param name="HostPart">Host part of the mail address eg. ping.works</param>
    public record DiscoveryHost(string AgentUrl, string HostPart)
    {
        private const int ExpirationInSeconds = 3600; // 1 hour.
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public bool IsExpired() => DateTime.UtcNow > CreatedAt.AddSeconds(ExpirationInSeconds);
    }
}
