using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.MessageEnvelope;

namespace OpenEmail.Domain.Models.Messages
{
    /// <summary>
    /// Envelope for attachment metadata.
    /// </summary>
    public class AttachmentEnvelope
    {
        public EnvelopeBase Envelope { get; set; }
        public string ParentId { get; }

        public AttachmentEnvelope(EnvelopeBase envelope)
        {
            Envelope = envelope;
            ParentId = Envelope.EnvelopeHeaderStore.GetData<string>("parent-id");

            if (!Envelope.EnvelopeHeaderStore.HasKey("parent-id"))
                throw new ArgumentException("Attachment envelope must have parent-id set.");
        }

        public MessageAttachment AsEntity()
        {
            return new MessageAttachment()
            {
                Id = Envelope.Id,
                ParentId = ParentId,
                Size = Envelope.EnvelopeHeaderStore.GetData<long>("size"),
                MimeType = Envelope.EnvelopeHeaderStore.GetData<string>("type"),
                FileName = Envelope.EnvelopeHeaderStore.GetData<string>("name"),
                AccessKey = Convert.ToBase64String(Envelope.AccessKey)
            };
        }
    }


}
