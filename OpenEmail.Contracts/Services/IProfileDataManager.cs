using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Contracts.Services
{
    /// <summary>
    /// Manages profile data for contacts.
    /// Profile data is saved in a folder named "ProfileData" in the application's local folder by their addresses.
    /// Data is the same for each account, but ownership is determined by the account.
    /// </summary>
    public interface IProfileDataManager
    {
        bool CanSaveImage(byte[] imageData);
        Task DeleteProfileImageAsync(UserAddress address);

        /// <summary>
        /// Gets the profile data for the given address.
        /// </summary>
        /// <param name="address">Address to get the profile data for.</param>
        Task<ProfileData> GetProfileDataAsync(UserAddress address, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns whether the profile data for the given address exists.
        /// </summary>
        /// <param name="address">Address to check profile data for.</param>
        bool IsProfileDataExists(UserAddress address);

        /// <summary>
        /// Saves downloaded text profile data for the given address.
        /// Updates the existing data if it already exists and notifies messenger.
        /// </summary>
        /// <param name="profileInformation">Data to be saved.</param>
        /// <param name="address">To be saved account for.</param>
        Task SaveProfileDataAsync(ProfileData profileInformation, UserAddress address, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the downloaded profile image data for the given address.
        /// </summary>
        /// <param name="downloadedImage">Image bytes.</param>
        /// <param name="address">Address to save for.</param>
        Task SaveProfileImageAsync(byte[] downloadedImage, UserAddress address, CancellationToken cancellationToken = default);
    }
}
