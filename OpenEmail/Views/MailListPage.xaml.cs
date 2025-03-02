using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using OpenEmail.ViewModels;

namespace OpenEmail.Views
{
    public abstract class MailListPageAbstract : BasePage<MailListPageViewModel> { }
    public sealed partial class MailListPage : MailListPageAbstract
    {
        private const double CompactWidth = 1000;
        public bool IsCompactPage
        {
            get { return (bool)GetValue(IsCompactPageProperty); }
            set { SetValue(IsCompactPageProperty, value); }
        }

        public static readonly DependencyProperty IsCompactPageProperty = DependencyProperty.Register(nameof(IsCompactPage), typeof(bool), typeof(MailListPage), new PropertyMetadata(false));

        public MailListPage()
        {
            InitializeComponent();
        }

        private void PageSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            UpdateAdaptiveness();
        }

        private void UpdateAdaptiveness()
        {
            IsCompactPage = ActualWidth < CompactWidth;
            Debug.WriteLine($"Width: {ActualWidth}, IsCompact: {IsCompactPage}");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            UpdateAdaptiveness();
        }
    }
}
