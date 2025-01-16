using System.Security.Cryptography;
using System.Text;
using OpenEmail.Domain.Models.Accounts;
using Sodium;
using static Sodium.Utilities;

namespace OpenEmail.Domain
{
    public static class CryptoUtils
    {
        private static Random Randomizer = new Random();

        public static string Base64Encode(byte[] data) => BinaryToBase64(data, Base64Variant.Original);

        public static byte[] Base64Decode(string encodedData) => Base64ToBinary(encodedData, " \n", Base64Variant.Original);

        public static (string privateKey, string publicKey, string keyId) GenerateEncryptionKeys()
        {
            var keyPair = PublicKeyBox.GenerateKeyPair();
            var encodedPrivateKey = Base64Encode(keyPair.PrivateKey);
            var encodedPublicKey = Base64Encode(keyPair.PublicKey);
            var keyId = GenerateRandomString(4);
            return (encodedPrivateKey, encodedPublicKey, keyId);
        }

        public static (string privateKey, string publicKey) GenerateSigningKeys()
        {
            var keyPair = PublicKeyAuth.GenerateKeyPair();
            var encodedPrivateKey = Base64Encode(keyPair.PrivateKey);
            var encodedPublicKey = Base64Encode(keyPair.PublicKey);

            return (encodedPrivateKey, encodedPublicKey);
        }

        public static string SignData(byte[] publicKey, byte[] privateKey, byte[] data)
        {
            var signature = PublicKeyAuth.SignDetached(data, privateKey) ?? throw new Exception("Signature mismatch"); ;

            return Convert.ToBase64String(signature);
        }

        public static string GenerateMessageId(UserAddress userAddress)
        {
            var randomString = GenerateRandomString(24);
            var rawId = $"{randomString}\\{userAddress.HostPart}\\{userAddress.LocalPart}";
            var idBytes = Encoding.UTF8.GetBytes(rawId);

            var sha256 = Sha256Sum(idBytes);

            return sha256.Item1;
        }

        public static string GenerateRandomString(int length)
        {
            const string letters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var random = new Random();
            var stringBuilder = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                int randomIndex = random.Next(letters.Length);
                stringBuilder.Append(letters[randomIndex]);
            }

            return stringBuilder.ToString();
        }

        public static byte[] GenerateRandomBytes(int length)
        {
            byte[] randomBytes = new byte[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            return randomBytes;
        }

        public static byte[] DecryptSymmetric(byte[] data, byte[] accessKey)
        {
            // First 24 bytes are the nonce.
            // The rest is the encrypted data.

            var nonce = data.Take(24).ToArray();
            data = data.Skip(24).ToArray();

            return SecretAeadXChaCha20Poly1305.Decrypt(data, nonce, accessKey);
        }

        public static byte[] EncryptSymmetric(byte[] data, byte[] accessKey)
        {
            var nonce = SecretAeadXChaCha20Poly1305.GenerateNonce();

            var detachedEncrypted = SecretAeadXChaCha20Poly1305.Encrypt(data, nonce, accessKey);

            // Switch to combined by adding nonce in the beginning
            return nonce.Concat(detachedEncrypted).ToArray();
        }

        public static string DecryptSymmetricAsString(byte[] data, byte[] accessKey)
            => Encoding.UTF8.GetString(DecryptSymmetric(data, accessKey));

        public static byte[] EncryptAnonymous(byte[] data, byte[] publicKey)
        {
            return SealedPublicKeyBox.Create(data, publicKey);
        }

        public static byte[] DecryptAnonymous(string cipherText, byte[] privateKey, byte[] publicKey)
        {
            if (privateKey.Length != 32 || publicKey.Length != 32)
            {
                throw new ArgumentException("Both private and public keys must be 32 bytes long.");
            }

            // Decode the base64 encoded ciphertext
            byte[] cipherData;
            try
            {
                cipherData = Convert.FromBase64String(cipherText);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Bad cipherText format: not valid Base64.");
            }

            return SealedPublicKeyBox.Open(cipherData, privateKey, publicKey);
        }

        public static byte[] GenerateRandomStringBytes(int length)
        {
            var randomString = GenerateRandomString(length);
            return Encoding.UTF8.GetBytes(randomString);
        }

        public static (string, byte[]) Sha256Sum(byte[] content)
        {
            using var sha256 = SHA256.Create();
            var digest = sha256.ComputeHash(content);

            var hashString = BitConverter.ToString(digest).Replace("-", "").ToLower();

            return (hashString, digest);
        }
    }
}
