namespace OpenEmail.Domain.Models.MessageEnvelope
{
    public record EnvelopeFile(string FileName, string MimeType, DateTimeOffset ModifiedAt, long Size, string Id, string Part);
}
