using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Server
    {
        static void Main(string[] args)
        {
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
                string poruka1 = Encoding.UTF8.GetString(bafer1);
                Console.WriteLine(poruka1);

                byte[] bafer2 = new byte[8];
                int brBajta2 = acceptedSocket.Receive(bafer2);
                string poruka2 = Encoding.UTF8.GetString(bafer2);
                Console.WriteLine(poruka2);

                byte[] bafer3 = new byte[8];
                int brBajta3 = acceptedSocket.Receive(bafer3);
                string poruka3 = Encoding.UTF8.GetString(bafer3);
                Console.WriteLine(poruka3);

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
                        string poruka = Encoding.UTF8.GetString(buffer);
                        if (poruka == "kraj")
                            break;
                        Console.WriteLine(poruka);

                        Console.WriteLine("Unesite poruku");
                        string odgovor = Console.ReadLine();

                        brBajta = acceptedSocket.Send(Encoding.UTF8.GetBytes(odgovor));
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
                EndPoint posiljaocEP1 = new IPEndPoint(IPAddress.Any, 0);
                EndPoint posiljaocEP2 = new IPEndPoint(IPAddress.Any, 0);
                EndPoint posiljaocEP3 = new IPEndPoint(IPAddress.Any, 0);

                byte[] bafer1 = new byte[1024];
                int brBajta1 = serverSocket.ReceiveFrom(bafer1, ref posiljaocEP1);
                string poruka1 = Encoding.UTF8.GetString(bafer1, 0, brBajta1);
                Console.WriteLine(poruka1);

                byte[] bafer2 = new byte[100];
                int brBajta2 = serverSocket.ReceiveFrom(bafer2, ref posiljaocEP2);
                string poruka2 = Encoding.UTF8.GetString(bafer2, 0, brBajta2);
                Console.WriteLine(poruka2);

                byte[] bafer3 = new byte[100];
                int brBajta3 = serverSocket.ReceiveFrom(bafer3, ref posiljaocEP3);
                string poruka3 = Encoding.UTF8.GetString(bafer3, 0, brBajta3);
                Console.WriteLine(poruka3);

                byte[] buffer = new byte[1024];
                while (true)
                {
                    try
                    {
                        int brBajta = serverSocket.ReceiveFrom(buffer, ref posiljaocEP);
                        if (brBajta == 0)
                        {
                            Console.WriteLine("Klijent je zavrsio sa radom");
                            break;
                        }
                        string poruka = Encoding.UTF8.GetString(buffer, 0, brBajta);

                        if (poruka == "kraj")
                            break;
                        Console.WriteLine(poruka);

                        Console.WriteLine("Unesite poruku");
                        string odgovor = Console.ReadLine();

                        brBajta = serverSocket.SendTo(Encoding.UTF8.GetBytes(odgovor), posiljaocEP);
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
                serverSocket.Close();
            }
        }
    }
}
