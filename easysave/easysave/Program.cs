// Program.cs
using System.Net.Quic;
using System.Security.Cryptography.X509Certificates;
using BackupManager;
public class Program
{
    // Main method

    private static Manager manager = Manager.Instance; 

    public static void Main(string[] args)
    {

        var quitter = false;
        while (!quitter)
        {
            MainMenu();
            var temp = Console.ReadKey();
            var choice = temp.KeyChar.ToString();
            switch (choice)
            {
                case "1":
                    AddJob();
                    break;
                case "2":
                    UpdateJobs();
                    break;
                case "3":
                    AfficherLancerJob();
                    break;
                case "4":
                    quitter = true;
                    break;
                default:
                    Console.WriteLine($"Choix invalide {choice}. Veuillez réessayer.");
                    break;
            }
        }
    }

    static void MainMenu()
    {
        Console.WriteLine("===== EasySave =====");
        Console.WriteLine("1. Ajouter un job");
        Console.WriteLine("2. Lister les jobs");
        Console.WriteLine("3. Exécuter un job");
        Console.WriteLine("4. Quitter");
        Console.WriteLine("=============================");
        Console.Write("Votre choix : ");
    }

    static void AddJob()
    {
        Console.Clear();
        Console.WriteLine("===== Ajouter un job =====");
        Console.WriteLine("=============================");
        Console.Write("Nom de votre backup : ");
        string? name = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Le nom du job ne peut pas être vide.");
            return;
        }

        Console.Write("Chemin source : ");
        string? sourcePath = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            Console.WriteLine("Le chemin source ne peut pas être vide.");
            return;
        }

        Console.Write("Chemin cible : ");
        string? targetPath = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            Console.WriteLine("Le chemin cible ne peut pas être vide.");
            return;
        }
        Console.Write("Type de sauvegarde (0 pour complète, 1 pour différentielle) : ");
        string? typeInput = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(typeInput) || !int.TryParse(typeInput, out int type) || (type != 0 && type != 1))
        {
            Console.WriteLine("Type de sauvegarde invalide. Veuillez entrer 0 ou 1.");
            return;
        }
        // Convertir le type en booléen
        bool isDifferential = type == 1;
        // Ajouter le job de sauvegarde
        manager.AddBackupJob(sourcePath, targetPath, isDifferential, name);

        Console.WriteLine("Job ajouté avec succès.");


    }
    static void UpdateJobs()
    {
        Console.WriteLine("===== Liste des jobs =====");

        Console.WriteLine("=============================");
    }

    static void AfficherLancerJob()
    {
        Console.Clear();
        Console.WriteLine("===== Exécuter un job =====");
        Console.WriteLine("Entrez un numéro entre 0 et 4 pour un job, ou 5 pour tous les jobs.");
        Console.WriteLine("=============================");

        manager.ListBackups();

        Console.WriteLine("=============================");
        Console.Write("Votre choix : ");
        string input = Console.ReadLine();

        if (!int.TryParse(input, out int choix))
        {
            Console.WriteLine("Entrée invalide. Veuillez entrer un chiffre.");
            return;
        }

        List<int> indexes = new();

        if (choix == 5)
        {
            // Crée une liste de tous les index disponibles
            indexes = Enumerable.Range(0, manager.MaxBackups).ToList();
        }
        else if (choix >= 0 && choix < manager.MaxBackups)
        {
            indexes.Add(choix);
        }
        else
        {
            Console.WriteLine("Numéro de job invalide.");
            return;
        }
        // debug print la liste des index
        Console.WriteLine("Index de sauvegarde sélectionnés : " + string.Join(", ", indexes));

        manager.Backup(indexes);
    }



}
