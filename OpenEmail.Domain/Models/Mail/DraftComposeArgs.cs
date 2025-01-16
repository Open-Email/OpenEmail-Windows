using OpenEmail.Domain.Entities;

namespace OpenEmail.Domain.Models.Mail
{
    /// <summary>
    /// Final data to send to composer view model.
    /// This will contain the draft message to be edited and action type.
    /// </summary>
    public record DraftComposeArgs : ComposeWindowArgs
    {
        public DraftComposeArgs(MailActionType Type, IMessage ReferencingMessage) : base(Type, ReferencingMessage)
        {
        }
    }
}
