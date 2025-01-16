using OpenEmail.Contracts.Configuration;
using Windows.Storage;

namespace OpenEmail.WinRT
{
    public class ApplicationConfiguration : IApplicationConfiguration
    {
        public string ApplicationDataFolderPath => ApplicationData.Current.LocalFolder.Path;
    }
}
