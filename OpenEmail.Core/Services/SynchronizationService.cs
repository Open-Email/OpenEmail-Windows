using CommunityToolkit.Diagnostics;
using OpenEmail.Contracts.Application;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Core.Services
{
    public class SynchronizationService : ISynchronizationService
    {
        private readonly INotificationsService _notificationsService;
        private readonly IContactService _contactService;
        private readonly IApplicationStateService _applicationStateService;
        private readonly IProfileDataManager _profileDataManager;
        private readonly IMessagesService _messagesService;
        private readonly ILinksService _linksService;
        private readonly IAttachmentManager _attachmentManager;
        private readonly IPublicClientService _publicClientService;
        private readonly IProfileDataService _profileDataService;

        // There can only be one synchronization at a time.
        private readonly SemaphoreSlim _semaphoreSlim = new(1);

        public SynchronizationService(INotificationsService notificationsService,
                                      IContactService contactService,
                                      IApplicationStateService applicationStateService,
                                      IProfileDataManager profileDataManager,
                                      IMessagesService messagesService,
                                      ILinksService linksService,
                                      IAttachmentManager attachmentManager,
                                      IPublicClientService publicClientService,
                                      IProfileDataService profileDataService)
        {
            _notificationsService = notificationsService;
            _contactService = contactService;
            _applicationStateService = applicationStateService;
            _profileDataManager = profileDataManager;
            _messagesService = messagesService;
            _linksService = linksService;
            _attachmentManager = attachmentManager;
            _publicClientService = publicClientService;
            _profileDataService = profileDataService;
        }

        public async Task SynchronizeAsync(AccountProfile accountProfile)
        {


            /*
            - fetch notifications from own server
            - if notification is from a link I do not have in my contacts, it is show as a contact request in the contacts
            - for notifications from known contacts, we connect to their agents and fetch messages from them
            - for all other contacts, who are not in notifications, connect and fetch if new messages present
            - for all contacts for who we have “broadcasts” subscription enable, fetch their broadcasts
            */

            try
            {
                await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

                await HandleOutgoingMessagesAsync(accountProfile).ConfigureAwait(false);

                // All links are approved contact requests.
                await HandleLinksAsync(accountProfile);

                // Download new notifications, validate them and insert them into the database.
                // Create new contact requests if the notification link is not in the contacts.
                // This call will not download any envelope.

                await HandleNewNotificationsAsync(accountProfile).ConfigureAwait(false);

                // We can now start downloading envelopes for the contacts.
                // For contacts that are not approved yet, no envelopes will be downloaded.

                // Get approved contacts for the account.
                var approvedContacts = await _contactService.GetApprovedContactsAsync(accountProfile.Account.Id).ConfigureAwait(false);

                // Start downloading new messages for contacts.
                foreach (var contact in approvedContacts)
                {
                    await DownloadContactMessagesInternalAsync(contact).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Fetches the links for the account profile and creates contact requests for the links that are not in the contacts.
        /// Denied and already accepted contacts are skipped.
        /// Pending contacts are approved.
        /// Locally non-existing contacts are created with their profile data.
        /// </summary>
        /// <param name="accountProfile">Profile account to sync links for.</param>
        private async Task HandleLinksAsync(AccountProfile accountProfile)
        {
            var links = await _linksService.GetProfileUserAddressLinksAsync(accountProfile);

            foreach (var link in links)
            {
                var address = UserAddress.CreateFromAddress(link.FullAddress);

                // Check whether this link is already in the contacts.
                var contactModel = await _contactService.GetContactAsync(accountProfile.Account.Id, link.FullAddress).ConfigureAwait(false);

                if (contactModel == null)
                {
                    // Create new approved contact for the account.

                    var contactProfileData = await GetContactProfileDataAsync(address);
                    await _contactService.CreateNewContactRequestAsync(accountProfile, contactProfileData, address.FullAddress, true).ConfigureAwait(false);
                }
                else if (contactModel.IsRequestAcccepted)
                {
                    // User either accepted the request before.
                    // In this case we don't need to do anything.
                }
                else if (!contactModel.IsRequestAcccepted)
                {
                    // Request is pending locally, but accepted on the server side since the link exists.
                    // We need to update the contact request to be accepted.
                    // Profile data is already synced since the contact exists locally.

                    await _contactService.AcceptContactRequestAsync(contactModel.UniqueId).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// 1. Gets new + validated notifications.
        /// 2. Checks whether the notification link is already in the contacts. Creates a contact request if the notification link is not in the contacts.
        /// </summary>
        /// <param name="profile">Profile to handle new notifications for.</param>
        private async Task HandleNewNotificationsAsync(AccountProfile profile)
        {
            // We start by retrieving valid + new notifications.
            var notificationResponses = await _notificationsService.FetchNewNotificationsAsync(profile.Address).ConfigureAwait(false);

            if (notificationResponses.Count != 0)
            {
                // Get valid notifications for the profile.
                // Validation will also resolve the encrypted address.

                var validNotifications = notificationResponses.Where(a => a.ValidateFor(profile));

                foreach (var notification in validNotifications)
                {
                    // Check whether this notification exists.
                    var existingNotification = await _notificationsService.GetNotificationAsync(notification.Id).ConfigureAwait(false);

                    // Exists. We don't need to do anything.
                    if (existingNotification != null) continue;

                    var address = UserAddress.CreateFromAddress(notification.DecryptedNotifierAddress);

                    // Check whether this notification link is already in the contacts.
                    var contactModel = await _contactService.GetContactAsync(profile.Account.Id, address.FullAddress).ConfigureAwait(false);

                    bool isContactExists = contactModel != null;

                    if (!isContactExists)
                    {
                        var contactProfileData = await GetContactProfileDataAsync(address);

                        // Create non-approved contact request for this address.
                        await _contactService.CreateNewContactRequestAsync(profile, contactProfileData, address.FullAddress, false).ConfigureAwait(false);
                    }

                    // Add notification to the database.
                    await _notificationsService.InsertNotificationAsync(notification.AsNotification()).ConfigureAwait(false);
                }
            }
        }

        private async Task DownloadContactMessagesInternalAsync(AccountContact contact)
        {
            Guard.IsNotNull(contact, nameof(contact));

            var toAddress = UserAddress.CreateFromAddress(contact.Address);
            var connectionLink = new AccountLink(contact.Link);

            List<string> handlingMessageIds = new();

            var privateMessageIds = await _messagesService.GetEnvelopeIdsAsync(toAddress, connectionLink).ConfigureAwait(false);

            if (contact.ReceiveBroadcasts)
            {
                var broadcastMessageIds = await _messagesService.GetBroadcastMessageIdsAsync(toAddress).ConfigureAwait(false);

                if (broadcastMessageIds != null)
                {
                    handlingMessageIds.AddRange(broadcastMessageIds);
                }
            }

            if (privateMessageIds != null)
            {
                handlingMessageIds.AddRange(privateMessageIds);
            }

            foreach (var messageId in handlingMessageIds)
            {
                await _messagesService.HandleMessageAsync(toAddress, connectionLink, messageId).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Synchronizes Outbox of the given profile.
        /// </summary>
        /// <param name="accountProfile">Profile to sync outbox for.</param>
        private async Task HandleOutgoingMessagesAsync(AccountProfile accountProfile)
        {
            var messageIds = await _messagesService.GetEnvelopeIdsAsync(accountProfile.UserAddress, null);

            if (messageIds != null)
            {
                // Find authored messages locally.
                // 1. Download locally missing ones.
                // 2. Remove remotely deleted ones.

                var localOutboxMessages = await _messagesService.GetMessagesAsync(a => a.Author == accountProfile.Address && !a.IsDraft && !a.IsDeleted).ConfigureAwait(false);

                // Remove remotely deleted messages.
                var deletedMessages = localOutboxMessages.Where(a => !messageIds.Contains(a.EnvelopeId));

                foreach (var message in deletedMessages)
                {
                    await _messagesService.DeleteMessagePermanentAsync(message.Id).ConfigureAwait(false);
                }

                // Download missing messages.
                var missingMessages = messageIds.Where(a => !localOutboxMessages.Select(b => b.EnvelopeId).Contains(a));

                foreach (var messageId in missingMessages)
                {
                    try
                    {
                        await _messagesService.HandleMessageAsync(accountProfile.UserAddress, null, messageId);
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        /// <summary>
        /// Gets the profile data for the given address.
        /// Profile data is downloaded if it doesn't exist locally.
        /// Otherwise it's fetched from the local cache.
        /// </summary>
        /// <param name="address">Address of the contact.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Raised when profile data can't be accessed after download or can't be downloaded.</exception>
        private async Task<ProfileData> GetContactProfileDataAsync(UserAddress address)
        {
            // Contact has never been initiated for this account.
            // Create contact request for this address that waits for approval.
            // Download contact details and profile image for this account if doesn't exists.

            ProfileData contactProfileData = null;

            if (!_profileDataManager.IsProfileDataExists(address))
            {
                // TODO: These calls might throw. Properly handle http or service failures...
                // Ideally we can't continue without the profile data because name is required for the contact request.

                contactProfileData = await _profileDataService.RefreshProfileDataAsync(address);
            }
            else
            {
                contactProfileData = await _profileDataManager.GetProfileDataAsync(address).ConfigureAwait(false);
            }

            if (contactProfileData == null) throw new Exception("Couldn't get profile data for the contact");

            return contactProfileData;
        }

    }
}
