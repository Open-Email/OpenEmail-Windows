using System.Text;
using OpenEmail.Domain.Models.Accounts;
using OpenEmail.Domain.Models.Discovery;
using Sodium;

namespace OpenEmail.Domain.Models.Cryptography
{
    public record Nonce(AuthorizationUser AuthorizationUser, DiscoveryHost DiscoveryHost)
    {
        public string GetSignAuth()
        {
            var randomToken = CryptoUtils.GenerateRandomString(CryptoConstants.NonceTokenLength);
            var signature = CryptoUtils.SignData(AuthorizationUser.PublicSigningKey, AuthorizationUser.PrivateSigningKey, Encoding.ASCII.GetBytes(DiscoveryHost.AgentUrl + randomToken));

            var pairs = new List<string>
            {
                string.Join(CryptoConstants.HeaderKeyValueSeparator, CryptoConstants.NONCE_HEADER_VALUE_KEY, randomToken),
                string.Join(CryptoConstants.HeaderKeyValueSeparator, CryptoConstants.NONCE_HEADER_VALUE_HOST, DiscoveryHost.AgentUrl),
                string.Join(CryptoConstants.HeaderKeyValueSeparator, CryptoConstants.NONCE_HEADER_ALGORITHM_KEY, CryptoConstants.SIGNING_ALGORITHM),
                string.Join(CryptoConstants.HeaderKeyValueSeparator, CryptoConstants.NONCE_HEADER_SIGNATURE_KEY, signature),
                string.Join(CryptoConstants.HeaderKeyValueSeparator, CryptoConstants.NONCE_HEADER_PUBKEY_KEY, Utilities.BinaryToBase64(AuthorizationUser.PublicSigningKey)),
            };

            return string.Join(CryptoConstants.HeaderFieldSeparator, pairs);
        }
    }
}
