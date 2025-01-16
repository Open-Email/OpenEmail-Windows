using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Cryptography;
using OpenEmail.Domain.Models.Discovery;
using Sodium;

namespace OpenEmail.Domain.Models.Profile
{
    public record AccountProfile(Account Account, ProfileData ProfileData, byte[] PrivateEncryptionKey, byte[] PrivateSigningKey, DiscoveryHost Host)
    {
        private AuthorizationUser _authUser;
        private AuthorizationUser AuthUser
        {
            get
            {
                _authUser ??= new AuthorizationUser(
                    Utilities.Base64ToBinary(Account.PublicSigningKey, null),
                    Utilities.Base64ToBinary(PrivateSigningKeyBase64, null));

                return _authUser;
            }
        }

        public string DisplayName => Account.DisplayName;
        public string Address => $"{Account.LocalPart}@{Account.HostPart}";
        public UserAddress UserAddress => UserAddress.CreateFromAddress(Address);
        public Nonce Nonce => new(AuthUser, Host);

        // Usefull keys for encryption and decryption
        public string PrivateSigningKeyBase64 { get; } = CryptoUtils.Base64Encode(PrivateSigningKey);
        public string PrivateEncryptionKeyBase64 { get; } = CryptoUtils.Base64Encode(PrivateEncryptionKey);

        public byte[] PublicEncryptionKey { get; } = CryptoUtils.Base64Decode(Account.PublicEncryptionKey);
        public byte[] PublicSigningKey { get; } = CryptoUtils.Base64Decode(Account.PublicSigningKey);
    }
}
