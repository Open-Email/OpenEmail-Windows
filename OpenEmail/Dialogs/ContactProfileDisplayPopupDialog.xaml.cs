using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenEmail.Domain.Models.Contacts;
using OpenEmail.ViewModels.Data;
using OpenEmail.ViewModels.Dialogs;

namespace OpenEmail.Dialogs
{
    public abstract class ContactProfileDisplayPopupDialogAbstract : BaseContentDialog<ContactProfileDisplayPopupDialogViewModel> { }
    public sealed partial class ContactProfileDisplayPopupDialog : ContactProfileDisplayPopupDialogAbstract
    {
        public ContactViewModel Contact
        {
            get { return (ContactViewModel)GetValue(ContactProperty); }
            set { SetValue(ContactProperty, value); }
        }

        public static readonly DependencyProperty ContactProperty = DependencyProperty.Register(nameof(Contact), typeof(ContactViewModel), typeof(ContactProfileDisplayPopupDialog), new PropertyMetadata(null));

        public ContactPopupDialogResult Result { get; set; }

        public ContactProfileDisplayPopupDialog(ContactViewModel contactViewModel, bool isInContacts)
        {
            InitializeComponent();

            ContactControl.Contact = contactViewModel;

            // If the profile is not loaded, we need to disable the button until it is loaded.
            IsPrimaryButtonEnabled = contactViewModel.Profile == null ? false : !isInContacts;
        }

        public void AddContactClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Result = ContactPopupDialogResult.AddContact;
            Hide();
        }

        public void RemoveReaderClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Result = ContactPopupDialogResult.RemoveReader;
            Hide();
        }

        private void ProfileLoadCompleted(object sender, bool e)
        {
            // Success.
            if (e)
            {
                IsPrimaryButtonEnabled = true;
            }
        }

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
