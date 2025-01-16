namespace OpenEmail.Domain.Models.Accounts
{
    public record AccountAccessLink(string Link, string Fingerprint, string Value, string Id);
}
