using System.Diagnostics;
using System.Text;
using OpenEmail.Contracts.Clients;
using OpenEmail.Contracts.Services;
using OpenEmail.Core.API.Refit;
using OpenEmail.Domain;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Contacts;
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

        public async Task<List<LinkResponse>> GetProfileUserAddressLinksAsync(AccountProfile accountProfile)
        {
            var linksClient = _clientFactory.CreateProfileClient<ILinksClient>();
            var response = await linksClient.GetAddressLinksAsync(accountProfile.Account.Address);
            var content = await response.Content.ReadAsStringAsync();

            // Format: LINK, ENCRYPTED CONTENT
            if (string.IsNullOrEmpty(content)) return [];

            var links = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var linkResponses = new List<LinkResponse>();

            foreach (var link in links)
            {
                // Can't be null or empty
                if (string.IsNullOrEmpty(link)) continue;

                // Must be 2 parts.
                var linkParts = link.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                if (linkParts.Length != 2) continue;

                try
                {
                    // Check if the encrypted content is an e-mail address or key-value pair.
                    // Decrypt the address.
                    var encryptedContent = linkParts[1];
                    var decryptedContentBytes = CryptoUtils.DecryptAnonymous(encryptedContent, accountProfile.PrivateEncryptionKey, accountProfile.PublicEncryptionKey);

                    var decryptedContent = Encoding.ASCII.GetString(decryptedContentBytes);

                    // If there is no '=' sign, it's an e-mail address.

                    LinkResponse linkUserAddress = null;

                    if (!decryptedContent.Contains('\n'))
                    {
                        linkUserAddress = new LinkResponse(UserAddress.CreateFromAddress(decryptedContent));
                    }
                    else
                    {
                        // Semicolon separated key-value pair for attributes.
                        var attributeStore = new KeyValueDataStore(decryptedContent, ';');

                        // Invalid. address must be present.
                        if (!attributeStore.HasKey("address"))
                        {
                            Debug.WriteLine($"Skipping link with invalid attributes: {decryptedContent}");
                            continue;
                        }

                        var address = attributeStore.GetData<string>("address");
                        var userAddress = UserAddress.CreateFromAddress(address);

                        if (attributeStore.HasKey("broadcasts"))
                        {
                            var isBroadcastsEnabled = attributeStore.GetData<bool>("broadcasts");

                            linkUserAddress = new LinkResponse(userAddress, isBroadcastsEnabled);
                        }
                        else
                        {
                            linkUserAddress = new LinkResponse(userAddress);
                        }
                    }

                    linkResponses.Add(linkUserAddress);
                }
                catch (Exception)
                {
                    // Ignore invalid links.
                }
            }

            return linkResponses;
        }

        public async Task<bool> RemoveLinkAsync(UserAddress fromAddress, UserAddress toAddress)
        {
            var linksClient = _clientFactory.CreateProfileClient<ILinksClient>();
            var response = await linksClient.DeleteLinkAsync(fromAddress, AccountLink.Create(fromAddress, toAddress));

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> StoreLinkAsync(AccountProfile profile, AccountContact contact)
        {
            var linksClient = _clientFactory.CreateProfileClient<ILinksClient>();

            // Create attribute form.
            var store = new CreateLinkRequestAttributes();
            store.Add("address", profile.UserAddress.FullAddress);
            store.Add("broadcasts", contact.ReceiveBroadcasts);

            var serializedAttributes = store.Serialize();
            var link = AccountLink.Create(profile.UserAddress, UserAddress.CreateFromAddress(contact.Address));

            var encryptedAddress = CryptoUtils.EncryptAnonymous(Encoding.UTF8.GetBytes(serializedAttributes), profile.PublicEncryptionKey);
            var encryptedAddressBase64 = Convert.ToBase64String(encryptedAddress);
            var response = await linksClient.StoreLinkAsync(profile.UserAddress, link, encryptedAddressBase64);

            return response.IsSuccessStatusCode;
        }
    }
}
