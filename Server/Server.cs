using Server.Crypto;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class Server
    {
        static void Main(string[] args)
        {
            List<NacinKomunikacije> komunikacijaLista = new List<NacinKomunikacije>();
            Repo repo = new Repo();
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

            if (protokol?.ToLower() == "tcp")
            {
                serverSocket.Listen(5);
            }

            Console.WriteLine($"Server je stavljen u stanje oslukivanja i ocekuje komunikaciju na {serverEP}");

            if (protokol?.ToLower() == "tcp")
            {
                Socket acceptedSocket = serverSocket.Accept();
                IPEndPoint clientEP = acceptedSocket.RemoteEndPoint as IPEndPoint;
                Console.WriteLine($"Povezao se novi klijent! Njegova adresa je {clientEP}");

                byte[] bafer1 = new byte[100];
                int brBajta1 = acceptedSocket.Receive(bafer1);
                string poruka1 = Encoding.UTF8.GetString(bafer1, 0, brBajta1);
                //Console.WriteLine(poruka1);
                string algoritam = repo.PronadjiAlgoritam(poruka1);

                byte[] bafer2;
                byte[] bafer3;
                if (algoritam == "des")
                {
                    bafer2 = new byte[8];
                    bafer3 = new byte[8];
                }
                else
                {
                    bafer2 = new byte[16];
                    bafer3 = new byte[16];
                }

                int brBajta2 = acceptedSocket.Receive(bafer2);
                string poruka2 = Encoding.UTF8.GetString(bafer2, 0, brBajta2);
                //Console.WriteLine(poruka2);

                int brBajta3 = acceptedSocket.Receive(bafer3);
                string poruka3 = Encoding.UTF8.GetString(bafer3, 0, brBajta3);
                //Console.WriteLine(poruka3);

                NacinKomunikacije komunikacijaSaKlijentom = new NacinKomunikacije(clientEP, algoritam, poruka2, poruka3);
                komunikacijaLista.Add(komunikacijaSaKlijentom);
                Console.WriteLine(komunikacijaSaKlijentom.ToString());

                byte[] buffer = new byte[1024];
                while (true)
                {
                    try
                    {
                        int brBajta = acceptedSocket.Receive(buffer);
                        if (brBajta == 0)
                        {
                            Console.WriteLine("Klijent je zavrsio sa radom");
                            break;
                        }
                        string sifrovanaPoruka = Encoding.UTF8.GetString(buffer, 0, brBajta);
                        string poruka = "";

                        if (algoritam == "des")
                        {
                            DES des = new DES(poruka2, poruka3);
                            poruka = des.Decrypt(sifrovanaPoruka);
                        }
                        else
                        {
                            AES aes = new AES(poruka2, poruka3);
                            poruka = aes.Decrypt(sifrovanaPoruka);
                        }

                        Console.WriteLine("Primljena (sifrovana) poruka: " + sifrovanaPoruka);
                        Console.WriteLine("Desifrovana poruka: " + poruka);

                        if (poruka == "kraj")
                            break;

                        Console.WriteLine("Unesite poruku");
                        string odgovor = Console.ReadLine();

                        string sifrovaniOdgovor = "";
                        if (algoritam == "des")
                        {
                            DES des = new DES(poruka2, poruka3);
                            sifrovaniOdgovor = des.Encrypt(odgovor);
                        }
                        else
                        {
                            AES aes = new AES(poruka2, poruka3);
                            sifrovaniOdgovor = aes.Encrypt(odgovor);
                        }

                        brBajta = acceptedSocket.Send(Encoding.UTF8.GetBytes(sifrovaniOdgovor));
                        if (odgovor == "kraj")
                            break;
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"Doslo je do greske {ex}");
                        break;
                    }

                }

                Console.WriteLine("Server zavrsava sa radom");
                Console.ReadKey();
                acceptedSocket.Close();
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
                            //Console.WriteLine($"Desilo se {checkRead.Count} dogadjaja\n");
                            foreach (Socket s in checkRead)
                            {
                                byte[] buffer = new byte[1024];
                                int brBajta = 0;
                                try
                                {
                                    if (s.Poll(1000000, SelectMode.SelectRead)) // Proverava da li je dostupno za čitanje
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

                                            // Čeka se druga poruka (ključ)
                                            if (s.Poll(1000, SelectMode.SelectRead))
                                            {
                                                posiljaocEP = new IPEndPoint(IPAddress.Any, 0);
                                                brBajta = s.ReceiveFrom(buffer, ref posiljaocEP);
                                                poruka2 = Encoding.UTF8.GetString(buffer, 0, brBajta);
                                                Console.WriteLine($"Primljena druga poruka (ključ): {poruka2}");

                                                // Čeka se treća poruka (IV)
                                                if (s.Poll(1000, SelectMode.SelectRead))
                                                {
                                                    posiljaocEP = new IPEndPoint(IPAddress.Any, 0);
                                                    brBajta = s.ReceiveFrom(buffer, ref posiljaocEP);
                                                    poruka3 = Encoding.UTF8.GetString(buffer, 0, brBajta);
                                                    Console.WriteLine($"Primljena treća poruka (IV): {poruka3}");

                                                    // Kreiranje objekta NacinKomunikacije nakon što su primljene sve tri poruke
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
                                                DES des = new DES(komunikacija.Kljuc, komunikacija.Dodatno);
                                                poruka = des.Decrypt(sifrovanaPoruka);
                                            }
                                            else
                                            {
                                                AES aes = new AES(komunikacija.Kljuc, komunikacija.Dodatno);
                                                poruka = aes.Decrypt(sifrovanaPoruka);
                                            }
                                            Console.WriteLine("Primljena (sifrovana) poruka: " + sifrovanaPoruka);
                                            Console.WriteLine("Desifrovana poruka: " + poruka);
                                            if (poruka == "kraj")
                                                break;

                                            Console.WriteLine("Unesite poruku");
                                            string odgovor = Console.ReadLine();

                                            string sifrovaniOdgovor = "";
                                            if (komunikacija.Algoritam == "des")
                                            {
                                                DES des = new DES(komunikacija.Kljuc, komunikacija.Dodatno);
                                                sifrovaniOdgovor = des.Encrypt(odgovor);
                                            }
                                            else
                                            {
                                                AES aes = new AES(komunikacija.Kljuc, komunikacija.Dodatno);
                                                sifrovaniOdgovor = aes.Encrypt(odgovor);
                                            }

                                            brBajta = s.SendTo(Encoding.UTF8.GetBytes(sifrovaniOdgovor), komunikacija.UticnicaAdresaKlijenta);
                                            if (odgovor == "kraj")
                                            {
                                                komunikacijaLista.Remove(komunikacija);
                                                if (komunikacijaLista.Count == 0)
                                                {
                                                    Console.WriteLine("Svi klijenti su zavrsili sa radom");
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
                Console.ReadKey();
                serverSocket.Close();
            }
        }
    }
}
