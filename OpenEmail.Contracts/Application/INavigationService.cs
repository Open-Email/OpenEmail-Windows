using OpenEmail.Domain.Models.Navigation;

namespace OpenEmail.Contracts.Application
{
    public interface INavigationService
    {
        void Navigate(PageType page, object parameter = null);
    }
}
