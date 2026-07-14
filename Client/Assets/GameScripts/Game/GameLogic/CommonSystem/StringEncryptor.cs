using System;
using System.Text;

/// <summary>
/// XOR + Base64 字符串加解密工具类
/// 用于加密CDN地址等敏感字符串
/// </summary>
public static class StringEncryptor
{
    // 密钥（修改后需重新加密所有字符串）
    private const string SECRET_KEY = "MyGameSecretKey2024!";

    public static string EncryptToBase64(string plainText)
    {
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] keyBytes = Encoding.UTF8.GetBytes(SECRET_KEY);
        byte[] encrypted = new byte[plainBytes.Length];
        for (int i = 0; i < plainBytes.Length; i++)
            encrypted[i] = (byte)(plainBytes[i] ^ keyBytes[i % keyBytes.Length]);
        return Convert.ToBase64String(encrypted);
    }

    public static string DecryptFromBase64(string base64Text)
    {
        byte[] encrypted = Convert.FromBase64String(base64Text);
        byte[] keyBytes = Encoding.UTF8.GetBytes(SECRET_KEY);
        byte[] decrypted = new byte[encrypted.Length];
        for (int i = 0; i < encrypted.Length; i++)
            decrypted[i] = (byte)(encrypted[i] ^ keyBytes[i % keyBytes.Length]);
        return Encoding.UTF8.GetString(decrypted);
    }
}
