using Refit;

namespace OpenEmail.Core.Services.Refit
{
    public interface IDiscoveryClient
    {
        [Get("/.well-known/mail.txt")]
        Task<string> GetMailAgentsAsync();
    }
}
