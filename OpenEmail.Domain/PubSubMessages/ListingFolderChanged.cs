using OpenEmail.Domain.Models.Mail;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// When listing folder is changed when the mail list page is present on the UI.
    /// </summary>
    /// <param name="NewFolder">Type of the folder.</param>
    public record ListingFolderChanged(MailFolder NewFolder, ComposeWindowArgs ComposeWindowArgs = null);
}
