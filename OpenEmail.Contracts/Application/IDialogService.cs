
using OpenEmail.Domain.Models.Shell;

namespace OpenEmail.Contracts.Application
{
    public interface IDialogService
    {
        Task ShowAddNewContactDialogAsync();
        Task<byte[]> ShowProfilePicturePickerAsync();
        Task ShowMessageAsync(string title, string message, WindowType mainWindowType = WindowType.Shell);
        void ShowInfoBarMessage(string title, string message, InfoBarMessageSeverity severity, bool autoDismiss = true);
        Task<bool> ShowConfirmationDialogAsync(string title, string message, WindowType windowType = WindowType.Shell);
    }
}
