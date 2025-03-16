using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Mail;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Contracts.Services
{
    public interface ILinksService
    {
        Task<List<LinkResponse>> GetProfileUserAddressLinksAsync(AccountProfile accountProfile);
        Task<bool> StoreLinkAsync(AccountProfile profile, AccountContact contact);
        Task<bool> RemoveLinkAsync(UserAddress fromAddress, UserAddress toAddress);
        Task CreateNotificationAsync(AccountLink link, UserAddress toAddress, UserAddress fromAddress, ReaderUploadData readerUploadData);
    }
}
