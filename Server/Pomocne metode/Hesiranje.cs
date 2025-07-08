using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Server.Pomocne_metode
{
    public class Hesiranje
    {
        public Hesiranje() {}

        public string Hesiraj(string naziv)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hesirano = sha256.ComputeHash(Encoding.UTF8.GetBytes(naziv));
                return BitConverter.ToString(hesirano).Replace("-", "").ToLower();
            }
        }
    }
}
