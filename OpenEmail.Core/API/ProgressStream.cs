using OpenEmail.Domain.Models.Messages;

namespace OpenEmail.Core.API
{
    public class ProgressStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly long _totalBytes;
        private readonly AttachmentProgress _attachmentProgress;

        public ProgressStream(Stream innerStream, long totalBytes, AttachmentProgress attachmentProgress)
        {
            _innerStream = innerStream;
            _totalBytes = totalBytes;
            _attachmentProgress = attachmentProgress;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            if (bytesRead > 0)
            {
                // Report before. We count bytes before we read them.
                _attachmentProgress.BytesDownloaded += bytesRead;
            }
            return bytesRead;
        }

        // unused
        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }
        public override void Flush() => _innerStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
    }

}
