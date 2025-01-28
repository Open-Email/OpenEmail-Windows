using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Cryptography;
using Refit;

namespace OpenEmail.Core.API.Refit
{
    [Headers($"Authorization: {CryptoConstants.NONCE_SCHEME} ")]

    public interface ILoginClient
    {
        [Head("/home/{address}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> TryAuthenticationAsync(UserAddress address);

        [Head("/account/{address}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> IsUsernameAvailableAsync(UserAddress address);
    }
}
