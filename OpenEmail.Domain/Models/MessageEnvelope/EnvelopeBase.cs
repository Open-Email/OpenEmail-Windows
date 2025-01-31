using System.Text;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Cryptography;
using OpenEmail.Domain.Models.Profile;

namespace OpenEmail.Domain.Models.MessageEnvelope
{
    public class EnvelopeBase : KeyValueDataStore
    {

        // TODO
        private static string[] ChecksumHeaders = ["message-access", "message-id", "message-stream", "message-headers", "message-encryption"];

        public string Id { get; private set; }
        public Guid AccountId { get; }
        public List<AccountAccessLink> AccessLinks { get; } = [];
        public EnvelopeChecksum Checksum { get; private set; }
        public EnvelopeSignature EnvelopeSignature { get; private set; }

        public string EncryptionAlgorithm { get; private set; }

        /// <summary>
        /// Decrypted headers as key-value store.
        /// </summary>
        public KeyValueDataStore EnvelopeHeaderStore { get; }

        public byte[] AccessKey { get; }

        public bool IsBroadcastEnvelope => AccessLinks.Count == 0;

        private EnvelopeHeaders _evelopeHeaders;

        // For decrypting message from headers.
        public EnvelopeBase(string dataStoreInput,
                            AccountProfile profile,
                            AccountLink link,
                            byte[] messageResponseContent = null) : base(dataStoreInput, 512 * 1024)
        {
            if (link == null)
            {
                // If link is not provided, create a self link.
                link = AccountLink.Create(profile.UserAddress, profile.UserAddress);
            }

            AccountId = profile.Account.Id;

            ParseEnvelopeId();
            ParseAccessLinks();
            ParseChecksum();
            ParseEnvelopeSignature();
            ParseEnvelopeHeaders();
            ParseEncryptionAlgorithm();

            // Do validations
            ValidateAuthenticity();

            string headerContent = string.Empty;
            var decodedHeaders = CryptoUtils.Base64Decode(_evelopeHeaders.Value);

            if (!IsBroadcastEnvelope)
            {
                var base64AccessLinkValue = AccessLinks.FirstOrDefault(a => a.Link == link.Link).Value;

                var decryptedPrivateEncryptionKey = CryptoUtils.Base64Decode(profile.PrivateEncryptionKeyBase64);
                var decryptedPublicEncryptionKey = CryptoUtils.Base64Decode(profile.Account.PublicEncryptionKey);

                AccessKey = CryptoUtils.DecryptAnonymous(base64AccessLinkValue, decryptedPrivateEncryptionKey, decryptedPublicEncryptionKey);

                headerContent = CryptoUtils.DecryptSymmetricAsString(decodedHeaders, AccessKey);
            }
            else
            {
                // Broadcasts have their headers encoded in base64 without any access key.
                headerContent = Encoding.UTF8.GetString(decodedHeaders);
            }

            EnvelopeHeaderStore = new KeyValueDataStore(headerContent);
        }

        #region Envelope Header Parsers

        private void ParseEncryptionAlgorithm()
        {
            var messageEncryptionHeader = GetData<string>("message-encryption");

            if (string.IsNullOrEmpty(messageEncryptionHeader)) return;

            EncryptionAlgorithm = ParseAttributes(messageEncryptionHeader)["algorithm"];
        }

        private void ParseChecksum()
        {
            var checksumDict = ParseAttributes(GetData<string>("message-checksum"));
            Checksum = new EnvelopeChecksum(checksumDict["algorithm"], checksumDict["value"], checksumDict["order"]);
        }

        private void ParseEnvelopeSignature()
        {
            var signatureDict = ParseAttributes(GetData<string>("message-signature"));
            EnvelopeSignature = new EnvelopeSignature(signatureDict["algorithm"], signatureDict["value"]);
        }

        private void ParseEnvelopeHeaders()
        {
            var headersDict = ParseAttributes(GetData<string>("message-headers"));

            string algorithm = string.Empty;

            if (headersDict.ContainsKey("algorithm"))
            {
                algorithm = headersDict["algorithm"];
            }

            if (!string.IsNullOrEmpty(algorithm) &&
                !string.Equals(CryptoConstants.SYMMETRIC_CIPHER, algorithm, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new Exception("Cryptographic mismatch on the headers.");
            }

            var value = headersDict["value"];

            _evelopeHeaders = new EnvelopeHeaders(algorithm, value);
        }

        private void ParseEnvelopeId() => Id = GetData<string>("message-id");

        private void ParseAccessLinks()
        {
            var accessLine = GetData<string>("message-access");
            if (string.IsNullOrEmpty(accessLine)) return;

            var splitAccess = accessLine.Split(" ").ToList();

            foreach (var key in splitAccess)
            {
                var keyIndex = splitAccess.IndexOf(key);

                if (key.StartsWith("link="))
                {
                    var linkValue = splitAccess[keyIndex].Substring(5, splitAccess[keyIndex].Length - 6);
                    var fingerPrintValue = splitAccess[keyIndex + 1].Substring(12, splitAccess[keyIndex + 1].Length - 13);
                    var value = splitAccess[keyIndex + 2].Substring(6, splitAccess[keyIndex + 2].Length - 7);
                    var id = splitAccess[keyIndex + 3].Substring(3, splitAccess[keyIndex + 3].Length - 4);

                    AccessLinks.Add(new AccountAccessLink(linkValue, fingerPrintValue, value, id));
                }
            }
        }

        #endregion

        private void ValidateAuthenticity()
        {
            // TODO: Implement
        }

        public override string ToString()
        {
            return $"All headers\n\n{base.ToString()}\nEnvelope headers\n\n{EnvelopeHeaderStore.ToString()}";
        }
    }
}
