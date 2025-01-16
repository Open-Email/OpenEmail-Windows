using OpenEmail.Domain.Entities;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// Raised when a new message added to database.
    /// </summary>
    /// <param name="Message">Added message</param>
    public record MessageAdded(Message Message);
}
