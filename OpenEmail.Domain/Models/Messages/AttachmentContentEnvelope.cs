using OpenEmail.Domain.Models.MessageEnvelope;

namespace OpenEmail.Domain.Models.Messages
{
    /// <summary>
    /// Envelope for attachments that has the content as well as metadata.
    /// </summary>
    public class AttachmentContentEnvelope : AttachmentEnvelope
    {
        public byte[] Content { get; set; }
        public AttachmentContentEnvelope(EnvelopeBase envelope, byte[] content) : base(envelope)
        {
            Content = CryptoUtils.DecryptSymmetric(content, Envelope.AccessKey);
        }
    }
}
