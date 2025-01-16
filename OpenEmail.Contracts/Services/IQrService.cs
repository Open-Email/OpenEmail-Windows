namespace OpenEmail.Contracts.Services
{
    public interface IQrService
    {
        byte[] GetQrImage(string privateEncryptionKey, string privateSigningKey);
    }
}
