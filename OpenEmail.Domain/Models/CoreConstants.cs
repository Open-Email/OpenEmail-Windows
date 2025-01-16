namespace OpenEmail.Domain.Models
{
    public static class CoreConstants
    {
        public static string ISOFormat = "yyyy-MM-ddTHH:mm:ssZ";

        // Headers

        public static string HEADER_CONTENT_MESSAGE_ID = "id";
        public static string HEADER_CONTENT_AUTHOR = "author";
        public static string HEADER_CONTENT_DATE = "date";
        public static string HEADER_CONTENT_SIZE = "size";
        public static string HEADER_CONTENT_CHECKSUM = "checksum";
        public static string HEADER_CONTENT_FILE = "file";
        public static string HEADER_CONTENT_SUBJECT = "subject";
        public static string HEADER_CONTENT_SUBJECT_ID = "subject-id";
        public static string HEADER_CONTENT_PARENT_ID = "parent-id";
        public static string HEADER_CONTENT_FILES = "files";
        public static string HEADER_CONTENT_CATEGORY = "category";
        public static string HEADER_CONTENT_READERS = "readers";

        public static string HEADER_MESSAGE_ID = "message-id";
        public static string HEADER_MESSAGE_STREAM = "    ";
        public static string HEADER_MESSAGE_ACCESS = "message-access";
        public static string HEADER_MESSAGE_HEADERS = "message-headers";
        public static string HEADER_MESSAGE_ENVELOPE_CHECKSUM = "message-checksum";
        public static string HEADER_MESSAGE_ENVELOPE_SIGNATURE = "message-signature";
        public static string HEADER_MESSAGE_ENCRYPTION = "message-encryption";

        public static string[] CHECKSUM_HEADERS = new[]
        {
            HEADER_MESSAGE_ID,
            HEADER_MESSAGE_STREAM,
            HEADER_MESSAGE_ACCESS,
            HEADER_MESSAGE_HEADERS,
            HEADER_MESSAGE_ENCRYPTION,
        };
    }
}
