using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Cryptography;
using Refit;

namespace OpenEmail.Core.API.Refit
{
    [Headers($"Authorization: {CryptoConstants.NONCE_SCHEME} ")]
    public interface ILinksClient
    {
        /// <summary>
        /// Private Messages API to retrieve address links.
        /// </summary>
        /// <param name="address">Address to get messages for.</param>
        [Get("/home/{address}/links")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> GetAddressLinksAsync(UserAddress address);

        /// <summary>
        /// Saves a new link for the given address.
        /// This means the address is now in address book.
        /// </summary>
        /// <param name="address">Address to store contacts for.</param>
        /// <param name="accountLink">Link to save for the address.</param>
        [Put("/home/{address}/links/{accountLink.Link}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> StoreLinkAsync(UserAddress address, AccountLink accountLink, [Body] string encryptedAddress);


        /// <summary>
        /// Deletes existing link.
        /// </summary>
        /// <param name="address">Address to delete contacts from..</param>
        /// <param name="accountLink">Link to delete for the address.</param>
        [Delete("/home/{address}/links/{accountLink.Link}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> DeleteLinkAsync(UserAddress address, AccountLink accountLink);

        // TODO: This should be targeted to target's host address.
        /// <summary>
        /// Creates a notification for message in the reader's agent.
        /// </summary>
        [Put("/mail/{address}/link/{accountLink.Link}/notifications")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> CreateNotificationAsync(UserAddress address, AccountLink accountLink, [Body] string body);
    }
}
