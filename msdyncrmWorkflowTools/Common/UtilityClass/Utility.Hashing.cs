using System.Security.Cryptography;
using System.Text;

namespace msdyncrmWorkflowTools
{
    public static partial class Utility
    {
        /// <summary>
        /// MD5 hash of the text's ASCII bytes, as 32 lowercase hex digits.
        /// </summary>
        public static string Md5Hash(string text)
        {
            using (var md5 = MD5.Create())
            {
                return ToHex(md5.ComputeHash(Encoding.ASCII.GetBytes(text)));
            }
        }

        /// <summary>
        /// SHA-512 hash of the text's ASCII bytes, as 128 lowercase hex digits.
        /// </summary>
        public static string Sha512Hash(string text)
        {
            using (var sha512 = SHA512.Create())
            {
                return ToHex(sha512.ComputeHash(Encoding.ASCII.GetBytes(text)));
            }
        }

        private static string ToHex(byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);

            foreach (var b in bytes)
            {
                hex.Append(b.ToString("x2"));
            }

            return hex.ToString();
        }
    }
}
