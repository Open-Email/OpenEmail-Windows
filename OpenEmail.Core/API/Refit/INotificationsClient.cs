using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Cryptography;
using Refit;

namespace OpenEmail.Core.API.Refit
{
    [Headers($"Authorization: {CryptoConstants.NONCE_SCHEME} ")]
    public interface INotificationsClient
    {
        // TODO: Maybe custom serializer can be used to parse response here.
        /// <summary>
        /// Private Messages API to retrieve notifications.
        /// </summary>
        /// <param name="address">Address to get notifications for.</param>
        [Get("/home/{address}/notifications")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> GetNotificationsAsync(UserAddress address);
    }
}
