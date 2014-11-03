using System;
using System.Security.Cryptography;
using System.Text;

namespace Radar.Util
{
    public static class GravatarUtil
    {
        private const string urlFormat = "http://www.gravatar.com/avatar/{0}";

        private static readonly char[] hexChars = new char[]
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'
        };

        public static string GetUrl(string email)
        {
            byte[] emailHash = new MD5CryptoServiceProvider().ComputeHash(
                Encoding.UTF8.GetBytes(email.Trim().ToLower()));

            return string.Format(urlFormat, HexString(emailHash));
        }

        private static string HexString(byte[] value)
        {
            StringBuilder hex = new StringBuilder();

            for (int i = 0; i < value.Length; i++)
            {
                hex.Append(hexChars[(value[i] & 0xf0) >> 4]);
                hex.Append(hexChars[(value[i] & 0x0f)]);
            }

            return hex.ToString();
        }
    }
}