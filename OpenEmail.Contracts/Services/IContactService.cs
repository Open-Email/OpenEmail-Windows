using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Contracts.Services
{
    public interface IContactService
    {
        /// <summary>
        /// Creates a new contact request for the given account id out of notification.
        /// This will create non-approved contact request with no broadcast on.
        /// </summary>
        /// <param name="profile">Account profile to create this contact for.</param>
        /// <param name="contactProfileData">Profile of the contact.</param>
        /// <param name="contactAddress">Address of the contact.</param>
        Task CreateNewContactRequestAsync(AccountProfile profile, ProfileData contactProfileData, string contactAddress, bool isApproved);

        /// <summary>
        /// Adds a new contact request for the given account.
        /// Used when users add contacts by themselves and request is auto approved.
        /// </summary>
        /// <param name="profile">Profile to create contact for.</param>
        /// <param name="contact">Contact</param>
        /// <param name="contactProfile">Contact profile</param>
        /// <returns>False if contact already exists.</returns>
        Task<bool> AddOrUpdateContactAsync(AccountProfile profile, AccountContact contact, ProfileData contactProfile);

        /// <summary>
        /// Gets the contact assignment for the given account id and contact address.
        /// </summary>
        /// <param name="accountId">Account ownership.</param>
        /// <param name="contactAddress">Full address of the account.</param>
        Task<AccountContact> GetContactAsync(Guid accountId, string contactAddress);

        /// <summary>
        /// Gets all contacts (approved or not) for the given account.
        /// </summary>
        /// <param name="accountId">Account to get contacts for.</param>
        Task<List<AccountContact>> GetContactsAsync(Guid accountId);

        /// <summary>
        /// Gets whether contact request exists for the given account id.
        /// </summary>
        /// <param name="accountId">Account id.</param>
        Task<int> GetPendingContactRequestCountAsync(Guid accountId);


        /// <summary>
        /// Accepts the contact request for the assigned account.
        /// </summary>
        /// <param name="contactRequestId">Contact request id.</param>
        Task AcceptContactRequestAsync(Guid contactId);

        /// <summary>
        /// Returns existing approved contacts for the given account id.
        /// </summary>
        /// <param name="accountId">Accountid</param>
        Task<List<AccountContact>> GetApprovedContactsAsync(Guid accountId);
        Task UpdateContactAsync(AccountContact contact, ProfileData contactProfile = null);
        Task DeleteContactAsync(AccountContact contact);
    }
}
