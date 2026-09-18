using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Sky
{
    public static class SkyEncryptUtils 
    {
        private const string XXTEA_KEY = "tkbff&(gBUjX#$s0710";
        private const string secretKey = "tk~!@#$%^&*()_+0708";

        public static string EncryptRequestBody(string requestBody)
        {
            var encryptRequestBodyBytes = SkyXXTEA.Encrypt(Encoding.UTF8.GetBytes(requestBody), Encoding.UTF8.GetBytes(XXTEA_KEY));
            var encryptRequestBody = BytesToHexString(encryptRequestBodyBytes, false);
            SkyLogUtils.Debug($"[[server]] [EncryptRequestBody] requestBody = {requestBody} encryptRequestBody = {encryptRequestBody}");
            return encryptRequestBody;
        }

        public static string DecryptResponseBody(string responseBody)
        {
            var decryptResponseBodyBytes = SkyXXTEA.Decrypt(HexStringToBytes(responseBody), Encoding.UTF8.GetBytes(XXTEA_KEY));
            var decryptResponseBody = Encoding.UTF8.GetString(decryptResponseBodyBytes);
            SkyLogUtils.Debug($"[[server]] [DecryptResponseBody] decryptResponseBody = {decryptResponseBody}");
            return decryptResponseBody;
        }

        public static string GetMD5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);

                var builder = new StringBuilder();
                foreach (var t in hashBytes)
                {
                    builder.Append(t.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        public static void EncryptionParameters(Dictionary<string, object> args)
        {
            var signString =
                $"{secretKey}platform={args["platform"]}packagename={args["package_name"]}channel={args["channel"]}appversion={args["app_version"]}appversioncode={args["app_version_code"]}language={args["language"]}ip={args["ip"]}ts={args["ts"]}";
            var sign = GetMD5Hash(signString);
            args["sign"] = sign;
        }

        private static string BytesToHexString(byte[] bytes, bool isUpperCase)
        {
            if (bytes == null)
                return "";

            char[] hexDigits = isUpperCase ? HEX_DIGITS_UPPER : HEX_DIGITS_LOWER;
            int len = bytes.Length;
            if (len == 0)
                return "";

            char[] ret = new char[len << 1];
            for (int i = 0, j = 0; i < len; i++)
            {
                ret[j++] = hexDigits[bytes[i] >> 4 & 0x0f];
                ret[j++] = hexDigits[bytes[i] & 0x0f];
            }

            return new string(ret);
        }

        private static byte[] HexStringToBytes(string hexString)
        {
            if (IsSpace(hexString))
                return new byte[0];

            int len = hexString.Length;
            if (len % 2 != 0)
            {
                hexString = "0" + hexString;
                len = len + 1;
            }

            char[] hexBytes = hexString.ToUpper().ToCharArray();
            byte[] ret = new byte[len >> 1];
            for (int i = 0; i < len; i += 2)
            {
                ret[i >> 1] = (byte)(HexToDec(hexBytes[i]) << 4 | HexToDec(hexBytes[i + 1]));
            }

            return ret;
        }

        private static bool IsSpace(string s)
        {
            if (s == null)
                return true;

            for (int i = 0, len = s.Length; i < len; ++i)
            {
                if (!char.IsWhiteSpace(s[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static int HexToDec(char hexChar)
        {
            if (hexChar >= '0' && hexChar <= '9')
            {
                return hexChar - '0';
            }
            else if (hexChar >= 'A' && hexChar <= 'F')
            {
                return hexChar - 'A' + 10;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private static readonly char[] HEX_DIGITS_UPPER =
            { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private static readonly char[] HEX_DIGITS_LOWER =
            { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
        
    }
}

