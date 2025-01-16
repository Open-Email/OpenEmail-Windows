using OpenEmail.ViewModels;

namespace OpenEmail.Views
{
    public class BroadcastPageAbstract : BasePage<BroadcastPageViewModel> { }
    public sealed partial class BroadcastPage : BroadcastPageAbstract
    {
        public BroadcastPage()
        {
            InitializeComponent();
        }
    }
}
