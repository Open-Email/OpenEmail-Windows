using OpenEmail.Domain.Models.Mail;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// Requsets to send a message.
    /// </summary>
    /// <param name="MessageId">Id of the message.</param>
    /// <param name="ReaderUploadMap">Profile data of the readers..</param>
    public record class SendMessage(Guid MessageId, List<ReaderUploadData> ReaderUploadMap);
}
