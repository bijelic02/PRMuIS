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
            Console.ReadLine(); // Sprečava zatvaranje konzole odmah
        }

        static void PokreniKlijente(int brojKlijenata)
        {
            for (int i = 0; i < brojKlijenata; i++)
            {
                string currentExecutableDirectory = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"[DEBUG] currentExecutableDirectory: {currentExecutableDirectory}");

                // Navigacija do glavnog foldera projekta ("Roditeljski_Projekat_Folder")
                // ClientLauncher.exe je u: .../Roditeljski_Projekat_Folder/ClientLauncher/bin/Debug/net8.0/
                // Potrebno je ići 4 nivoa gore da bi se došlo do "Roditeljski_Projekat_Folder"

                // Kreiramo DirectoryInfo objekat za trenutni direktorijum izvršnog fajla
                DirectoryInfo currentDirInfo = new DirectoryInfo(currentExecutableDirectory);

                // Navigujemo 4 nivoa gore koristeći Parent svojstvo
                // Svaki put proveravamo da li Parent postoji pre nego što nastavimo
                DirectoryInfo mainProjectRootInfo = currentDirInfo;
                for (int j = 0; j < 4; j++)
                {
                    if (mainProjectRootInfo?.Parent != null)
                    {
                        mainProjectRootInfo = mainProjectRootInfo.Parent;
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] Ne mogu da pronađem glavni koren projekta. Putanja je prekratka na nivou {i + 1}.");
                        return; // Prekida izvršavanje ako ne možemo da se popnemo dovoljno visoko
                    }
                }

                string mainProjectRoot = mainProjectRootInfo.FullName;
                Console.WriteLine($"[DEBUG] mainProjectRoot: {mainProjectRoot}");

                // Izgradnja pune apsolutne putanje do klijentskog izvršnog fajla
                // Client.exe je u: .../Roditeljski_Projekat_Folder/Projekat/bin/Debug/net8.0/Client.exe
                string clientPath = Path.Combine(mainProjectRoot, "Client", "bin", "Debug", "net8.0", "Client.exe");
                Console.WriteLine($"[DEBUG] clientPath (izračunata): {clientPath}");

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

