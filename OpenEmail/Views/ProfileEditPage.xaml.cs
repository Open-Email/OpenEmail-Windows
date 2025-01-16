using OpenEmail.ViewModels;

namespace OpenEmail.Views
{
    public abstract class ProfileEditPageAbstract : BasePage<ProfileEditPageViewModel> { }
    public sealed partial class ProfileEditPage : ProfileEditPageAbstract
    {
        public ProfileEditPage()
        {
            InitializeComponent();
        }
    }
}
