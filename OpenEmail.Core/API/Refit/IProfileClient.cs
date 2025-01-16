using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Cryptography;
using Refit;

namespace OpenEmail.Core.API.Refit
{
    [Headers($"Authorization: {CryptoConstants.NONCE_SCHEME} ")]
    public interface IProfileClient
    {
        [Put("/home/{address}/profile")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> UpdateProfileDataAsync(UserAddress address, [Body] string serializedProfileData, CancellationToken cancellationToken = default);

        [Put("/home/{address}/image")]
        [QueryUriFormat(UriFormat.Unescaped)]
        [Headers("Content-Type: application/octet-stream")]
        Task<HttpResponseMessage> UpdateProfileImageAsync(UserAddress address, [Body] ByteArrayContent imageData, CancellationToken cancellationToken = default);

        [Delete("/home/{address}/image")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> DeleteProfileImageAsync(UserAddress address);
    }
}
