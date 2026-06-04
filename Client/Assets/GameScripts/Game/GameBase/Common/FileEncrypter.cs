using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameBase
{
    public class FileEncrypter 
    {
        private const string _secretKey = "abcd1234";   
    
        public static byte[] Encrypt(byte[] data)
        {
            byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(_secretKey);
            byte[] encryptedData = new byte[data.Length];
        
            for (int i = 0; i < data.Length; i++)
            {
                encryptedData[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
            }
        
            return encryptedData;
        }

        /// <summary>
        /// 原地加密/解密（不分配新内存，直接修改传入的数组）
        /// </summary>
        public static void EncryptInPlace(byte[] data)
        {
            if (data == null) return;
            
            byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(_secretKey);
            int keyLen = keyBytes.Length;
            
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(data[i] ^ keyBytes[i % keyLen]);
            }
        }

        public static byte[] Decrypt(byte[] encryptedData)
        {
            return Encrypt(encryptedData);
        }
    }
}
