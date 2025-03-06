using System.Linq.Expressions;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;

namespace OpenEmail.Contracts.Services
{
    public interface IMessagesService
    {

        /// <summary>
        /// Does HEAD request to get the headers only for the message.
        /// Attachment envelopes are stored as metadata.
        /// Message envelopes are stored in the database if they don't exist.
        /// Content for message envelopes are fetched with GET request.
        /// Any attachments inside the message envelope are stored in the database, but not downloaded.
        /// If the link is not present, it will handle Outbox (authored) mesage for the given address.
        /// </summary>
        /// <param name="toAddress">Target address that sent the mail.</param>
        /// <param name="link">Connection identifier.</param>
        /// <param name="messageId">Envelope id to download</param>
        Task HandleMessageAsync(UserAddress toAddress, AccountLink link, string messageId);

        /// <summary>
        /// Return the message ids for the given address and link.
        /// If link is not present, it will return Outbox messages for the given address.
        /// </summary>
        /// <param name="address">Requesting user address.</param>
        /// <param name="link">Account link for from - to addresses.</param>
        /// <returns>Array of message id strings.</returns>
        Task<string[]> GetEnvelopeIdsAsync(UserAddress address, AccountLink link);

        Task<bool> IsMessageAttachmentExistsAsync(string attachmentId);
        Task<List<MessageAttachment>> GetMessageAttachmentsAsync(string parentMessageId);
        Task<Guid> GetAttachmentGroupIdAsync(string parentMessageId, string fileName);
        Task<List<Message>> GetMessagesAsync(Expression<Func<Message, bool>> filter);
        Task MarkMessageReadAsync(Guid messageId);
        Task UpdateMessageAsync(Message message);
        Task SaveMessageAttachmentAsync(MessageAttachment messageAttachment);
        Task RemoveMessageAttachmentAsync(Guid attachmentGroupId);
        Task<Message> GetMessageAsync(Guid messageId);

        /// <summary>
        /// Recalls authored message by message id.
        /// This will delete the message from the server.
        /// </summary>
        /// <param name="messageId">Message id.</param>
        Task RecallMessageAsync(Guid messageId);

        /// <summary>
        /// Recalls authored message by envelope id.
        /// This is primarily used for attachments since we don't store them as a Message entity.
        /// </summary>
        /// <param name="envelopeId">Envelope id (remote server id) of the message.</param>
        Task RecallMessageByEnvelopeId(string envelopeId);

        /// <summary>
        /// Soft deletes the message by moving it to trash.
        /// </summary>
        /// <param name="messageId">Message id.</param>
        Task DeleteMessageToTrashAsync(Guid messageId);

        /// <summary>
        /// Permanently deletes the message from local storage.
        /// </summary>
        /// <param name="messageId">Message id.</param>
        Task DeleteMessagePermanentAsync(Guid messageId);

        /// <summary>
        /// Gets the list of broadcast message ids.
        /// </summary>
        /// <param name="address">Contact address.</param>
        /// <returns>Array of ids of the contact's broadcast messages.</returns>
        Task<string[]> GetBroadcastMessageIdsAsync(UserAddress address);

        /// <summary>
        /// Fetches delivery informations for the given envelope id and inserts them into the database.
        /// </summary>
        /// <param name="envelopeId">Envelope id</param>
        Task HandleDeliveryInformationAsync(string envelopeId);

        /// <summary>
        /// Returns the delivery information for the given envelope id.
        /// </summary>
        /// <param name="envelopeId">Envelope id</param>
        /// <returns>A list of delivery info containing links for the given envelope id.</returns>
        Task<List<MessageDeliveryInformation>> GetMessageDeliveryInformationAsync(string envelopeId);

        /// <summary>
        /// Creates a message attachment metadata chunks for the given root message from the file path.
        /// File is not read, but only the file size.
        /// Chunks are created based on the given chunk size.
        /// </summary>
        /// <param name="rootMessage">Root message.</param>
        /// <param name="filePath">Physical file path.</param>
        /// <returns>List of attachment metadata without a content bytes.</returns>
        List<MessageAttachment> CreateMessageAttachmentMetadata(Message rootMessage, string filePath);
    }
}
