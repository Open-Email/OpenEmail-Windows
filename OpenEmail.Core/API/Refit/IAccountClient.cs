using OpenEmail.Domain.Models.Cryptography;
using Refit;

namespace OpenEmail.Core.API.Refit
{
    public interface IAccountClient
    {
        [Post("/account/{hostPart}/{localPart}")]
        [Headers($"Authorization: {CryptoConstants.NONCE_SCHEME}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> PostCreateAccountAsync(string hostPart, string localPart, [Body] string postData);
    }
}
