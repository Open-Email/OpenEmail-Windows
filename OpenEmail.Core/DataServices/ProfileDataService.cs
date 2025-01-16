using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.Clients;
using OpenEmail.Contracts.Services;
using OpenEmail.Core.API.Refit;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Profile;
using OpenEmail.Domain.PubSubMessages;

namespace OpenEmail.Core.DataServices
{
    public class ProfileDataService : IProfileDataService
    {
        private readonly IProfileDataManager _profileDataManager;
        private readonly IPublicClientService _publicClientService;
        private readonly IClientFactory _clientFactory;

        public ProfileDataService(IProfileDataManager profileDataManager, IPublicClientService publicClientService, IClientFactory clientFactory)
        {
            _profileDataManager = profileDataManager;
            _publicClientService = publicClientService;
            _clientFactory = clientFactory;
        }

        public async Task DeleteProfileImageAsync(UserAddress address)
        {
            var profileClient = _clientFactory.CreateProfileClient<IProfileClient>();

            var message = await profileClient.DeleteProfileImageAsync(address).ConfigureAwait(false);

            if (message.IsSuccessStatusCode)
            {
                await _profileDataManager.DeleteProfileImageAsync(address).ConfigureAwait(false);
                WeakReferenceMessenger.Default.Send(new ProfileImageUpdated());
            }
        }

        public async Task<ProfileData> GetProfileDataAsync(UserAddress address, CancellationToken cancellationToken = default)
        {
            var existingProfile = await _profileDataManager.GetProfileDataAsync(address, cancellationToken);

            if (existingProfile == null)
            {
                return await RefreshProfileDataAsync(address, true, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return existingProfile;
            }
        }

        public async Task<ProfileData> RefreshProfileDataAsync(UserAddress address, bool refreshProfileImage = true, CancellationToken cancellationToken = default)
        {
            var contactProfileData = await _publicClientService.FetchProfileDataAsync(address, cancellationToken).ConfigureAwait(false);

            if (refreshProfileImage)
            {
                var profileImage = await _publicClientService.FetchProfileImageDataAsync(address, cancellationToken).ConfigureAwait(false);

                // Save profile image first, because saving profile data will fire a message for update.
                await _profileDataManager.SaveProfileImageAsync(profileImage, address, cancellationToken).ConfigureAwait(false);
            }

            await _profileDataManager.SaveProfileDataAsync(contactProfileData, address, cancellationToken).ConfigureAwait(false);

            return contactProfileData;
        }

        public async Task<ProfileData> UpdateProfileDataAsync(UserAddress address, ProfileData profileData, CancellationToken cancellationToken = default)
        {
            // Always update the profile before the push.
            profileData.UpdateAttribute("Updated", DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            var profileClient = _clientFactory.CreateProfileClient<IProfileClient>();
            var serialized = profileData.Serialize();

            await profileClient.UpdateProfileDataAsync(address, serialized, cancellationToken).ConfigureAwait(false);

            return await RefreshProfileDataAsync(address, refreshProfileImage: false, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> UpdateProfileImageAsync(UserAddress address, byte[] imageData, CancellationToken cancellationToken = default)
        {
            var client = _clientFactory.CreateProfileClient<IProfileClient>();
            var updatedMessage = await client.UpdateProfileImageAsync(address, new ByteArrayContent(imageData), cancellationToken);

            if (updatedMessage.IsSuccessStatusCode)
            {
                // Update locally if success.
                await _profileDataManager.SaveProfileImageAsync(imageData, address, cancellationToken).ConfigureAwait(false);

                WeakReferenceMessenger.Default.Send(new ProfileImageUpdated());
            }
            else
                return false;

            return true;
        }
    }
}
