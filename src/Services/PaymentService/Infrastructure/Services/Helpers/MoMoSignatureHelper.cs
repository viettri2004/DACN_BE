using System.Security.Cryptography;
using System.Text;

public static class MoMoSignatureHelper
{
    public static string GenerateSignature(string text, string key)
    {
        UTF8Encoding encoding = new UTF8Encoding();

        Byte[] textBytes = encoding.GetBytes(text);
        Byte[] keyBytes = encoding.GetBytes(key);

        Byte[] hashBytes;

        using (HMACSHA256 hash = new HMACSHA256(keyBytes))
            hashBytes = hash.ComputeHash(textBytes);

        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}