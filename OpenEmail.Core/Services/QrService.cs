using OpenEmail.Contracts.Services;
using QRCoder;

namespace OpenEmail.Core.Services
{
    public class QrService : IQrService
    {
        public byte[] GetQrImage(string privateEncryptionKey, string privateSigningKey)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode("The payload aka the text which should be encoded.", QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);

            return qrCode.GetGraphic(20);
        }
    }
}
