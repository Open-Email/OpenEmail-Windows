using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using OpenEmail.Contracts.Navigation;
using OpenEmail.ViewModels;

namespace OpenEmail.Dialogs
{
    public abstract class BaseContentDialog : ContentDialog
    {
        public abstract IDialogAware ViewModel { get; }
    }

    public abstract class BaseContentDialog<TViewModel> : BaseContentDialog where TViewModel : BaseDialogViewModel
    {
        public override TViewModel ViewModel { get; } = App.Current.Services.GetService<TViewModel>();
    }
}
