namespace OpenEmail.Core.Helpers
{
    public static class ByteHelper
    {
        public const int MaxPartSizeMB = 64;

        public static List<byte[]> SplitByteArray(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (MaxPartSizeMB <= 0)
                throw new ArgumentOutOfRangeException(nameof(MaxPartSizeMB), "Max part size must be greater than 0.");

            int maxPartSizeBytes = MaxPartSizeMB * 1024 * 1024;
            int totalParts = (int)Math.Ceiling((double)data.Length / maxPartSizeBytes);

            List<byte[]> parts = new List<byte[]>(totalParts);

            for (int i = 0; i < totalParts; i++)
            {
                int startIndex = i * maxPartSizeBytes;
                int partLength = Math.Min(maxPartSizeBytes, data.Length - startIndex);

                byte[] part = new byte[partLength];
                Array.Copy(data, startIndex, part, 0, partLength);

                parts.Add(part);
            }

            return parts;
        }

        public static Dictionary<int, long> GetFilePartSizes(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            long fileSize = new FileInfo(filePath).Length;
            long maxPartSizeBytes = MaxPartSizeMB * 1024L * 1024L;
            int totalParts = (int)Math.Ceiling((double)fileSize / maxPartSizeBytes);

            var partSizes = new Dictionary<int, long>();

            for (int i = 0; i < totalParts; i++)
            {
                long remainingBytes = fileSize - (i * maxPartSizeBytes);
                long partSize = Math.Min(maxPartSizeBytes, remainingBytes);
                partSizes.Add(i + 1, partSize);
            }

            return partSizes;
        }
    }
}
