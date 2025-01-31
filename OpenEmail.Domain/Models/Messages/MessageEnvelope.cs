using System.Text;
using OpenEmail.Domain.Entities;
using OpenEmail.Domain.Models.MessageEnvelope;

namespace OpenEmail.Domain.Models.Messages
{
    /// <summary>
    /// Envelope for root messages that has a string content.
    /// </summary>
    public class MessageEnvelope
    {
        public EnvelopeBase Envelope { get; private set; }
        public string Content { get; }
        public EnvelopeFileCollection Files { get; }

        public static MessageEnvelope FromEnvelope(EnvelopeBase envelope, byte[] messageResponseContent)
            => new MessageEnvelope(envelope, messageResponseContent);

        protected MessageEnvelope(EnvelopeBase envelope, byte[] messageResponseContent)
        {
            Envelope = envelope;

            if (envelope.IsBroadcastEnvelope)
            {
                // Broadcasts evenlopes have their content unencrypted.
                Content = Encoding.UTF8.GetString(messageResponseContent);
            }
            else
            {
                Content = CryptoUtils.DecryptSymmetricAsString(messageResponseContent, Envelope.AccessKey);
            }

            // Parse Files header if any.

            if (Envelope.EnvelopeHeaderStore.HasKey("files"))
            {
                Files = [];

                var splittedFiles = Envelope.EnvelopeHeaderStore.GetData<string>("files").Split(',');

                foreach (var file in splittedFiles)
                {
                    var filesDictionary = KeyValueDataStore.ParseAttributes(file);

                    var fileName = filesDictionary["name"];
                    var mimeType = filesDictionary["type"];
                    var modifiedAt = DateTimeOffset.Parse(filesDictionary["modified"]);
                    var size = long.Parse(filesDictionary["size"]);
                    var id = filesDictionary["id"];
                    var part = filesDictionary["part"];

                    Files.Add(new EnvelopeFile(fileName, mimeType, modifiedAt, size, id, part));
                }
            }
        }

        protected MessageEnvelope(EnvelopeBase envelope, string content)
        {
            Envelope = envelope;
            Content = content;
        }

        public Message AsEntity()
        {
            var msg = new Message()
            {
                Id = Guid.NewGuid(),
                AccountId = Envelope.AccountId,
                CreatedAt = DateTimeOffset.UtcNow,
                Author = Envelope.EnvelopeHeaderStore.GetData<string>("author"),
                Subject = Envelope.EnvelopeHeaderStore.GetData<string>("subject"),
                IsBroadcast = !Envelope.EnvelopeHeaderStore.HasKey("readers"), // Broadcast messages don't have readers
                Category = Envelope.EnvelopeHeaderStore.GetData<string>("category"),
                IsRead = false,
                Readers = Envelope.EnvelopeHeaderStore.GetData<string>("readers"),
                AccessKey = Envelope.AccessKey != null ? Convert.ToBase64String(Envelope.AccessKey) : string.Empty,
                DeletedAt = null,
                ReceivedAt = Envelope.EnvelopeHeaderStore.GetData<DateTimeOffset>("date"),
                EnvelopeId = Envelope.Id,
                Size = Envelope.EnvelopeHeaderStore.GetData<long>("size"),
                Body = Content,
                SubjectId = Envelope.EnvelopeHeaderStore.GetData<string>("subject-id")
            };

            return msg;
        }
    }
}
