using OpenEmail.ViewModels;

namespace OpenEmail.Views
{
    public abstract class SettingsPageAbstract : BasePage<SettingsPageViewModel> { }

    public sealed partial class SettingsPage : SettingsPageAbstract
    {
        public SettingsPage()
        {
            InitializeComponent();
        }
    }
}
