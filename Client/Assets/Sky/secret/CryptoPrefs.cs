using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;


public class CryptoPrefs
{
    private static string sKEY = "ZTdkvTNmaBE2NTM3nWM0NDFhNRdzNzU1";
    private static string sIV = "4rZtmENfa/PpeJ86qU3gyA==";
    public static void SetInt(string key, int val)
    {
        PlayerPrefs.SetString(CryptoPrefs.GetHash(key), CryptoPrefs.Encrypt(val.ToString()));
    }
    public static int GetInt(string key, int defaultValue = 0)
    {
        string @string = CryptoPrefs.GetString(key, defaultValue.ToString());
        int result = defaultValue;
        int.TryParse(@string, out result);
        return result;
    }
    public static void SetFloat(string key, float val)
    {
        PlayerPrefs.SetString(CryptoPrefs.GetHash(key), CryptoPrefs.Encrypt(val.ToString()));
    }
    public static float GetFloat(string key, float defaultValue = 0f)
    {
        string @string = CryptoPrefs.GetString(key, defaultValue.ToString());
        float result = defaultValue;
        float.TryParse(@string, out result);
        return result;
    }
    public static void SetString(string key, string val)
    {
        PlayerPrefs.SetString(CryptoPrefs.GetHash(key), CryptoPrefs.Encrypt(val));
    }
    public static string GetString(string key, string defaultValue = "")
    {
        string text = defaultValue;
        string @string = PlayerPrefs.GetString(CryptoPrefs.GetHash(key), defaultValue.ToString());
        if (!text.Equals(@string))
        {
            text = CryptoPrefs.Decrypt(@string);
        }
        return text;
    }
    public static bool HasKey(string key)
    {
        string hash = CryptoPrefs.GetHash(key);
        return PlayerPrefs.HasKey(hash);
    }
    public static void DeleteKey(string key)
    {
        string hash = CryptoPrefs.GetHash(key);
        PlayerPrefs.DeleteKey(hash);
    }
    public static void DeleteAll()
    {
        PlayerPrefs.DeleteAll();
    }
    public static void Save()
    {
        PlayerPrefs.Save();
    }
    private static string Decrypt(string encString)
    {
        RijndaelManaged rijndaelManaged = new RijndaelManaged
        {
            Padding = PaddingMode.Zeros,
            Mode = CipherMode.CBC,
            KeySize = 128,
            BlockSize = 128
        };
        byte[] bytes = Encoding.UTF8.GetBytes(CryptoPrefs.sKEY);
        byte[] rgbIV = Convert.FromBase64String(CryptoPrefs.sIV);
        ICryptoTransform transform = rijndaelManaged.CreateDecryptor(bytes, rgbIV);
        byte[] array = Convert.FromBase64String(encString);
        byte[] array2 = new byte[array.Length];
        MemoryStream stream = new MemoryStream(array);
        CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read);
        cryptoStream.Read(array2, 0, array2.Length);
        //Debug.Log("Encoding.UTF8.GetString(array2).TrimEnd(new char[1] " + Encoding.UTF8.GetString(array2).TrimEnd(new char[1]));
        return Encoding.UTF8.GetString(array2).TrimEnd(new char[1]);
    }
    private static string Encrypt(string rawString)
    {
        RijndaelManaged rijndaelManaged = new RijndaelManaged
        {
            Padding = PaddingMode.Zeros,
            Mode = CipherMode.CBC,
            KeySize = 128,
            BlockSize = 128
        };
        byte[] bytes = Encoding.UTF8.GetBytes(CryptoPrefs.sKEY);
        byte[] rgbIV = Convert.FromBase64String(CryptoPrefs.sIV);
        ICryptoTransform transform = rijndaelManaged.CreateEncryptor(bytes, rgbIV);
        MemoryStream memoryStream = new MemoryStream();
        CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
        byte[] bytes2 = Encoding.UTF8.GetBytes(rawString);
        cryptoStream.Write(bytes2, 0, bytes2.Length);
        cryptoStream.FlushFinalBlock();
        byte[] inArray = memoryStream.ToArray();
        return Convert.ToBase64String(inArray);
    }
    private static string GetHash(string key)
    {
        MD5 mD = new MD5CryptoServiceProvider();
        byte[] array = mD.ComputeHash(Encoding.UTF8.GetBytes(key));
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < array.Length; i++)
        {
            stringBuilder.Append(array[i].ToString("x2"));
        }
        return stringBuilder.ToString();
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
}