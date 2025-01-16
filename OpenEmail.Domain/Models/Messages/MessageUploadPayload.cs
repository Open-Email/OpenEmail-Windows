using System.Diagnostics;
using System.Text;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Cryptography;
using OpenEmail.Domain.Models.Mail;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Domain.Models.Messages
{
    public class MessageUploadPayload : KeyValueDataStore
    {
        public byte[] MessageAccessKey { get; set; }
        public KeyValueDataStore EnvelopeHeaderStore { get; } = new KeyValueDataStore();
        public string Author { get; }
        public string ContentChecksum { get; }

        public Dictionary<string, string> AsDictionary() => Data;

        public MessageUploadPayload(string envelopeId,
                                    string author,
                                    string contentSize,
                                    string category,
                                    DateTimeOffset createdAt,
                                    string subject,
                                    string subjectId,
                                    string parentId,
                                    string readers,
                                    byte[] content,
                                    List<ReaderUploadData> readerProfileMap,
                                    List<MessageAttachment> attachments,
                                    AccountProfile profile,
                                    PayloadSeal seal)
        {
            // Prepare header content for the message.

            bool isBroadcast = readerProfileMap == null || !readerProfileMap.Any();

            var checksum = CryptoUtils.Sha256Sum(content);

            ContentChecksum = checksum.Item1;

            EnvelopeHeaderStore.Add(CoreConstants.HEADER_CONTENT_MESSAGE_ID, envelopeId);
            EnvelopeHeaderStore.Add(CoreConstants.HEADER_CONTENT_AUTHOR, author);
            EnvelopeHeaderStore.Add(CoreConstants.HEADER_CONTENT_SIZE, contentSize);
            EnvelopeHeaderStore.Add(CoreConstants.HEADER_CONTENT_CHECKSUM, $"algorithm={CryptoConstants.CHECKSUM_ALGORITHM}; value={ContentChecksum}");
            EnvelopeHeaderStore.Add(CoreConstants.HEADER_CONTENT_CATEGORY, category);
            EnvelopeHeaderStore.Add(CoreConstants.HEADER_CONTENT_DATE, createdAt.ToString(CoreConstants.ISOFormat));
            EnvelopeHeaderStore.Add(CoreConstants.HEADER_CONTENT_SUBJECT, subject);

            if (!string.IsNullOrEmpty(subjectId))
            {
                EnvelopeHeaderStore.Add(CoreConstants.HEADER_CONTENT_SUBJECT_ID, subjectId);
            }

            if (!string.IsNullOrEmpty(parentId))
            {
                EnvelopeHeaderStore.Add(CoreConstants.HEADER_CONTENT_PARENT_ID, parentId);
            }

            if (!string.IsNullOrEmpty(readers))
            {
                EnvelopeHeaderStore.Add(CoreConstants.HEADER_CONTENT_READERS, readers);
            }

            if (attachments?.Any() ?? false)
            {
                EnvelopeHeaderStore.Add(CoreConstants.HEADER_CONTENT_FILES, string.Join(",", attachments.Select(a => CreateAttachmentFile(a, attachments.Count))));
            }

            // Encrypt headers.

            var str = EnvelopeHeaderStore.ToString();
            var contentHeaderBytes = Encoding.ASCII.GetBytes(str);

            // Preperation for headers are done.
            // Below we are preparing the message headers.

            // Header: Message-Id
            Add(CoreConstants.HEADER_MESSAGE_ID, envelopeId);

            // Header: Message-Access
            if (isBroadcast)
            {
                Add(CoreConstants.HEADER_MESSAGE_HEADERS, $"value={Convert.ToBase64String(contentHeaderBytes)}");
            }
            else
            {
                // Generate access key.
                MessageAccessKey = CryptoUtils.GenerateRandomBytes(32);

                // Make sure to add Author to the readers if not already added.

                if (!readerProfileMap.Any(a => a.Address == profile.Address))
                {
                    var authorReaderData = new ReaderUploadData(profile.Address, profile.Account.PublicEncryptionKey, profile.Account.PublicSigningKey, profile.Account.PublicEncryptionKeyId);
                    readerProfileMap.Add(authorReaderData);
                }

                // Header: Message-Encryption
                Add(CoreConstants.HEADER_MESSAGE_ENCRYPTION, seal.AsHeader());

                // Header: Message-Access
                List<string> accessLinks = new();

                foreach (var reader in readerProfileMap)
                {
                    var signingKey = Convert.FromBase64String(reader.SigningKey);
                    var encryptionKey = Convert.FromBase64String(reader.EncryptionKey);

                    var link = AccountLink.Create(profile.UserAddress, UserAddress.CreateFromAddress(reader.Address));

                    var accessKeyFingerprint = CryptoUtils.Sha256Sum(signingKey);
                    var accessKey = CryptoUtils.EncryptAnonymous(MessageAccessKey, encryptionKey);

                    var readerAccessLink = CreateAccessLinkGroup(link, accessKeyFingerprint.Item1, accessKey, reader.EncryptionKeyId);

                    accessLinks.Add(readerAccessLink);
                }

                Add(CoreConstants.HEADER_MESSAGE_ACCESS, string.Join(", ", accessLinks));

                var encryptedHeaderContent = CryptoUtils.EncryptSymmetric(contentHeaderBytes, MessageAccessKey);
                Add(CoreConstants.HEADER_MESSAGE_HEADERS, $"algorithm={CryptoConstants.SYMMETRIC_CIPHER}; value={Convert.ToBase64String(encryptedHeaderContent)}");

                Debug.WriteLine("Message access key (in base 64) is: " + Convert.ToBase64String(MessageAccessKey));
            }

            // Header: Message-Checksum

            OrderKeys();

            string headerString = string.Empty;

            foreach (var keyPair in Data)
            {
                headerString += $"{keyPair.Value}";
            }

            Debug.WriteLine("Header String: " + headerString);

            var headerChecksumTuple = CryptoUtils.Sha256Sum(Encoding.ASCII.GetBytes(headerString));

            var checksumOrder = $"{string.Join(":", Data.Keys.OrderBy(a => a))}";
            Add(CoreConstants.HEADER_MESSAGE_ENVELOPE_CHECKSUM, $"algorithm={CryptoConstants.CHECKSUM_ALGORITHM}; order={checksumOrder}; value={headerChecksumTuple.Item1}");

            Debug.WriteLine("Header Checksum: " + headerChecksumTuple.Item1);

            var signedChecksum = CryptoUtils.SignData(profile.PublicSigningKey, profile.PrivateSigningKey, headerChecksumTuple.Item2);

            // Header: Message-Signature
            Add(CoreConstants.HEADER_MESSAGE_ENVELOPE_SIGNATURE, $"algorithm={CryptoConstants.SIGNING_ALGORITHM}; value={signedChecksum}; id={profile.Account.PublicEncryptionKeyId}");
            Author = author;
        }

        private string CreateAttachmentFile(MessageAttachment attachment, int totalParts)
            => $"name={attachment.FileName}; type={attachment.MimeType}; modified={attachment.ModifiedAt.ToString(CoreConstants.ISOFormat)}; size={attachment.Size}; id={attachment.Id}; part={attachment.Part}/{totalParts}";

        private string CreateAccessLinkGroup(AccountLink link, string accessKeyFingerprint, byte[] accessKey, string encryptionKeyId)
            => $"link={link.Link}; fingerprint={accessKeyFingerprint}; value={Convert.ToBase64String(accessKey)}; id={encryptionKeyId}";
    }
}
