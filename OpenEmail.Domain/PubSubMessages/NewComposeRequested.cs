using OpenEmail.Domain.Models.Mail;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// When creating new message, replying or forwarding existing message.
    /// This will create a new Window with the ComposePage.
    /// </summary>
    /// <param name="Args"></param>
    public record NewComposeRequested(ComposeWindowArgs Args);
}
