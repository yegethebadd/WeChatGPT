using System.Security.Cryptography;
using System.Text;

namespace WeChatGPT.Services
{
    public class UniqueKey
    {
        public static string GetUniqueKey(int maxSize)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890=".ToCharArray();
            byte[] data = new byte[1];
            using (RNGCryptoServiceProvider cryto = new RNGCryptoServiceProvider())
            {
                cryto.GetNonZeroBytes(data);
                data = new byte[maxSize];
                cryto.GetNonZeroBytes(data);
            }
            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }
    }
}
