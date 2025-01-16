namespace OpenEmail.Core.Helpers
{
    public static class ByteHelper
    {
        public static List<byte[]> SplitByteArray(byte[] data, int maxPartSizeMB = 64)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (maxPartSizeMB <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxPartSizeMB), "Max part size must be greater than 0.");

            int maxPartSizeBytes = maxPartSizeMB * 1024 * 1024;
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
    }
}
