using System.Text;
using OpenEmail.Contracts.Clients;
using OpenEmail.Contracts.Services;
using OpenEmail.Core.API.Refit;
using OpenEmail.Domain;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Mail;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Core.Services
{
    public class LinksService : ILinksService
    {
        private readonly IClientFactory _clientFactory;

        public LinksService(IClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task CreateNotificationAsync(AccountLink link, UserAddress toAddress, UserAddress fromAddress, ReaderUploadData readerUploadData)
        {
            var encryptedLocalAddress = CryptoUtils.EncryptAnonymous(Encoding.UTF8.GetBytes(fromAddress.FullAddress), Convert.FromBase64String(readerUploadData.EncryptionKey));
            var encodedEncryptedAdddress = Convert.ToBase64String(encryptedLocalAddress);

            var linksClient = _clientFactory.CreateProfileClient<ILinksClient>();
            await linksClient.CreateNotificationAsync(toAddress, link, encodedEncryptedAdddress).ConfigureAwait(false);
        }

        public async Task<List<UserAddress>> GetProfileUserAddressLinksAsync(AccountProfile accountProfile)
        {
            var linksClient = _clientFactory.CreateProfileClient<ILinksClient>();
            var response = await linksClient.GetAddressLinksAsync(accountProfile.Account.Address);
            var content = await response.Content.ReadAsStringAsync();

            // Format: LINK, ENCRYPTED ADDRESS
            if (string.IsNullOrEmpty(content)) return [];

            var links = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var userAddresses = new List<UserAddress>();

            foreach (var link in links)
            {
                // Can't be null or empty
                if (string.IsNullOrEmpty(link)) continue;

                // Must be 2 parts.
                var linkParts = link.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                if (linkParts.Length != 2) continue;

                try
                {
                    // Decrypt the address.
                    var encryptedAddress = linkParts[1];
                    var decryptedAddressBytes = CryptoUtils.DecryptAnonymous(encryptedAddress, accountProfile.PrivateEncryptionKey, accountProfile.PublicEncryptionKey);

                    var decryptedAddress = Encoding.ASCII.GetString(decryptedAddressBytes);

                    userAddresses.Add(UserAddress.CreateFromAddress(decryptedAddress));
                }
                catch (Exception)
                {
                    // Ignore invalid links.
                }
            }

            return userAddresses;
        }

        public async Task<bool> RemoveLinkAsync(UserAddress fromAddress, UserAddress toAddress)
        {
            var linksClient = _clientFactory.CreateProfileClient<ILinksClient>();
            var response = await linksClient.DeleteLinkAsync(fromAddress, AccountLink.Create(fromAddress, toAddress));

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> StoreLinkAsync(AccountProfile profile, UserAddress toAddress)
        {
            var linksClient = _clientFactory.CreateProfileClient<ILinksClient>();

            // Encrypt toAddress.

            var fromAddress = profile.UserAddress;

            var encryptedAddress = CryptoUtils.EncryptAnonymous(Encoding.UTF8.GetBytes(toAddress.FullAddress), profile.PublicEncryptionKey);
            var encryptedAddressBase64 = Convert.ToBase64String(encryptedAddress);
            var response = await linksClient.StoreLinkAsync(fromAddress, AccountLink.Create(fromAddress, toAddress), encryptedAddressBase64);

            return response.IsSuccessStatusCode;
        }
    }
}
