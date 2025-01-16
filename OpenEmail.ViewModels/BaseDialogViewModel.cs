using CommunityToolkit.Mvvm.ComponentModel;
using OpenEmail.Contracts.Navigation;

namespace OpenEmail.ViewModels
{
    public class BaseDialogViewModel : ObservableRecipient, IDialogAware
    {
        public event EventHandler HideRequested;

        public void Hide() => HideRequested?.Invoke(this, EventArgs.Empty);

        public virtual void OnDialogClosed() { }

        public virtual void OnDialogOpened() { }
    }
}
