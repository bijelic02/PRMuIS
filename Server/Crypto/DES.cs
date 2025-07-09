using System.Security.Cryptography;
using System.Text;

namespace Server.Crypto
{
    public class DES
    {
        private static string key;
        private static string iv;

        public DES(string k, string i)
        {
            key = k;
            iv = i;
        }

        public string Encrypt(string plainText)
        {
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.Key = Encoding.UTF8.GetBytes(key);
                des.IV = Encoding.UTF8.GetBytes(iv);
                des.Mode = CipherMode.CBC;
                des.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = des.CreateEncryptor(des.Key, des.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(inputBytes, 0, inputBytes.Length);
                        cs.FlushFinalBlock();
                    }
                    byte[] encryptedData = ms.ToArray();
                    string encryptedTextBase64 = Convert.ToBase64String(encryptedData);
                    return encryptedTextBase64;
                }
            }
        }

        public string Decrypt(string cipherTextBase64)
        {
            byte[] cipherText = Convert.FromBase64String(cipherTextBase64);

            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.Key = Encoding.UTF8.GetBytes(key);
                des.IV = Encoding.UTF8.GetBytes(iv);
                des.Mode = CipherMode.CBC;
                des.Padding = PaddingMode.PKCS7;

                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    using (ICryptoTransform decryptor = des.CreateDecryptor(des.Key, des.IV))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            /*
                            cs.Write(cipherText, 0, cipherText.Length);
                            cs.FlushFinalBlock();
                            return Encoding.UTF8.GetString(ms.ToArray());
                            */
                            using (StreamReader srDecrypt = new StreamReader(cs, Encoding.UTF8))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }
    }
}
