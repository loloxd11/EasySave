using System;
using System.Collections.Generic;

namespace EasySave
{
    public class Program
    {
        private static CommandLineInterface cli;

        public static void Main(string[] args)
        {
            try
            {
                var configManager = ConfigManager.GetInstance();
                var backupManager = BackupManager.GetInstance();

                // Charger les jobs après l'initialisation
                configManager.LoadBackupJobs();

                // Initialize the command line interface
                cli = new CommandLineInterface();

                // Parse command line arguments if any
                if (args.Length > 0)
                {
                    ParseArgs(args);
                }
                else
                {
                    // Display menu and handle user input
                    bool exit = false;
                    while (!exit)
                    {
                        cli.Start();
                        exit = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Une erreur s'est produite : {ex.Message}");
            }
        }

        public static void ParseArgs(string[] args)
        {
            Console.WriteLine("Arguments de la ligne de commande détectés : " + string.Join(", ", args));
            if (args.Length > 0)
            {
                string command = args[0];

                // Si la commande contient une plage comme "1-3"
                if (command.Contains("-"))
                {
                    string[] range = command.Split('-');
                    if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                    {
                        List<int> indices = new List<int>();
                        for (int i = start; i <= end; i++)
                        {
                            indices.Add(i - 1); // Conversion en index 0-based
                        }

                        BackupManager.GetInstance().ExecuteBackupJob(indices);
                    }
                }
                // Si la commande contient des travaux spécifiques comme "1;3"
                else if (command.Contains(","))
                {
                    string[] jobs = command.Split(',');
                    List<int> indices = new List<int>();

                    foreach (string job in jobs)
                    {
                        if (int.TryParse(job.Trim(), out int index))
                        {
                            indices.Add(index - 1); // Conversion en index 0-based
                        }
                    }

                    if (indices.Count > 0)
                    {
                        BackupManager.GetInstance().ExecuteBackupJob(indices);
                    }
                }
                // Si la commande est un seul numéro de travail
                else if (int.TryParse(command, out int index))
                {
                    List<int> indices = new List<int> { index - 1 }; // Conversion en index 0-based
                    BackupManager.GetInstance().ExecuteBackupJob(indices);
                }
                else
                {
                    Console.WriteLine("Commande non reconnue. Utilisez un format valide : numéro, plage (1-3) ou liste (1,2,3)");
                }
            }
        }
    }
}
