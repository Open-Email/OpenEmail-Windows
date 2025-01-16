using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenEmail.Contracts.Application;
using OpenEmail.Domain.Models.Shell;
using OpenEmail.Helpers;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace OpenEmail.Services
{
    public class FileService : IFileService
    {
        public async Task LaunchFileAsync(string filePath)
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(filePath);

            if (storageFile == null) return;

            await Launcher.LaunchFileAsync(storageFile);
        }

        public async Task<string> PickFolderAsync(WindowType callerWindow = WindowType.Shell)
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;

            AssignXamlRoot(picker, callerWindow);

            var pickedFolder = await picker.PickSingleFolderAsync();

            if (pickedFolder == null) return string.Empty;

            return pickedFolder.Path;
        }


        public async Task<List<string>> PickFilesAsync(WindowType callerWindow = WindowType.Shell)
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };

            picker.FileTypeFilter.Add("*");

            AssignXamlRoot(picker, callerWindow);

            var pickedFiles = await picker.PickMultipleFilesAsync();
            if (pickedFiles == null) return new List<string>();

            var filePaths = new List<string>();

            foreach (var file in pickedFiles)
            {
                filePaths.Add(file.Path);
            }

            return filePaths;
        }

        private void AssignXamlRoot(object target, WindowType windowType = WindowType.Shell)
        {
            // First window is always the shell.
            // Second is composer.

            object targetWindow = windowType switch
            {
                WindowType.Shell => WindowHelper.ActiveWindows[0],
                WindowType.Composer => WindowHelper.ActiveWindows[1],
                _ => throw new NotImplementedException()
            };

            nint windowHandle = WindowNative.GetWindowHandle(targetWindow);
            InitializeWithWindow.Initialize(target, windowHandle);
        }
    }
}
