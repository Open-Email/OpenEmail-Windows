using OpenEmail.ViewModels;

namespace OpenEmail.Views
{
    public abstract class LoginPageAbstract : BasePage<LoginPageViewModel> { }
    public sealed partial class LoginPage : LoginPageAbstract
    {
        public LoginPage()
        {
            this.InitializeComponent();
        }
    }
}
