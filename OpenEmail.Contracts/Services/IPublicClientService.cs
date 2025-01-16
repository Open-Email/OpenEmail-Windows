using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Contracts.Services
{
    public interface IPublicClientService
    {
        /// <summary>
        /// Downloads the profile data for the given address from web.
        /// </summary>
        /// <param name="address">Address</param>
        /// <returns>Downloaded profile data.</returns>
        Task<ProfileData> FetchProfileDataAsync(UserAddress address, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads  the profile image data for the gien address from web.
        /// </summary>
        /// <param name="address">Address</param>
        /// <returns>Downloaded profile image data.</returns>
        Task<byte[]> FetchProfileImageDataAsync(UserAddress address, CancellationToken cancellationToken = default);
    }
}
