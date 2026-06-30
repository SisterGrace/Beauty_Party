using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class CryptoPrefs
{
    // 128-bit key (16 bytes) - 取自原密钥前16字符
    private static readonly string sKEY = "ZTdkvTNmaBE2NTM3";
    // 128-bit IV (16 bytes) - Base64编码
    private static readonly string sIV = "4rZtmENfa/PpeJ86qU3gyA==";

    private const int TrueValue = 1;
    private const int FalseValue = 0;

    // ---------- Bool ----------
    public static void SetBool(string key, bool value)
    {
        SetInt(key, value ? TrueValue : FalseValue);
    }

    public static bool GetBool(string key, bool defaultValue = false)
    {
        int value = GetInt(key, defaultValue ? TrueValue : FalseValue);
        return value == TrueValue;
    }

    // ---------- Int ----------
    public static void SetInt(string key, int val)
    {
        PlayerPrefs.SetString(GetHash(key), Encrypt(val.ToString()));
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
        string str = GetString(key, defaultValue.ToString());
        return int.TryParse(str, out int result) ? result : defaultValue;
    }

    // ---------- Float ----------
    public static void SetFloat(string key, float val)
    {
        PlayerPrefs.SetString(GetHash(key), Encrypt(val.ToString()));
    }

    public static float GetFloat(string key, float defaultValue = 0f)
    {
        string str = GetString(key, defaultValue.ToString());
        return float.TryParse(str, out float result) ? result : defaultValue;
    }

    // ---------- String ----------
    public static void SetString(string key, string val)
    {
        if (val == null) val = "";
        PlayerPrefs.SetString(GetHash(key), Encrypt(val));
    }

    public static string GetString(string key, string defaultValue = "")
    {
        string hashKey = GetHash(key);
        if (!PlayerPrefs.HasKey(hashKey))
            return defaultValue;

        string encrypted = PlayerPrefs.GetString(hashKey);
        return Decrypt(encrypted);
    }

    // ---------- Management ----------
    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(GetHash(key));
    }

    public static void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(GetHash(key));
    }

    public static void DeleteAll()
    {
        PlayerPrefs.DeleteAll();
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    // ---------- Cryptography ----------
    private static string Encrypt(string rawString)
    {
        if (string.IsNullOrEmpty(rawString))
            return "";

        byte[] keyBytes = Encoding.UTF8.GetBytes(sKEY);
        byte[] ivBytes = Convert.FromBase64String(sIV);

        using (var aes = new RijndaelManaged())
        {
            aes.KeySize = 128;          // 128位密钥
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.Zeros;
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                byte[] plain = Encoding.UTF8.GetBytes(rawString);
                cs.Write(plain, 0, plain.Length);
                cs.FlushFinalBlock();
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    private static string Decrypt(string encString)
    {
        if (string.IsNullOrEmpty(encString))
            return "";

        byte[] keyBytes = Encoding.UTF8.GetBytes(sKEY);
        byte[] ivBytes = Convert.FromBase64String(sIV);
        byte[] cipher = Convert.FromBase64String(encString);

        using (var aes = new RijndaelManaged())
        {
            aes.KeySize = 128;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.Zeros;
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using (var decryptor = aes.CreateDecryptor())
            using (var ms = new MemoryStream(cipher))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var resultMs = new MemoryStream())
            {
                cs.CopyTo(resultMs);   // 完整读取（自动循环）
                byte[] decrypted = resultMs.ToArray();
                string raw = Encoding.UTF8.GetString(decrypted);
                return raw.TrimEnd('\0'); // 去除零填充
            }
        }
    }

    private static string GetHash(string key)
    {
        using (var md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
            var sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }
    }
}

    //  //默认密钥向量
    //  private static byte[] Keys = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
    //  public static string keyss = "1234567z";
    //  /// <summary>
    //  /// DES加密字符串
    //  /// </summary>
    //  /// <param name="encryptString">待加密的字符串</param>
    //  /// <param name="encryptKey">加密密钥,要求为8位</param>
    //  /// <returns>加密成功返回加密后的字符串，失败返回源串</returns>
    //  public static string EncryptDES(string encryptString, string encryptKey)
    //  {
    //      try
    //      {
    //          byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
    //          byte[] rgbIV = Keys;
    //          byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);
    //          DESCryptoServiceProvider dCSP = new DESCryptoServiceProvider();
    //          MemoryStream mStream = new MemoryStream();
    //          CryptoStream cStream = new CryptoStream(mStream, dCSP.CreateEncryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
    //          cStream.Write(inputByteArray, 0, inputByteArray.Length);
    //          cStream.FlushFinalBlock();
    //          return Convert.ToBase64String(mStream.ToArray());
    //      }
    //      catch
    //      {
    //          return encryptString;
    //      }
    //  }/// <summary>
    //  /// DES解密字符串
    //  /// </summary>
    //  /// <param name="decryptString">待解密的字符串</param>
    //  /// <param name="decryptKey">解密密钥,要求为8位,和加密密钥相同</param>
    //  /// <returns>解密成功返回解密后的字符串，失败返源串</returns>
    //  public static string DecryptDES(string decryptString, string decryptKey)
    //  {
    //      try
    //      {
    //          byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey);
    //          byte[] rgbIV = Keys;
    //          byte[] inputByteArray = Convert.FromBase64String(decryptString);
    //          DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();
    //          MemoryStream mStream = new MemoryStream();
    //          CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
    //          cStream.Write(inputByteArray, 0, inputByteArray.Length);
    //          cStream.FlushFinalBlock();
    //          return Encoding.UTF8.GetString(mStream.ToArray());
    //      }
    //      catch
    //      {
    //          return decryptString;
    //      }
    //  }
