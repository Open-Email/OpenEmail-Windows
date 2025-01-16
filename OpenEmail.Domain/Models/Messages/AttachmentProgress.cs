namespace OpenEmail.Domain.Models.Messages
{
    public record AttachmentProgress(long TotalBytes)
    {
        public event EventHandler ProgressChanged;
        private long bytesDownloaded;

        public long BytesDownloaded
        {
            get { return bytesDownloaded; }
            set
            {
                var oldProgress = Progress;

                bytesDownloaded = value;

                if (oldProgress != Progress)
                {
                    ProgressChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private AttachmentDownloadStatus status;

        public AttachmentDownloadStatus Status
        {
            get { return status; }
            set
            {
                status = value;
                ProgressChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int Progress
        {
            get
            {
                if (TotalBytes == 0) return 0;

                var progress = (double)((BytesDownloaded * 100) / TotalBytes);

                return (int)progress;
            }
        }
    }
}
