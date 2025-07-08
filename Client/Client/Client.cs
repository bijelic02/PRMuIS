using Client.Pomocne_metode;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Client
{
    public class Client
    {
        static void Main(string[] args)
        {
            Socket clientSocket;
            IPEndPoint destEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 65000);

            Console.WriteLine("Aplikacija je pokrenuta.\n");
            Console.WriteLine("Izaberite koji protokol zelite da koristite (UDP ili TCP): ");
            string? protokol = Console.ReadLine();

            if (protokol?.ToLower() == "tcp")
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    clientSocket.Connect(destEP);
                    Console.WriteLine("Klijent je uspesno povezan sa serverom!");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Greska pri povezivanju sa serverom: {ex.SocketErrorCode}");
                    return;
                }
            }
            else if (protokol?.ToLower() == "udp")
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            else
            {
                Console.WriteLine("Za tip protokola niste uneli validnu vrednost. Aplikacija prestaje sa radom.\n");
                return;
            }

            Console.WriteLine("Izaberite koji algoritam sifrovanja zelite da koristite (DES ili AES): ");
            string? algoritam = Console.ReadLine();

            if (algoritam?.ToLower() != "des" && algoritam?.ToLower() != "aes")
            {
                Console.WriteLine("Za algoritam sifrovanja niste uneli validnu vrednost. Aplikacija prestaje sa radom.\n");
                clientSocket.Close();
                return;
            }

            string key;
            string iv;
            if (algoritam?.ToLower() == "des")
            {
                key = "12345678";
                iv = "87654321";
            }
            else
            {
                key = "1234567890abcdef";
                iv = "fedcba0987654321";
            }

            // sad treba da salje poruke serveru
            // prvo salje hesirani naziv algoritma, pa kljuc i iv

            if (protokol?.ToLower() == "udp")
            {
                byte[] buffer = new byte[1024];
                try
                {
                    Hesiranje hes = new Hesiranje();
                    string hesiranAlg = hes.Hesiraj(algoritam);
                    byte[] binarno1 = Encoding.UTF8.GetBytes(hesiranAlg);
                    int brBajta1 = clientSocket.SendTo(binarno1, 0, binarno1.Length, SocketFlags.None, destEP);

                    byte[] binarno2 = Encoding.UTF8.GetBytes(key);
                    int brBajta2 = clientSocket.SendTo(binarno2, 0, binarno2.Length, SocketFlags.None, destEP);

                    byte[] binarno3 = Encoding.UTF8.GetBytes(iv);
                    int brBajta3 = clientSocket.SendTo(binarno3, 0, binarno3.Length, SocketFlags.None, destEP);

                    while (true)
                    {
                        Console.WriteLine("Unesite poruku:");
                        string poruka = Console.ReadLine();

                        // sifruje se poruka pa se salje
                        string sifrovanaPoruka = "";
                        if (algoritam.ToLower() == "des")
                        {
                            Crypto.DES des = new Crypto.DES(key, iv);
                            sifrovanaPoruka = des.Encrypt(poruka);

                        }
                        else if (algoritam.ToLower() == "aes")
                        {
                            Crypto.AES aes = new Crypto.AES(key, iv);
                            sifrovanaPoruka = aes.Encrypt(poruka);
                        }
                        Console.WriteLine("Sifrovana poruka: " + sifrovanaPoruka);

                        byte[] binarnaPoruka = Encoding.UTF8.GetBytes(sifrovanaPoruka);
                        int brBajta = clientSocket.SendTo(binarnaPoruka, 0, binarnaPoruka.Length, SocketFlags.None, destEP); // Poruka koju saljemo u binarnom zapisu, pocetak poruke, duzina, flegovi, odrediste

                        if (poruka == "kraj")
                            break;

                        EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);
                        brBajta = clientSocket.ReceiveFrom(buffer, ref posiljaocEP);

                        if (brBajta == 0)
                        {
                            Console.WriteLine("Server je zavrsio sa radom");
                            break;
                        }

                        string odgovor = Encoding.UTF8.GetString(buffer, 0, brBajta);
                        string desifrovaniOdgovor = "";
                        if (algoritam.ToLower() == "des")
                        {
                            Crypto.DES des = new Crypto.DES(key, iv);
                            desifrovaniOdgovor = des.Decrypt(odgovor);
                        }
                        else
                        {
                            Crypto.AES aes = new Crypto.AES(key, iv);
                            desifrovaniOdgovor = aes.Decrypt(odgovor);
                        }

                        Console.WriteLine("Primljeni (sifrovani) odgovor: " + odgovor);
                        Console.WriteLine("Desifrovani odgovor: " + desifrovaniOdgovor);

                        if (desifrovaniOdgovor == "kraj")
                            break;
                    }

                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Doslo je do greske tokom slanja poruke: \n{ex}");
                }

                Console.WriteLine("Klijent zavrsava sa radom");
                clientSocket.Close(); // Zatvaramo soket na kraju rada
                Console.ReadKey();
            }
            else
            {
                try
                {
                    Hesiranje hes = new Hesiranje();
                    string hesiranAlg = hes.Hesiraj(algoritam);
                    byte[] binarno1 = Encoding.UTF8.GetBytes(hesiranAlg);
                    int brBajta1 = clientSocket.Send(binarno1);

                    byte[] binarno2 = Encoding.UTF8.GetBytes(key);
                    int brBajta2 = clientSocket.Send(binarno2);

                    byte[] binarno3 = Encoding.UTF8.GetBytes(iv);
                    int brBajta3 = clientSocket.Send(binarno3);

                    byte[] buffer = new byte[1024];
                    while (true)
                    {
                        Console.WriteLine("Unesite poruku");
                        try
                        {
                            string poruka = Console.ReadLine();
                            string sifrovanaPoruka = "";

                            if (algoritam.ToLower() == "des")
                            {
                                Crypto.DES des = new Crypto.DES(key, iv);
                                sifrovanaPoruka = des.Encrypt(poruka);

                            }
                            else if (algoritam.ToLower() == "aes")
                            {
                                Crypto.AES aes = new Crypto.AES(key, iv);
                                sifrovanaPoruka = aes.Encrypt(poruka);
                            }
                            Console.WriteLine("Sifrovana poruka: " + sifrovanaPoruka);

                            int brBajta = clientSocket.Send(Encoding.UTF8.GetBytes(sifrovanaPoruka));

                            if (poruka == "kraj")
                                break;

                            brBajta = clientSocket.Receive(buffer);

                            if (brBajta == 0)
                            {
                                Console.WriteLine("Server je zavrsio sa radom");
                                break;
                            }

                            string odgovor = Encoding.UTF8.GetString(buffer, 0, brBajta);
                            string desifrovaniOdgovor = "";
                            if (algoritam.ToLower() == "des")
                            {
                                Crypto.DES des = new Crypto.DES(key, iv);
                                desifrovaniOdgovor = des.Decrypt(odgovor);
                            }
                            else
                            {
                                Crypto.AES aes = new Crypto.AES(key, iv);
                                desifrovaniOdgovor = aes.Decrypt(odgovor);
                            }

                            Console.WriteLine("Primljeni (sifrovani) odgovor: " + odgovor);
                            Console.WriteLine("Desifrovani odgovor: " + desifrovaniOdgovor);

                            if (desifrovaniOdgovor == "kraj")
                                break;
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine($"Doslo je do greske tokom slanja:\n{ex}");
                            break;
                        }

                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Doslo je do greske: \n{ex}");
                }
                Console.WriteLine("Klijent zavrsava sa radom");
                clientSocket.Close(); // Zatvaramo soket na kraju rada
                Console.ReadKey();
            }
        }
    }
}