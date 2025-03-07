using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Contracts.Services
{
    public interface IProfileDataService
    {
        /// <summary>
        /// Retrieves profile data for the given address.
        /// Fetches the data from the local cache if available, otherwise fetches it from the server.
        /// </summary>
        /// <param name="address">Address to refresh profile for.</param>
        /// <returns>Profile data.</returns>
        Task<ProfileData> GetProfileDataAsync(UserAddress address, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetches and updates the local profile data for the given address.
        /// </summary>
        /// <param name="address">Address to refresh profile for.</param>
        /// <returns>New profile data.</returns>
        Task<ProfileData> RefreshProfileDataAsync(UserAddress address, bool refreshProfileImage = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates user's profile data.
        /// </summary>
        /// <param name="address">Address to update profile for.</param>
        /// <param name="profileData">Profile data</param>
        /// <returns>Refreshed profile data.</returns>
        Task<ProfileData> UpdateProfileDataAsync(UserAddress address, ProfileData profileData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates user's profile image.
        /// </summary>
        /// <param name="address">Address to update profile for.</param>
        /// <param name="imageData">Resized byte array of image data.</param>
        Task UpdateProfileImageAsync(UserAddress address, byte[] imageData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes user's avatar.
        /// </summary>
        /// <param name="address">Address to delete avatar for.</param>
        Task DeleteProfileImageAsync(UserAddress address);
    }
}
