using System.Threading.Tasks;
using OpenEmail.Contracts.Services;

namespace OpenEmail.Services
{
    public class WindowService : IWindowService
    {
        public async Task GoBackLoginAsync()
        {
            await App.Current.LoadLoginPageAsync();

            CloseOlderWindows();
        }

        public async Task StartShellApplicationAsync()
        {
            await App.Current.LoadShellAsync();

            CloseOlderWindows();
        }

        private void CloseOlderWindows()
        {
            //while (WindowHelper.ActiveWindows.Count > 1)
            //{
            //    WindowHelper.ActiveWindows[0].Close();
            //}
        }
    }
}
