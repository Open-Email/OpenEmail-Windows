using OpenEmail.Domain.Models.Shell;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// Displays an info bar message on the UI that auto-dismisses in 3 seconds.
    /// </summary>
    /// <param name="Title">Title of the message</param>
    /// <param name="Message">Message content</param>
    /// <param name="Severity">Severity of the message</param>
    /// <param name="AutoDismiss">Whether message should fade away after some time or not.</param>
    public record DisplayInfoMessage(string Title, string Message, InfoBarMessageSeverity Severity = InfoBarMessageSeverity.Info, bool AutoDismiss = true);
}
