using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// Raised when a contact is updated.
    /// </summary>
    /// <param name="Contact">Updated contact.</param>
    /// <param name="ContactProfileData">Profile data of the updated contact.</param>
    public record ContactUpdated(AccountContact Contact, ProfileData ContactProfileData);
}
