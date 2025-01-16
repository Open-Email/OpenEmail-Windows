using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Contracts.Services
{
    public interface ILoginService
    {
        Task<Account> AuthenticateAsync(UserAddress address, string privateEncryptionKey, string privateSigningKey);

        Task LogoutAsync(AccountProfile profile);
    }
}
