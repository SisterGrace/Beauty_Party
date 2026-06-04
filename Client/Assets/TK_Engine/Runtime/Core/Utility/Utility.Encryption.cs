using System;
using System.Security.Cryptography;
using System.Text;


namespace TK_Engine
{
    public static partial class Utility
    {
        /// <summary>
        /// 加密解密相关的实用函数。
        /// </summary>
        public static class Encryption
        {
            internal const int QuickEncryptLength = 220;

            /// <summary>
            /// 将 bytes 使用 code 做异或运算的快速版本。
            /// </summary>
            /// <param name="bytes">原始二进制流。</param>
            /// <param name="code">异或二进制流。</param>
            /// <returns>异或后的二进制流。</returns>
            public static byte[] GetQuickXorBytes(byte[] bytes, byte[] code)
            {
                return GetXorBytes(bytes, 0, QuickEncryptLength, code);
            }

            /// <summary>
            /// 将 bytes 使用 code 做异或运算的快速版本。此方法将复用并改写传入的 bytes 作为返回值，而不额外分配内存空间。
            /// </summary>
            /// <param name="bytes">原始及异或后的二进制流。</param>
            /// <param name="code">异或二进制流。</param>
            public static void GetQuickSelfXorBytes(byte[] bytes, byte[] code)
            {
                GetSelfXorBytes(bytes, 0, QuickEncryptLength, code);
            }

            /// <summary>
            /// 将 bytes 使用 code 做异或运算。
            /// </summary>
            /// <param name="bytes">原始二进制流。</param>
            /// <param name="code">异或二进制流。</param>
            /// <returns>异或后的二进制流。</returns>
            public static byte[] GetXorBytes(byte[] bytes, byte[] code)
            {
                if (bytes == null)
                {
                    return null;
                }

                return GetXorBytes(bytes, 0, bytes.Length, code);
            }

            /// <summary>
            /// 将 bytes 使用 code 做异或运算。此方法将复用并改写传入的 bytes 作为返回值，而不额外分配内存空间。
            /// </summary>
            /// <param name="bytes">原始及异或后的二进制流。</param>
            /// <param name="code">异或二进制流。</param>
            public static void GetSelfXorBytes(byte[] bytes, byte[] code)
            {
                if (bytes == null)
                {
                    return;
                }

                GetSelfXorBytes(bytes, 0, bytes.Length, code);
            }

            /// <summary>
            /// 将 bytes 使用 code 做异或运算。
            /// </summary>
            /// <param name="bytes">原始二进制流。</param>
            /// <param name="startIndex">异或计算的开始位置。</param>
            /// <param name="length">异或计算长度，若小于 0，则计算整个二进制流。</param>
            /// <param name="code">异或二进制流。</param>
            /// <returns>异或后的二进制流。</returns>
            public static byte[] GetXorBytes(byte[] bytes, int startIndex, int length, byte[] code)
            {
                if (bytes == null)
                {
                    return null;
                }

                int bytesLength = bytes.Length;
                byte[] results = new byte[bytesLength];
                Array.Copy(bytes, 0, results, 0, bytesLength);
                GetSelfXorBytes(results, startIndex, length, code);
                return results;
            }

            /// <summary>
            /// 将 bytes 使用 code 做异或运算。此方法将复用并改写传入的 bytes 作为返回值，而不额外分配内存空间。
            /// </summary>
            /// <param name="bytes">原始及异或后的二进制流。</param>
            /// <param name="startIndex">异或计算的开始位置。</param>
            /// <param name="length">异或计算长度。</param>
            /// <param name="code">异或二进制流。</param>
            public static void GetSelfXorBytes(byte[] bytes, int startIndex, int length, byte[] code)
            {
                if (bytes == null)
                {
                    return;
                }

                if (code == null)
                {
                    throw new GameFrameworkException("Code is invalid.");
                }

                int codeLength = code.Length;
                if (codeLength <= 0)
                {
                    throw new GameFrameworkException("Code length is invalid.");
                }

                if (startIndex < 0 || length < 0 || startIndex + length > bytes.Length)
                {
                    throw new GameFrameworkException("Start index or length is invalid.");
                }

                int codeIndex = startIndex % codeLength;
                for (int i = startIndex; i < length; i++)
                {
                    bytes[i] ^= code[codeIndex++];
                    codeIndex %= codeLength;
                }
            }
        }

        public static class EncryptionHelper
        {
            #region XXTEA
            public const string XXTEAKey = "tk423&(gBUjX#$s0628";

            public static string XXTEAEncrypt(string pOriginStr)
            {
                Encoding tEncoder = Encoding.UTF8;
                byte[] tata = XXTEA.Encrypt(tEncoder.GetBytes(pOriginStr), tEncoder.GetBytes(XXTEAKey));
                return BytesToHex(tata);
            }

            public static string XXTEADecrypt(string pOriginStr)
            {
                Encoding tEncoder = Encoding.UTF8;
                string tDeStr = string.Empty;
                try
                {
                    tDeStr = tEncoder.GetString(XXTEA.Decrypt(HexToBytes(pOriginStr), tEncoder.GetBytes(XXTEAKey)));
                }
                catch (Exception)
                {
                    return null;
                }
                return tDeStr;
            }
            #endregion
            #region bytes And string
            public static string BytesToString(byte[] pBytes)
            {
                Encoding tCoding = Encoding.UTF8;
                string tString = tCoding.GetString(pBytes);

                return tString;
            }

