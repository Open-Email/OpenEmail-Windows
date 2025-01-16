using OpenEmail.Domain.Entities;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// Raised when Message is removed from the database.
    /// </summary>
    /// <param name="Message">Deleted message</param>
    public record MessageDeleted(Message Message);
}
