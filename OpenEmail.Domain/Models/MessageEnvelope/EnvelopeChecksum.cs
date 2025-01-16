namespace OpenEmail.Domain.Models.MessageEnvelope
{
    public record EnvelopeChecksum(string Algorithm, string Value, string Order);
}
