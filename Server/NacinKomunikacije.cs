using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class NacinKomunikacije
    {
        public EndPoint UticnicaAdresaKlijenta { get; set; }
        public string Algoritam { get; set; }

        public string Kljuc { get; set; }

        public string Dodatno { get; set; }

        public NacinKomunikacije (EndPoint uticnicaAdresaKlijenta, string algoritam, string kljuc, string dodatno = "")
        {
            UticnicaAdresaKlijenta = uticnicaAdresaKlijenta;
            Algoritam = algoritam;
            Kljuc = kljuc;
            Dodatno = dodatno;
        }

        public override string? ToString()
        {
            return "Komunikacija sa klijentom na adresi i portu: " + UticnicaAdresaKlijenta.ToString() + "\nAlgoritam: " + Algoritam + "\nKljuc: " + Kljuc + "\nDodatne informacije: " + Dodatno;
        }
    }
}