            public static byte[] StringToBytes(string pStr)
            {
                Encoding tCoding = Encoding.UTF8;
                byte[] tBytes = tCoding.GetBytes(pStr);

                return tBytes;
            }
            #endregion
            #region bytes And hex
            public static string BytesToHex(byte[] pBytes)
            {
                return BitConverter.ToString(pBytes, 0).Replace("-", string.Empty).ToLower();
            }

            public static byte[] HexToBytes(string pHex)
            {
                byte[] tBytes = new byte[pHex.Length / 2];
                for (var x = 0; x < tBytes.Length; x++)
                {
                    var i = Convert.ToInt32(pHex.Substring(x * 2, 2), 16);
                    tBytes[x] = (byte)i;
                }

                return tBytes;
            }
            #endregion
            #region MD5
            public const string MD5Key = "fantasy2026";
            public static string GetMD5(string pOriginal)
            {
                var temp = $"{pOriginal}-{MD5Key}";
                using (var md5 = MD5.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(temp);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    var sb = new StringBuilder();
                    foreach (var hb in hashBytes)
                    {
                        sb.Append(hb.ToString("x2"));
                    }

                    return sb.ToString();
                }
            }
            #endregion
        }

        public class XXTEA
        {
            public static Byte[] Encrypt(Byte[] Data, Byte[] Key)
            {
                if (Data.Length == 0)
                {
                    return Data;
                }
                return ToByteArray(Encrypt(ToUInt32Array(Data, true), ToUInt32Array(Key, false)), false);
            }
            public static Byte[] Decrypt(Byte[] Data, Byte[] Key)
            {
                if (Data.Length == 0)
                {
                    return Data;
                }
                return ToByteArray(Decrypt(ToUInt32Array(Data, false), ToUInt32Array(Key, false)), true);
            }
            public static UInt32[] Encrypt(UInt32[] v, UInt32[] k)
            {
                Int32 n = v.Length - 1;
                if (n < 1)
                {
                    return v;
                }
                if (k.Length < 4)
                {
                    UInt32[] Key = new UInt32[4];
                    k.CopyTo(Key, 0);
                    k = Key;
                }
                UInt32 z = v[n], y = v[0], delta = 0x9E3779B9, sum = 0, e;
                Int32 p, q = 6 + 52 / (n + 1);
                while (q-- > 0)
                {
                    sum = unchecked(sum + delta);
                    e = sum >> 2 & 3;
                    for (p = 0; p < n; p++)
                    {
                        y = v[p + 1];
                        z = unchecked(v[p] += (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z));
                    }
                    y = v[0];
                    z = unchecked(v[n] += (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z));
                }
                return v;

            }

            public static UInt32[] Decrypt(UInt32[] v, UInt32[] k)
            {
                Int32 n = v.Length - 1;
                if (n < 1)
                {
                    return v;
                }
                if (k.Length < 4)
                {
                    UInt32[] Key = new UInt32[4];
                    k.CopyTo(Key, 0);
                    k = Key;
                }
                UInt32 z = v[n], y = v[0], delta = 0x9E3779B9, sum, e;
                Int32 p, q = 6 + 52 / (n + 1);
                sum = unchecked((UInt32)(q * delta));
                while (sum != 0)
                {
                    e = sum >> 2 & 3;
                    for (p = n; p > 0; p--)
                    {
                        z = v[p - 1];
                        y = unchecked(v[p] -= (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z));
                    }
                    z = v[n];
                    y = unchecked(v[0] -= (z >> 5 ^ y << 2) + (y >> 3 ^ z << 4) ^ (sum ^ y) + (k[p & 3 ^ e] ^ z));
                    sum = unchecked(sum - delta);
                }
                return v;
            }
            private static UInt32[] ToUInt32Array(Byte[] Data, Boolean IncludeLength)
            {
                Int32 n = (((Data.Length & 3) == 0) ? (Data.Length >> 2) : ((Data.Length >> 2) + 1));
                UInt32[] Result;
                if (IncludeLength)
                {
                    Result = new UInt32[n + 1];
                    Result[n] = (UInt32)Data.Length;
                }
                else
                {
                    Result = new UInt32[n];
                }
                n = Data.Length;
                for (Int32 i = 0; i < n; i++)
                {
                    Result[i >> 2] |= (UInt32)Data[i] << ((i & 3) << 3);
                }
                return Result;
            }

            private static Byte[] ToByteArray(UInt32[] Data, Boolean IncludeLength)
            {
                Int32 n;
                if (IncludeLength)
                {
                    n = (Int32)Data[Data.Length - 1];
                }
                else
                {
                    n = Data.Length << 2;
                }
                Byte[] Result = new Byte[n];
                for (Int32 i = 0; i < n; i++)
                {
                    Result[i] = (Byte)(Data[i >> 2] >> ((i & 3) << 3));
                }
                return Result;
            }
        }
    }
}
