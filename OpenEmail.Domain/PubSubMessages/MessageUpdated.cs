using OpenEmail.Domain.Entities;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// Raised when message is updated.
    /// </summary>
    /// <param name="UpdatedMessage">Updated message.</param>
    public record MessageUpdated(Message UpdatedMessage);
}
