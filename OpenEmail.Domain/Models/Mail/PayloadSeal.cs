namespace OpenEmail.Domain.Models.Mail
{
    public record PayloadSeal(string Algorithm, bool Stream = false, int ChunkSize = 0, string OriginalHeaderValue = "")
    {
        public string AsHeader()
        {
            if (Stream)
            {
                if (string.IsNullOrEmpty(Algorithm) || ChunkSize == 0) return string.Empty;

                return $"algorithm={Algorithm}; chunk-size={ChunkSize}";
            }

            return "algorithm=" + Algorithm;
        }
    }
}
