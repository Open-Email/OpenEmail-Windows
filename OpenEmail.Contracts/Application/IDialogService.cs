
using OpenEmail.Domain.Models.Shell;

namespace OpenEmail.Contracts.Application
{
    public interface IDialogService
    {
        Task ShowAddNewContactDialogAsync();
        Task<byte[]> ShowProfilePicturePickerAsync();
        Task ShowMessageAsync(string title, string message, WindowType mainWindowType = WindowType.Shell);
        void ShowInfoBarMessage(string title, string message, InfoBarMessageSeverity severity, bool autoDismiss = true);
    }
}
