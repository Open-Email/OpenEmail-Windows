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

        private const long MaximumImageSize = 1024 * 512; // 512 KB

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

        public SKImageInfo ValidateAndGetImageInfo(byte[] downloadedImage)
        {
            // Supported formats JPEG,GIF, PNG and WebP.
            // Image must be square. Ideally 400x400.
            // Maximum size 512 KB.

            bool exceedsSize = downloadedImage.Length > MaximumImageSize;

            if (exceedsSize)
                throw new ArgumentException($"Image size is too large. Maximum allowed size {MaximumImageSize} bytes.");

            using var input = new MemoryStream(downloadedImage);

            var info = SKBitmap.DecodeBounds(input);

            int width = info.Width;
            int height = info.Height;

            bool isImageSquare = width == height;

            if (!isImageSquare)
                throw new ArgumentException("Profile picture must be square, ideally 400x400 in size with less than 512kb.");

            return info;
        }

        public bool CanSaveImage(byte[] imageData)
        {
            var imageInfo = ValidateAndGetImageInfo(imageData);
            return imageInfo.Width <= 400 && imageInfo.Height <= 400;
        }

        public async Task SaveProfileImageAsync(byte[] downloadedImage, UserAddress address, CancellationToken cancellationToken = default)
        {
            if (downloadedImage.Length == 0) return;

            byte[] finalImageData = null;

            var imageInfo = ValidateAndGetImageInfo(downloadedImage);

            int width = imageInfo.Width;
            int height = imageInfo.Height;

            bool exceedsBoundsLimit = width > 400 || height > 400;

            // Resize to 400x400.
            if (exceedsBoundsLimit)
            {
                finalImageData = GetResizedImageData(downloadedImage, 400, 400);
            }
            else
            {
                // Everything looks good.
                finalImageData = downloadedImage;
            }

            // Save 64x64 image as thumbnail.
            var thumbnailImageData = GetResizedImageData(downloadedImage, 64, 64);

            await File.WriteAllBytesAsync($"{Path.Combine(ProfileDataFolderPath, address.FullAddress)}{ProfileImageFileExtension}", finalImageData, cancellationToken);
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
