using CommunityToolkit.Mvvm.Messaging;
using OpenEmail.Contracts.DataServices;
using OpenEmail.Contracts.Services;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Profile;
using OpenEmail.Domain.PubSubMessages;
using SQLite;

namespace OpenEmail.Core.DataServices
{
    public class ContactService : BaseDataService, IContactService
    {
        public ContactService(IDatabaseService<ISQLiteAsyncConnection> databaseService) : base(databaseService) { }

        public Task<int> GetPendingContactRequestCountAsync(Guid accountId)
            => Connection.Table<AccountContact>().CountAsync(a => a.AccountId == accountId && a.IsRequestAcccepted == false);

        public Task<AccountContact> GetContactAsync(Guid accountId, string contactAddress)
            => Connection.Table<AccountContact>().FirstOrDefaultAsync(a => a.AccountId == accountId && a.Address == contactAddress);

        public Task<List<AccountContact>> GetContactsAsync(Guid accountId)
            => Connection.Table<AccountContact>().Where(a => a.AccountId == accountId).ToListAsync();

        public Task<List<AccountContact>> GetApprovedContactsAsync(Guid accountId)
            => Connection.Table<AccountContact>().Where(a => a.AccountId == accountId && a.IsRequestAcccepted).ToListAsync();

        public async Task DeleteContactAsync(AccountContact contact)
        {
            await Connection.DeleteAsync(contact).ConfigureAwait(false);
            WeakReferenceMessenger.Default.Send(new ContactDeleted(contact));
        }

        public Task CreateNewContactRequestAsync(AccountProfile profile, ProfileData contactProfileData, string contactAddress, bool isApproved)
        {
            // Create new link for the contact with the profile.

            var accountLink = AccountLink.Create(profile.UserAddress, UserAddress.CreateFromAddress(contactAddress));

            var contact = new AccountContact()
            {
                UniqueId = Guid.NewGuid(),
                AccountId = profile.Account.Id,
                Address = contactAddress,
                IsRequestAcccepted = isApproved,
                CreatedAt = DateTimeOffset.Now,
                ReceiveBroadcasts = false,
                Link = accountLink.Link,
                Name = contactProfileData.Name,
                Id = "n/a"
            };

            return AddOrUpdateContactAsync(profile, contact, contactProfileData);
        }

        public async Task<bool> AddOrUpdateContactAsync(AccountProfile profile, AccountContact contact, ProfileData contactProfile)
        {
            // Make sure we don't duplicate the data somehow.
            var existingContact = await GetContactAsync(profile.Account.Id, contact.Address);

            if (existingContact != null)
            {
                await UpdateContactAsync(contact, contactProfile);
            }
            else
            {
                await Connection.InsertAsync(contact).ConfigureAwait(false);

                WeakReferenceMessenger.Default.Send(new ContactCreated(contact, contactProfile));
            }

            return true;
        }

        public async Task UpdateContactAsync(AccountContact contact, ProfileData contactProfile = null)
        {
            await Connection.UpdateAsync(contact).ConfigureAwait(false);
            WeakReferenceMessenger.Default.Send(new ContactUpdated(contact, contactProfile));
        }

        public async Task AcceptContactRequestAsync(Guid contactId)
        {
            var contact = await Connection.Table<AccountContact>().Where(a => a.UniqueId == contactId).FirstOrDefaultAsync().ConfigureAwait(false);

            if (contact == null) return;

            contact.ReceiveBroadcasts = false;
            contact.IsRequestAcccepted = true;
            contact.CreatedAt = DateTimeOffset.Now;

            await Connection.UpdateAsync(contact).ConfigureAwait(false);

            WeakReferenceMessenger.Default.Send(new ContactUpdated(contact, null));
        }
    }
}
