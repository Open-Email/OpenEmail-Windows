using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// Raised when a new contact is assigned to account.
    /// </summary>
    /// <param name="Contact">Assigned contact.</param>
    /// <param name="ContactProfileData">Profile data of the new contact.</param>
    public record ContactCreated(AccountContact Contact, ProfileData ContactProfileData);
}
