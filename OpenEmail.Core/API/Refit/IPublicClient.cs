using OpenEmail.Domain.Models.Accounts;
using Refit;

namespace OpenEmail.Core.API.Refit
{
    public interface IPublicClient
    {
        [Get("/mail/{address}/profile")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> GetProfileAsync(UserAddress address, CancellationToken cancellationToken = default);

        [Get("/mail/{address}/image")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> GetProfileImageAsync(UserAddress address, CancellationToken cancellationToken = default);
    }
}
