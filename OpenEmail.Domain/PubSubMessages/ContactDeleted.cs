using OpenEmail.Domain.Entities;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// Raised when a contact is deleted from account.
    /// </summary>
    /// <param name="Contact">Deleted contact.</param>
    public record ContactDeleted(AccountContact Contact);
}
