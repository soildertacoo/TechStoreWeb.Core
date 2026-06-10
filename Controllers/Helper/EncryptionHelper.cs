using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class EncryptionHelper
{
    //ép kiểu sang 32byte
     private static byte[] GetValidAesKey(string keyString)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
        }
    }
    // HÀM MÃ HÓA: Trả về thẳng mảng byte[] (VARBINARY)
    public static byte[] EncryptStringToBytes(string plainText, string keyString)
    {
        if (string.IsNullOrEmpty(plainText)) return null;

        byte[] key = GetValidAesKey(keyString);

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.GenerateIV();
            byte[] iv = aesAlg.IV;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                // Ghi IV vào đầu để sau này lấy ra giải mã
                msEncrypt.Write(iv, 0, iv.Length); 
                
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                
                // Trả thẳng mảng Byte về
                return msEncrypt.ToArray(); 
            }
        }
    }
    

    // HÀM GIẢI MÃ: Nhận vào mảng byte[] (VARBINARY) và trả ra chữ gốc
    public static string DecryptStringFromBytes(byte[] cipherData, string keyString)
    {
        if (cipherData == null || cipherData.Length == 0) return null;

        byte[] key = GetValidAesKey(keyString);

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;

            // Bóc 16 bytes đầu tiên ra để làm IV
            byte[] iv = new byte[16];
            Array.Copy(cipherData, 0, iv, 0, iv.Length);
            aesAlg.IV = iv;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            // Đọc phần dữ liệu còn lại (bỏ qua 16 bytes IV)
            using (MemoryStream msDecrypt = new MemoryStream(cipherData, iv.Length, cipherData.Length - iv.Length))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
    }
}