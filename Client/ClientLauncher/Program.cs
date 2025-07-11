using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLauncher
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Unesite broj klijenata za pokretanje: ");
            int brojKlijenata = int.Parse(Console.ReadLine());

            PokreniKlijente(brojKlijenata);

            Console.WriteLine("Svi klijenti su pokrenuti.");
            Console.ReadLine(); 
        }

        static void PokreniKlijente(int brojKlijenata)
        {
            for (int i = 0; i < brojKlijenata; i++)
            {
                string currentExecutableDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // ClientLauncher.exe je u .../Client/ClientLauncher/bin/Debug/net8.0/
                // treva ici 4 nivoa gore

                DirectoryInfo currentDirInfo = new DirectoryInfo(currentExecutableDirectory);
                DirectoryInfo mainProjectRootInfo = currentDirInfo;
                for (int j = 0; j < 4; j++)
                {
                    if (mainProjectRootInfo?.Parent != null)
                    {
                        mainProjectRootInfo = mainProjectRootInfo.Parent;
                    }
                    else
                    {
                        return;
                    }
                }

                string mainProjectRoot = mainProjectRootInfo.FullName;
                
                // Client.exe je u .../Client/Client/bin/Debug/net8.0/Client.exe
                string clientPath = Path.Combine(mainProjectRoot, "Client", "bin", "Debug", "net8.0", "Client.exe");
               
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"Klijent {i + 1}\" \"{clientPath}\" {i + 1}",
                    WindowStyle = ProcessWindowStyle.Normal
                };

                Process.Start(psi);

                Console.WriteLine($"Pokrenut klijent #{i + 1}");
            }
        }
    }
}

