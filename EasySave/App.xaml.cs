using EasySave.Models;
using EasySave.Views;
using System.Configuration;
using System.Data;
using EasySave.Converters;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;
using System.Runtime.InteropServices;

namespace EasySave
{
    /// <summary>
    /// Main application class for EasySave WPF application.
    /// Handles application-level events and resource initialization.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Constructor for the App class.
        /// Initializes application-wide resources such as value converters.
        /// </summary>
        public App()
        {
            // Add a Boolean to Visibility converter to application resources
            Resources.Add("BoolToVis", new BooleanToVisibilityConverter());
            // Add a NotNull converter to application resources
            Resources.Add("NotNullConverter", new NotNullConverter());
        }

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigManager.GetInstance().LoadConfiguration();
            EncryptionService.GetInstance();

            if (e.Args.Length > 0)
            {
                AllocConsole();
                // Redirige la sortie standard vers la nouvelle console
                var stdOut = System.Console.OpenStandardOutput();
                var writer = new System.IO.StreamWriter(stdOut, System.Text.Encoding.UTF8) { AutoFlush = true };
                System.Console.OutputEncoding = System.Text.Encoding.UTF8;
                System.Console.SetOut(writer);
                System.Console.SetError(writer);

                if (TryRunConsoleMode(e.Args))
                {
                    Console.WriteLine("Appuyez sur une touche pour quitter...");
                    Console.ReadKey();
                    Shutdown();
                    return;
                }
            }
            else
            {
                base.OnStartup(e);
            }
        }


        private bool TryRunConsoleMode(string[] args)
        {
            // Charger les jobs sauvegardés (adapter selon votre logique)
            BackupManager backupManager = BackupManager.GetInstance();
            var jobs = backupManager.ListBackups();

            // Afficher la liste des jobs détectés
            Console.WriteLine("Jobs détectés :");
            for (int i = 0; i < jobs.Count; i++)
            {
                var job = jobs[i];
                Console.WriteLine($"{i + 1}. Nom: {job.Name} | Source: {job.Source} | Destination: {job.Destination} | Type: {job.Type}");
            }
            Console.WriteLine();

            // Parser les arguments
            var jobsToRun = ParseJobArguments(args[0], jobs.Count);
            if (jobsToRun == null || jobsToRun.Count == 0)
            {
                Console.WriteLine("Aucun job à exécuter.");
                return true;
            }

            foreach (var idx in jobsToRun)
            {
                if (idx < 1 || idx > jobs.Count)
                {
                    Console.WriteLine($"Job {idx} inexistant.");
                    continue;
                }
                var job = jobs[idx - 1];
                Console.WriteLine($"---\nExécution du job {idx} :");
                Console.WriteLine($"Nom: {job.Name}");
                Console.WriteLine($"Source: {job.Source}");
                Console.WriteLine($"Destination: {job.Destination}");
                Console.WriteLine($"Type: {job.Type}");
                bool result = job.ExecuteJob();
                Console.WriteLine(result ? "Succès" : "Échec");
            }
            return true;
        }

        private List<int> ParseJobArguments(string arg, int maxJob)
        {
            var result = new List<int>();
            string completeArg = arg;
            if (completeArg.Contains('-'))
            {
                var parts = completeArg.Split('-');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int start) &&
                    int.TryParse(parts[1], out int end))
                {
                    for (int i = start; i <= end && i <= maxJob; i++)
                        result.Add(i);
                }
            }
            else if (completeArg.Contains(';'))
            {
                // Par celle-ci pour ignorer les éléments vides et les espaces :
                var parts = completeArg.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var p in parts)
                    if (int.TryParse(p, out int idx))
                        result.Add(idx);
            }
            else if (int.TryParse(completeArg, out int single))
            {
                result.Add(single);
            }
            return result;
        }
    }

}
