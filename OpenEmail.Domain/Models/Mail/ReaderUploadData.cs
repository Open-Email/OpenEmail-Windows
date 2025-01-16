namespace OpenEmail.Domain.Models.Mail
{
    /// <summary>
    /// Small portion of the profile data for sending messages.
    /// </summary>
    public record ReaderUploadData(string Address, string EncryptionKey, string SigningKey, string EncryptionKeyId);
}
