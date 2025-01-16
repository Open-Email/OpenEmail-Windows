using EmailValidation;
using Sodium;

namespace OpenEmail.Domain.Models.Accounts
{
    public record AuthorizationUser(byte[] PublicSigningKey, byte[] PrivateSigningKey);

    public record LocalUser(byte[] PublicSigningKey,
                            byte[] PrivateSigningKey,
                            UserAddress Address,
                            string Name,
                            byte[] PrivateEncryptionKey,
                            string PrivateEncryptionKeyBase64,
                            byte[] PublicEncryptionKey,
                            string PublicEncryptionKeyBase64,
                            string PublicEncryptionKeyId,
                            string PrivateSigningKeyBase64,
                            string PublicSigningKeyBase64,
                            string PublicSigningKeyFingerprint) : AuthorizationUser(PublicSigningKey, PrivateSigningKey)
    {
        public LocalUser(string name,
                         UserAddress address,
                         string privateEncryptionKeyBase64,
                         string publicEncryptionKeyBase64,
                         string publicEncryptionKeyId,
                         string privateSigningKeyBase64,
                         string publicSigningKeyBase64) : this(default, default, address, name, default, privateEncryptionKeyBase64, default, publicEncryptionKeyBase64, publicEncryptionKeyId, privateSigningKeyBase64, publicSigningKeyBase64, default)
        {
            byte[] privateEncryptionKey = Utilities.Base64ToBinary(privateEncryptionKeyBase64, null);
            byte[] publicEncryptionKey = Utilities.Base64ToBinary(publicEncryptionKeyBase64, null);
            byte[] privateSigningKey = Utilities.Base64ToBinary(privateSigningKeyBase64, null);
            byte[] publicSigningKey = Utilities.Base64ToBinary(publicSigningKeyBase64, null);

            if (privateEncryptionKey == null || publicEncryptionKey == null || privateSigningKey == null || publicSigningKey == null)
                throw new Exception("Invalid key provided.");

            if (!EmailValidator.Validate(address.FullAddress)) throw new Exception("Invalid e-mail address.");
            PrivateEncryptionKey = privateEncryptionKey;
            PublicEncryptionKey = publicEncryptionKey;
            PrivateSigningKey = privateSigningKey;
            PublicSigningKey = publicSigningKey;
        }
    }
}
