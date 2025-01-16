
using OpenEmail.Domain.Models.Shell;

namespace OpenEmail.Contracts.Application
{
    public interface IFileService
    {
        Task<string> PickFolderAsync(WindowType callerWindow = WindowType.Shell);
        Task<List<string>> PickFilesAsync(WindowType callerWindow = WindowType.Shell);
        Task LaunchFileAsync(string filePath);
    }
}
