using OpenEmail.ViewModels;

namespace OpenEmail.Views
{
    public abstract class MailListPageAbstract : BasePage<MailListPageViewModel> { }
    public sealed partial class MailListPage : MailListPageAbstract
    {
        public MailListPage()
        {
            InitializeComponent();
        }

        //protected override void OnNavigatedTo(NavigationEventArgs e)
        //{
        //    base.OnNavigatedTo(e);

        //    if (e.Parameter is ComposeWindowArgs args)
        //    {
        //        var window = WindowHelper.CreateWindow(new ComposeWindow(args));

        //        window.Activate();
        //    }
        //}
    }
}
