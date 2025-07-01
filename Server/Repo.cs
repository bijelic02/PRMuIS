using Client.Pomocne_metode;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Repo
    {
        private Dictionary<string, string> data = new Dictionary<string, string>();

        public Repo()
        {
            data = new Dictionary<string, string>();
            Hesiranje hesiranje = new Hesiranje();
            data.Add(hesiranje.Hesiraj("des"), "des");
            data.Add(hesiranje.Hesiraj("aes"), "aes");
        }

        public string PronadjiAlgoritam(string hesirano)
        {
            return data.GetValueOrDefault(hesirano);
        }
    }
}
