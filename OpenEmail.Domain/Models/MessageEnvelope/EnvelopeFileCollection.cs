using OpenEmail.Domain.Entities;

namespace OpenEmail.Domain.Models.MessageEnvelope
{
    public class EnvelopeFileCollection : List<EnvelopeFile>
    {
        public IEnumerable<MessageAttachment> AsEntity(string parentId)
        {
            foreach (var file in this)
            {
                // Create attachment parts.

                var attachment = new MessageAttachment()
                {
                    FileName = file.FileName,
                    MimeType = file.MimeType,
                    Size = file.Size,
                    Id = file.Id,
                    Part = int.Parse(file.Part.Split('/')[0]),
                    ParentId = parentId
                };

                yield return attachment;
            }
        }
    }
}
