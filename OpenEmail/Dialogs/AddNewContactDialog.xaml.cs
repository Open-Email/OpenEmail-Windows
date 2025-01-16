using OpenEmail.ViewModels.Dialogs;

namespace OpenEmail.Dialogs
{
    public abstract class AddNewContactDialogAbstract : BaseContentDialog<AddNewContactDialogViewModel> { }
    public sealed partial class AddNewContactDialog : AddNewContactDialogAbstract
    {
        public AddNewContactDialog()
        {
            this.InitializeComponent();
        }

        private void ContactMailKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            // Submit on enter.
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ViewModel.AddContactCommand.Execute(null);
            }
        }
    }
}
