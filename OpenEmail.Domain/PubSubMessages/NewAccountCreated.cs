using OpenEmail.Domain.Entities;

namespace OpenEmail.Domain.PubSubMessages
{
    /// <summary>
    /// Raised when an account is created.
    /// </summary>
    /// <param name="CreatedAccount">Created account.</param>
    public record NewAccountCreated(Account CreatedAccount);
}
