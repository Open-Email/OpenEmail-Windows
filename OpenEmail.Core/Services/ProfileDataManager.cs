using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Configuration;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Profile;
using OpenEmail.Domain.PubSubMessages;
using SkiaSharp;

namespace OpenEmail.Core.Services
{
    public class ProfileDataManager : IProfileDataManager
    {
        private const string ProfileDataFolder = "ProfileData";
        private const string ProfileDataFileExtension = ".data";
        private const string ProfileImageFileExtension = ".png";

        private const long MaximumImageSize = 1024 * 640; // 640 KB
        private const int MaximumWidth = 800;
        private const int MaximumHeight = 800;


        private string ProfileDataFolderPath { get; }
        private readonly IApplicationConfiguration _applicationConfiguration;

        public ProfileDataManager(IApplicationConfiguration applicationConfiguration)
        {
            _applicationConfiguration = applicationConfiguration;

            ProfileDataFolderPath = Path.Combine(_applicationConfiguration.ApplicationDataFolderPath, ProfileDataFolder);

            // Make sure the root directory exists
            if (!Directory.Exists(ProfileDataFolderPath)) Directory.CreateDirectory(ProfileDataFolderPath);
        }

        private string ResolveProfileDataPath(UserAddress address)
            => $"{Path.Combine(ProfileDataFolderPath, address.FullAddress)}{ProfileDataFileExtension}";

        public async Task<ProfileData> GetProfileDataAsync(UserAddress address, CancellationToken cancellationToken = default)
        {
            var resolvedFilePath = ResolveProfileDataPath(address);
            if (File.Exists(resolvedFilePath))
            {
                var data = await File.ReadAllTextAsync(resolvedFilePath, cancellationToken);

                return new ProfileData(data);
            }

            return null;
        }

        public Task DeleteProfileImageAsync(UserAddress address)
        {
            var imagePath = $"{Path.Combine(ProfileDataFolderPath, address.FullAddress)}{ProfileImageFileExtension}";
            var thumbnailPath = $"{Path.Combine(ProfileDataFolderPath, address.FullAddress)}_thumbnail{ProfileImageFileExtension}";
            if (File.Exists(imagePath)) File.Delete(imagePath);
            if (File.Exists(thumbnailPath)) File.Delete(thumbnailPath);

            return Task.CompletedTask;
        }

        public async Task SaveProfileDataAsync(ProfileData profileInformation, UserAddress address, CancellationToken cancellationToken = default)
        {
            bool isProfileDataExists = IsProfileDataExists(address);

            // Update the data.
            await File.WriteAllTextAsync(ResolveProfileDataPath(address), profileInformation.ToString(), cancellationToken);

            // Notify the messenger if the data was updated.

            if (isProfileDataExists)
            {
                WeakReferenceMessenger.Default.Send(new ProfileDataUpdated(address, profileInformation));
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new ProfileDataCreated(address, profileInformation));
            }
        }

        public byte[] GetValidAvatar(byte[] pickedFileBytes)
        {
            // Max dimensions 800x800.
            // Rescale to 400x400 if exceeds.

            var finalImageData = pickedFileBytes;

            using var input = new MemoryStream(pickedFileBytes);

            var imageInfo = SKBitmap.DecodeBounds(input);

            int width = imageInfo.Width;
            int height = imageInfo.Height;

            bool exceedsBoundsLimit = width > MaximumWidth || height > MaximumHeight;

            // Resize to 400x400.
            if (exceedsBoundsLimit)
            {
                finalImageData = GetResizedImageData(pickedFileBytes, 400, 400);
            }

            // Check size.
            if (finalImageData.Length > MaximumImageSize)
                throw new Exception("Image size exceeds the maximum allowed size. Try again with smaller image.");

            return finalImageData;
        }

        public async Task SaveProfileImageAsync(byte[] downloadedImage, UserAddress address, CancellationToken cancellationToken = default)
        {
            if (downloadedImage.Length == 0) return;

            // Save 64x64 image as thumbnail.
            var thumbnailImageData = GetResizedImageData(downloadedImage, 64, 64);

            await File.WriteAllBytesAsync($"{Path.Combine(ProfileDataFolderPath, address.FullAddress)}{ProfileImageFileExtension}", downloadedImage, cancellationToken);
            await File.WriteAllBytesAsync($"{Path.Combine(ProfileDataFolderPath, address.FullAddress)}_thumbnail{ProfileImageFileExtension}", thumbnailImageData, cancellationToken);
        }

        private byte[] GetResizedImageData(byte[] originalImage, int width, int height)
        {
            SKBitmap srcBitmap = SKBitmap.Decode(originalImage);

            using SKBitmap resizedSKBitmap = srcBitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.Medium);
            using SKImage newImg = SKImage.FromPixels(resizedSKBitmap.PeekPixels());
            using SKData data = newImg.Encode(SKEncodedImageFormat.Png, 100);
            using Stream imgStream = data.AsStream();

            var array = new byte[imgStream.Length];
            imgStream.Read(array, 0, (int)imgStream.Length);
            return array;
        }

        public bool IsProfileDataExists(UserAddress address) => File.Exists(ResolveProfileDataPath(address));
    }
}
