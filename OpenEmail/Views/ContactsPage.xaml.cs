using OpenEmail.ViewModels;


namespace OpenEmail.Views
{
    public abstract class ContactsPageAbstract : BasePage<ContactsPageViewModel> { }

    public sealed partial class ContactsPage : ContactsPageAbstract
    {
        public ContactsPage()
        {
            InitializeComponent();
        }
    }
}
