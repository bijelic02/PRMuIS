using Server.Pomocne_metode;

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

        public void Ispisi()
        {
            foreach (var item in data)
            {
                Console.WriteLine(item.Key + " " + item.Value);
            }
        }
    }
}