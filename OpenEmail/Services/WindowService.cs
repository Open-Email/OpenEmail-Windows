using System.Threading.Tasks;
using OpenEmail.Contracts.Services;

namespace OpenEmail.Services
{
    public class WindowService : IWindowService
    {
        public Task RestartApplicationAsync() => App.Current.StartApplicationAsync();
    }
}
