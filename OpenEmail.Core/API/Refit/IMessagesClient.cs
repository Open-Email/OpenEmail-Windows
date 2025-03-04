using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Cryptography;
using Refit;

namespace OpenEmail.Core.API.Refit
{
    [Headers($"Authorization: {CryptoConstants.NONCE_SCHEME} ")]
    public interface IMessagesClient
    {
        /// <summary>
        /// Private Messages API to retrieve authored messages.
        /// </summary>
        /// <param name="address">Address to get messages for.</param>
        [Get("/home/{address}/messages")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> GetAuthoredMessagesAsync(UserAddress address);

        /// <summary>
        /// Get message ids for the given account link for the given address.
        /// </summary>
        /// <param name="link">Account link</param>
        [Get("/mail/{address}/link/{link.Link}/messages")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> GetMessageIdsAsync(UserAddress address, AccountLink link);

        /// <summary>
        /// Get message content for the given message id for the given account link for the given address.
        /// </summary>
        /// <param name="address">Target address of the author.</param>
        /// <param name="messageId">Message envelope id.</param>
        /// <param name="link">Connection identifier.</param>
        /// <returns>Response message with content.</returns>
        [Get("/mail/{address}/link/{link.Link}/messages/{messageId}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> GetMessageContentAsync(UserAddress address, string messageId, AccountLink link);

        /// <summary>
        /// Get message content of the authored message.
        /// </summary>
        /// <param name="address">Target address of the author.</param>
        /// <param name="messageId">Message envelope id.</param>
        [Get("/home/{address}/messages/{messageId}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> GetMessageContentAsync(UserAddress address, string messageId);

        /// <summary>
        /// Get message headers for the given message id for the given account link for the given address.
        /// </summary>
        /// <param name="address">Target address of the author.</param>
        /// <param name="messageId">Message envelope id.</param>
        /// <param name="link">Connection identifier.</param>
        /// <returns>Response message with headers.</returns>
        [Head("/mail/{address}/link/{link.Link}/messages/{messageId}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> GetMessageHeadersAsync(UserAddress address, string messageId, AccountLink link);

        /// <summary>
        /// Gets message headers for the authored message of the given authorAddress.
        /// Other override is used by link to get the messages between author and contact using link.
        /// </summary>
        /// <param name="authorAddress">Author of the messages.</param>
        /// <param name="messageId">Message id.</param>
        [Head("/home/{authorAddress}/messages/{messageId}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> GetMessageHeadersAsync(UserAddress authorAddress, string messageId);

        /// <summary>
        /// Deletes a message from the given address.
        /// </summary>
        /// <param name="address">Address</param>
        /// <param name="messageId">Message envelope id.</param>
        [Delete("/home/{address}/messages/{messageId}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> RecallAuthoredMessageAsync(UserAddress address, string messageId);

        /// <summary>
        /// Gets the broadcast messages for the given address.
        /// </summary>
        /// <param name="address">Address</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [Get("/mail/{address}/messages")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> GetBroadcastMessagesAsync(UserAddress address, CancellationToken cancellationToken = default);


        /// <summary>
        /// Gets the readers that opened the message.
        /// </summary>
        /// <param name="address">Address</param>
        /// <param name="messageId">Message id</param>
        [Get("/home/{address}/messages/{messageId}/deliveries")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> FetchMessageDeliveriesAsync(UserAddress address, string messageId);

        /// <summary>
        /// Sends a message to the given address.
        /// </summary>
        /// <param name="address">Address</param>
        /// <param name="payloadContent">Message payload.</param>
        /// <param name="headers">Headers</param>
        [Post("/home/{address}/messages")]
        [QueryUriFormat(UriFormat.Unescaped)]
        [Headers("Content-Type: application/octet-stream")]
        Task<HttpResponseMessage> SendMessageAsync(
            UserAddress address,
            [Body] ByteArrayContent payloadContent,
            [HeaderCollection] IDictionary<string, string> headers);
    }
}
