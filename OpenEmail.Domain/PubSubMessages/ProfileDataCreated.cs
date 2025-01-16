using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// Fired when a profile data is created.
    /// </summary>
    /// <param name="UserAddress">New address.</param>
    /// <param name="ProfileData">New profile data.</param>
    public record ProfileDataCreated(UserAddress UserAddress, ProfileData ProfileData);
}
