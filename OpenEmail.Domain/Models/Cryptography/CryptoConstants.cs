namespace OpenEmail.Domain.Models.Cryptography
{
    public static class CryptoConstants
    {
        private static string FIELD_SEPARATOR = "$";

        public static string ANONYMOUS_ENCRYPTION_CIPHER = "curve25519xsalsa20poly1305";
        public static string CHECKSUM_ALGORITHM = "sha256";
        public static string SIGNING_ALGORITHM = "ed25519";
        public static string SYMMETRIC_CIPHER = "xchacha20poly1305";
        public static string SYMMETRIC_FILE_CIPHER = "secretstream_xchacha20poly1305";

        public static int SYMMETRIC_FILE_CIPHER_HEADER_SIZE = 24;
        public static int SYMMETRIC_FILE_CIPHER_OVERHEAD_SIZE = 17;

        public const string NONCE_SCHEME = "SOTN";

        public static int NonceTokenLength = 32;
        public static string HeaderFieldSeparator = "; ";
        public static string HeaderKeyValueSeparator = "=";
        public static string NONCE_HEADER_VALUE_HOST = "host";
        public static string NONCE_HEADER_VALUE_KEY = "value";
        public static string NONCE_HEADER_ALGORITHM_KEY = "algorithm";
        public static string NONCE_HEADER_SIGNATURE_KEY = "signature";
        public static string NONCE_HEADER_PUBKEY_KEY = "key";
    }
}
