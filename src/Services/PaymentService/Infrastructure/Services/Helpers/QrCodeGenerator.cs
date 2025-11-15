using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

public static class QrCodeGenerator
{
    public static string GenerateQrCodeBase64(string payload)
    {
        using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
        using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q))
        using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
        {
            byte[] qrCodeAsPng = qrCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeAsPng)}";
        }
    }
}