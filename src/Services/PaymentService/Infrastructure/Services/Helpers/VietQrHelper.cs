using System.Text;

public static class VietQrHelper
{
    // MBBank BIN: 970422
    private const string MBBANK_BIN = "970422";
    private const string MERCHANT_ACCOUNT_FIELD_ID = "38";
    private const string MERCHANT_GUID = "A000000727"; 
    private const string QR_PAY_BY_ACCOUNT = "QRIBFTTA"; 

    public static string GenerateVietQrPayload(string accountNumber, string accountName, long amount, string description)
    {
        var payload = new StringBuilder();

        payload.Append(BuildTLV("00", "01"));
        payload.Append(BuildTLV("01", "12"));

        string merchantInfo = BuildMerchantInfo(accountNumber, accountName, description);
        payload.Append(BuildTLV(MERCHANT_ACCOUNT_FIELD_ID, merchantInfo));

        payload.Append(BuildTLV("53", "704"));
        payload.Append(BuildTLV("54", amount.ToString()));
        payload.Append(BuildTLV("58", "VN"));
        payload.Append(BuildTLV("62", BuildAdditionalData(description)));

        string dataToHash = payload.ToString() + "6304";
        string crc = ComputeCRC(dataToHash);
        payload.Append(BuildTLV("63", crc));

        return payload.ToString();
    }

    private static string BuildMerchantInfo(string accountNumber, string accountName, string description)
    {
        var merchantInfo = new StringBuilder();
        merchantInfo.Append(BuildTLV("00", MERCHANT_GUID));
        
        var consumerInfo = new StringBuilder();
        consumerInfo.Append(BuildTLV("00", MBBANK_BIN)); // Bank BIN
        consumerInfo.Append(BuildTLV("01", accountNumber)); // Account Number
        merchantInfo.Append(BuildTLV("01", consumerInfo.ToString()));

        merchantInfo.Append(BuildTLV("02", QR_PAY_BY_ACCOUNT));
        
        return merchantInfo.ToString();
    }

    private static string BuildAdditionalData(string description)
    {
        return BuildTLV("08", description);
    }

    private static string BuildTLV(string id, string value)
    {
        return $"{id}{value.Length:D2}{value}";
    }

    private static string ComputeCRC(string data)
    {
        ushort crc = 0xFFFF;
        foreach (char c in data)
        {
            crc ^= (ushort)(c << 8);
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc = (ushort)(crc << 1);
            }
        }
        return $"{crc:X4}"; 
    }
}