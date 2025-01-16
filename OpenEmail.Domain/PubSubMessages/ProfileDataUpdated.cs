using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// Fired when a profile data is updated.
    /// </summary>
    /// <param name="UserAddress">Updated address.</param>
    /// <param name="ProfileData">New profile data.</param>
    public record ProfileDataUpdated(UserAddress UserAddress, ProfileData ProfileData);
}
