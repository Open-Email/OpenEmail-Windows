using System.Threading.Tasks;
using OpenEmail.Contracts.Services;
using OpenEmail.Helpers;

namespace OpenEmail.Services
{
    public class WindowService : IWindowService
    {
        public void GoBackLogin()
        {
            App.Current.LoadLoginPage();

            CloseOlderWindows();
        }

        public void StartShellApplication()
        {
            App.Current.LoadShell();

            CloseOlderWindows();
        }

        private async void CloseOlderWindows()
        {
            await Task.Delay(100);

            while (WindowHelper.ActiveWindows.Count > 1)
            {
                WindowHelper.ActiveWindows[0].Close();
            }
        }
    }
}
