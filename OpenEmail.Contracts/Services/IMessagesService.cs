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
        List<Tuple<MessageAttachment, byte[]>> CreateMessageAttachments(Message rootMessage, string filePath);
        Task SaveMessageAttachmentAsync(MessageAttachment messageAttachment);
        Task RemoveMessageAttachmentAsync(Guid attachmentGroupId);
        Task<Message> GetMessageAsync(Guid messageId);

        /// <summary>
        /// Recalls authored message.
        /// This will delete the message from the server.
        /// </summary>
        /// <param name="messageId">Message id.</param>
        Task RecallMessageAsync(Guid messageId);

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
        Task<string[]> GetBroadcastMessageIdsAsync(UserAddress address);
    }
}
