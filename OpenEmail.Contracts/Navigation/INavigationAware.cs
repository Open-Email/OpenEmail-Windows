using OpenEmail.Domain.Models.Navigation;

namespace OpenEmail.Contracts.Navigation
{
    public interface INavigationAware
    {
        void OnNavigatedTo(FrameNavigationMode navigationMode, object parameter);
        void OnNavigatedFrom(FrameNavigationMode navigationMode, object parameter);
    }
}
