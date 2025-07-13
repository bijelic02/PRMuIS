using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class Server
    {
        // TODO: popravi metodu za statistiku
        // treba na osnovu duzine reci da se izracuna prosek vremena za des i aes
        // moze i malo ispis da se popravi

        static void Main(string[] args)
        {
            List<NacinKomunikacije> komunikacijaLista = new List<NacinKomunikacije>();
            Repo repo = new Repo();
            Dictionary<double, double> desVremena = new Dictionary<double, double>();
            Dictionary<double, double> aesVremena = new Dictionary<double, double>();

            Socket serverSocket;
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 65000);

            Console.WriteLine("Server je pokrenut.\n");
            Console.WriteLine("Izaberite koji protokol zelite da koristite (UDP ili TCP): ");
            string? protokol = Console.ReadLine();

            if (protokol?.ToLower() == "tcp")
            {
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            else if (protokol?.ToLower() == "udp")
            {
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            else
            {
                Console.WriteLine("Za tip protokola niste uneli validnu vrednost. Aplikacija prestaje sa radom.\n");
                return;
            }
            serverSocket.Bind(serverEP);
            serverSocket.Blocking = false;

            int maxKlijenata = 5;
            if (protokol?.ToLower() == "tcp")
            {
                serverSocket.Listen(maxKlijenata);
            }

            Console.WriteLine($"Server je stavljen u stanje oslukivanja i ocekuje komunikaciju na {serverEP}");

            if (protokol?.ToLower() == "tcp")
            {
                List<Socket> klijenti = new List<Socket>();
                try
                {
                    bool kraj = true;
                    while (kraj)
                    {
                        List<Socket> checkRead = new List<Socket>();
                        List<Socket> checkError = new List<Socket>();

                        if (klijenti.Count < maxKlijenata)
                        {
                            checkRead.Add(serverSocket);

                        }
                        checkError.Add(serverSocket);

                        foreach (Socket s in klijenti)
                        {
                            checkRead.Add(s);
                            checkError.Add(s);
                        }

                        Socket.Select(checkRead, null, checkError, 1000);
                        if (checkRead.Count > 0)
                        {
                            foreach (Socket s in checkRead)
                            {
                                byte[] buffer = new byte[1024];
                                int brBajta = 0;
                                if (s == serverSocket)
                                {
                                    Socket client = serverSocket.Accept();
                                    client.Blocking = false;
                                    klijenti.Add(client);
                                    IPEndPoint clientEP = client.RemoteEndPoint as IPEndPoint;
                                    Console.WriteLine($"Povezao se novi klijent! Njegova adresa je {clientEP}");
                                }
                                else
                                {
                                    try
                                    {

                                        if (s.Poll(1000000, SelectMode.SelectRead))
                                        {
                                            brBajta = s.Receive(buffer);

                                            var komunikacija = komunikacijaLista.Find(k => k.UticnicaAdresaKlijenta.Equals(s.RemoteEndPoint));

                                            if (komunikacija == null)
                                            {
                                                //primi Hesh alg, key i iv
                                                string hesiranAlg = Encoding.UTF8.GetString(buffer, 0, 64);
                                                string algoritam = repo.PronadjiAlgoritam(hesiranAlg);
                                                string key = "", iv = "";
                                                if (algoritam == "des")
                                                {
                                                    key = Encoding.UTF8.GetString(buffer, brBajta - 16, 8);
                                                    iv = Encoding.UTF8.GetString(buffer, brBajta - 8, 8);
                                                }
                                                else
                                                {
                                                    key = Encoding.UTF8.GetString(buffer, brBajta - 32, 16);
                                                    iv = Encoding.UTF8.GetString(buffer, brBajta - 16, 16);
                                                }

                                                NacinKomunikacije komunikacijaSaKlijentom = new NacinKomunikacije
                                                                (s.RemoteEndPoint, algoritam, key, iv);
                                                komunikacijaLista.Add(komunikacijaSaKlijentom);
                                                Console.WriteLine(komunikacijaSaKlijentom.ToString());
                                                continue;
                                            }
                                            else
                                            {
                                                if (brBajta == 0)
                                                {
                                                    Console.WriteLine("Klijent je zavrsio sa radom");
                                                    s.Close();
                                                    klijenti.Remove(s);
                                                    komunikacijaLista.Remove(komunikacija);
                                                    continue;
                                                }

                                                string sifrovanaPoruka = Encoding.UTF8.GetString(buffer, 0, brBajta);
                                                string poruka = "";

                                                if (komunikacija.Algoritam == "des")
                                                {
                                                    Crypto.DES des = new Crypto.DES(komunikacija.Kljuc, komunikacija.Dodatno);
                                                    poruka = des.Decrypt(sifrovanaPoruka);
                                                }
                                                else
                                                {
                                                    Crypto.AES aes = new Crypto.AES(komunikacija.Kljuc, komunikacija.Dodatno);
                                                    poruka = aes.Decrypt(sifrovanaPoruka);
                                                }
                                                Console.WriteLine("Primljena (sifrovana) poruka: " + sifrovanaPoruka);
                                                Console.WriteLine("Desifrovana poruka: " + poruka);

                                                if (poruka == "kraj")
                                                {
                                                    klijenti.Remove(s);
                                                    komunikacijaLista.Remove(komunikacija);
                                                    if (komunikacijaLista.Count == 0)
                                                    {
                                                        Console.WriteLine("Svi klijenti su zavrsili sa radom");
                                                        IspisiStatistiku(desVremena, aesVremena);
                                                        kraj = false;
                                                    }
                                                    break;
                                                }
                                                Console.WriteLine("Unesite poruku");
                                                string odgovor = Console.ReadLine();
                                                string sifrovaniOdgovor = "";

                                                Stopwatch sw = new Stopwatch();
                                                if (komunikacija.Algoritam == "des")
                                                {
                                                    Crypto.DES des = new Crypto.DES(komunikacija.Kljuc, komunikacija.Dodatno);
                                                    sw.Start();
                                                    sifrovaniOdgovor = des.Encrypt(odgovor);
                                                    sw.Stop();
                                                    desVremena.Add(odgovor.Length, sw.Elapsed.TotalMilliseconds);
                                                }
                                                else
                                                {
                                                    Crypto.AES aes = new Crypto.AES(komunikacija.Kljuc, komunikacija.Dodatno);
                                                    sw.Start();
                                                    sifrovaniOdgovor = aes.Encrypt(odgovor);
                                                    sw.Stop();
                                                    aesVremena.Add(odgovor.Length, sw.Elapsed.TotalMilliseconds);
                                                }

                                                brBajta = s.Send(Encoding.UTF8.GetBytes(sifrovaniOdgovor));
                                                if (odgovor == "kraj")
                                                {
                                                    klijenti.Remove(s);
                                                    komunikacijaLista.Remove(komunikacija);
                                                    if (komunikacijaLista.Count == 0)
                                                    {
                                                        Console.WriteLine("Server zavrsava sa radom");
                                                        IspisiStatistiku(desVremena, aesVremena);
                                                        kraj = false;
                                                    }
                                                    break;

                                                }
                                                continue;
                                            }
                                        }
                                    }
                                    catch (SocketException ex)
                                    {
                                        Console.WriteLine($"Doslo je do greske {ex}");
                                    }

                                    Console.WriteLine("Server zavrsava sa radom");
                                    IspisiStatistiku(desVremena, aesVremena);
                                    Console.ReadKey();
                                    s.Close();
                                    serverSocket.Close();
                                }
                            }
                        }
                        if (checkError.Count > 0)
                        {
                            Console.WriteLine($"Desilo se {checkError.Count} gresaka\n");

                            foreach (Socket s in checkError)
                            {
                                Console.WriteLine($"Greska na socketu: {s.LocalEndPoint}");

                                Console.WriteLine("Zatvaram socket zbog greske...");
                                s.Close();

                            }
                        }
                        checkError.Clear();
                        checkRead.Clear();
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Doslo je do greske {ex}");
                }
                Console.WriteLine("Server zavrsava sa radom");
               // IspisiStatistiku(desVremena, aesVremena);
                
                Console.ReadKey();
                serverSocket.Close();
            }
            else
            {

                EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    try
                    {
                        List<Socket> checkRead = new List<Socket> { serverSocket };
                        List<Socket> checkError = new List<Socket> { serverSocket };
                        Socket.Select(checkRead, null, checkError, 1000);

                        if (checkRead.Count > 0)
                        {
                            foreach (Socket s in checkRead)
                            {
                                byte[] buffer = new byte[1024];
                                int brBajta = 0;
                                try
                                {
                                    if (s.Poll(1000000, SelectMode.SelectRead)) // proverava da li je dostupno za citanje
                                    {
                                        brBajta = s.ReceiveFrom(buffer, ref posiljaocEP);
                                        if (brBajta == 0)
                                        {
                                            Console.WriteLine("Klijent je zavrsio sa radom");
                                            break;
                                        }

                                        // provera da li klijent vec postoji
                                        var komunikacija = komunikacijaLista.Find(k => k.UticnicaAdresaKlijenta.Equals(posiljaocEP));
                                        // ako ne postoji, onda dodajemo u listu
                                        if (komunikacija == null)
                                        {
                                            string poruka1 = Encoding.UTF8.GetString(buffer, 0, brBajta);
                                            string algoritam = repo.PronadjiAlgoritam(poruka1);
                                            string poruka2 = "", poruka3 = "";

                                            // ceka se druga poruka - kljuc
                                            if (s.Poll(1000, SelectMode.SelectRead))
                                            {
                                                posiljaocEP = new IPEndPoint(IPAddress.Any, 0);
                                                brBajta = s.ReceiveFrom(buffer, ref posiljaocEP);
                                                poruka2 = Encoding.UTF8.GetString(buffer, 0, brBajta);
                                                Console.WriteLine($"Primljena druga poruka (ključ): {poruka2}");

                                                // ceka se treca poruka - iv
                                                if (s.Poll(1000, SelectMode.SelectRead))
                                                {
                                                    posiljaocEP = new IPEndPoint(IPAddress.Any, 0);
                                                    brBajta = s.ReceiveFrom(buffer, ref posiljaocEP);
                                                    poruka3 = Encoding.UTF8.GetString(buffer, 0, brBajta);
                                                    Console.WriteLine($"Primljena treća poruka (IV): {poruka3}");

                                                    komunikacija = new NacinKomunikacije(posiljaocEP, algoritam, poruka2, poruka3);
                                                    komunikacijaLista.Add(komunikacija);
                                                    Console.WriteLine(komunikacija.ToString());
                                                }
                                            }
                                        }
                                        else // klijent vec postoji
                                        {
                                            string sifrovanaPoruka = Encoding.UTF8.GetString(buffer, 0, brBajta);
                                            string poruka = "";

                                            if (komunikacija.Algoritam == "des")
                                            {
                                                Crypto.DES des = new Crypto.DES(komunikacija.Kljuc, komunikacija.Dodatno);
                                                poruka = des.Decrypt(sifrovanaPoruka);
                                            }
                                            else
                                            {
                                                Crypto.AES aes = new Crypto.AES(komunikacija.Kljuc, komunikacija.Dodatno);
                                                poruka = aes.Decrypt(sifrovanaPoruka);
                                            }
                                            Console.WriteLine("Primljena (sifrovana) poruka: " + sifrovanaPoruka);
                                            Console.WriteLine("Desifrovana poruka: " + poruka);
                                            if (poruka == "kraj")
                                            {
                                                komunikacijaLista.Remove(komunikacija);
                                                if (komunikacijaLista.Count == 0)
                                                {
                                                    Console.WriteLine("Svi klijenti su zavrsili sa radom");
                                                    IspisiStatistiku(desVremena, aesVremena);
                                                    break;
                                                }
                                                break;
                                            }

                                            Console.WriteLine("Unesite poruku");
                                            string odgovor = Console.ReadLine();
                                            string sifrovaniOdgovor = "";

                                            Stopwatch sw = new Stopwatch();
                                            if (komunikacija.Algoritam == "des")
                                            {
                                                Crypto.DES des = new Crypto.DES(komunikacija.Kljuc, komunikacija.Dodatno);
                                                sw.Start();
                                                sifrovaniOdgovor = des.Encrypt(odgovor);
                                                sw.Stop();
                                                desVremena.Add( odgovor.Length, sw.Elapsed.TotalMilliseconds);
                                            }
                                            else
                                            {
                                                Crypto.AES aes = new Crypto.AES(komunikacija.Kljuc, komunikacija.Dodatno);
                                                sw.Start();
                                                sifrovaniOdgovor = aes.Encrypt(odgovor);
                                                sw.Stop();
                                                aesVremena.Add( odgovor.Length, sw.Elapsed.TotalMilliseconds);
                                            }

                                            brBajta = s.SendTo(Encoding.UTF8.GetBytes(sifrovaniOdgovor), komunikacija.UticnicaAdresaKlijenta);
                                            if (odgovor == "kraj")
                                            {
                                                komunikacijaLista.Remove(komunikacija);
                                                if (komunikacijaLista.Count == 0)
                                                {
                                                    Console.WriteLine("Svi klijenti su zavrsili sa radom");
                                                    IspisiStatistiku(desVremena, aesVremena);
                                                    break;
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (SocketException ex)
                                {
                                    Console.WriteLine($"Greška sa soketom: {ex.Message}");
                                    break;
                                }
                            }
                        }
                        if (checkError.Count > 0)
                        {
                            Console.WriteLine($"Desilo se {checkError.Count} gresaka\n");

                            foreach (Socket s in checkError)
                            {
                                Console.WriteLine($"Greska na socketu: {s.LocalEndPoint}");

                                Console.WriteLine("Zatvaram socket zbog greske...");
                                s.Close();

                            }
                        }
                        checkError.Clear();
                        checkRead.Clear();
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"Doslo je do greske {ex}");
                        break;
                    }

                }

                Console.WriteLine("Server zavrsava sa radom");
                IspisiStatistiku(desVremena, aesVremena);
                serverSocket.Close();
                Console.ReadKey();
                
            }
        }

        private static void IspisiStatistiku(Dictionary<double, double> desVremena, Dictionary<double, double> aesVremena)
        {
            Console.WriteLine("--- Statistika za DES ---");
            if (desVremena == null || desVremena.Count == 0)
            {
                Console.WriteLine("Nema podataka za DES prikaz.");
            }
            else
            {
                var desProseciPoDuzini = desVremena
                  .GroupBy(item => item.Key) // Grupiši po dužini poruke
                  .Select(group => new
                  {
                      DuzinaPoruke = group.Key,
                      ProsecnoVreme = group.Average(item => item.Value) // Izračunaj prosek vremena unutar grupe
                  })
                  .OrderBy(item => item.DuzinaPoruke); // Sortiraj po dužini poruke

                foreach (var stavka in desProseciPoDuzini)
                {
                    Console.WriteLine($"DES: Dužina poruke: {stavka.DuzinaPoruke}, Prosečno vreme: {stavka.ProsecnoVreme:F6} ms");
                }
            }
            Console.WriteLine("\n--- Statistika za AES ---");
            if (aesVremena == null || aesVremena.Count == 0)
            {
                Console.WriteLine("Nema podataka za AES prikaz.");
            }
            else
            {
                // Grupiši AES merenja po dužini poruke (ključu) i izračunaj prosek vremena za svaku grupu
                var aesProseciPoDuzini = aesVremena
                    .GroupBy(item => item.Key) // Grupiši po dužini poruke
                    .Select(group => new
                    {
                        DuzinaPoruke = group.Key,
                        ProsecnoVreme = group.Average(item => item.Value) // Izračunaj prosek vremena unutar grupe
                    })
                    .OrderBy(item => item.DuzinaPoruke); // Sortiraj po dužini poruke

                foreach (var stavka in aesProseciPoDuzini)
                {
                    Console.WriteLine($"AES: Dužina poruke: {stavka.DuzinaPoruke}, Prosečno vreme: {stavka.ProsecnoVreme:F6} ms");
                }
            }

        }
       
    }
    
}
