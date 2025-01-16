using OpenEmail.Domain.Entities;

namespace OpenEmail.Domain.Models.Mail
{
    /// <param name="Type">Type of the composing.</param>
    /// <param name="ReferencingMessage">When reply, forward or edit draft, this will store the message that is operating.</param>
    public record ComposeWindowArgs(MailActionType Type, IMessage ReferencingMessage = null);
}
