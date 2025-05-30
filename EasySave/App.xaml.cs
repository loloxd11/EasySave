using EasySave.Converters;
using EasySave.Models;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;

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

        /// <summary>
        /// Imports the AllocConsole function from kernel32.dll to allocate a new console for the application.
        /// </summary>
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        /// <summary>
        /// Handles application startup logic.
        /// Loads configuration, initializes encryption service, and determines if the application should run in console mode or WPF mode.
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Load application configuration
            ConfigManager.GetInstance().LoadConfiguration();
            // Initialize encryption service
            EncryptionService.GetInstance();

            // If command-line arguments are provided, run in console mode
            if (e.Args.Length > 0)
            {
                AllocConsole();
                // Redirect standard output to the new console
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
                // Default WPF startup
                base.OnStartup(e);
            }
        }

        /// <summary>
        /// Runs the application in console mode, allowing execution of backup jobs via command-line arguments.
        /// </summary>
        /// <param name="args">Command-line arguments specifying which jobs to run.</param>
        /// <returns>True if console mode was handled, otherwise false.</returns>
        private bool TryRunConsoleMode(string[] args)
        {
            // Load saved backup jobs
            BackupManager backupManager = BackupManager.GetInstance();
            var jobs = backupManager.ListBackups();

            // Display detected jobs
            Console.WriteLine("Jobs détectés :");
            for (int i = 0; i < jobs.Count; i++)
            {
                var job = jobs[i];
                Console.WriteLine($"{i + 1}. Nom: {job.Name} | Source: {job.Source} | Destination: {job.Destination} | Type: {job.Type}");
            }
            Console.WriteLine();

            // Parse job indices from arguments
            var jobsToRun = ParseJobArguments(args[0], jobs.Count);
            if (jobsToRun == null || jobsToRun.Count == 0)
            {
                Console.WriteLine("Aucun job à exécuter.");
                return true;
            }

            // Execute selected jobs
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

        /// <summary>
        /// Parses a string argument to determine which backup jobs to execute.
        /// Supports single indices, ranges (e.g., "1-3"), and lists (e.g., "1;2;4").
        /// </summary>
        /// <param name="arg">Argument string specifying job indices.</param>
        /// <param name="maxJob">Maximum valid job index.</param>
        /// <returns>List of job indices to execute.</returns>
        private List<int> ParseJobArguments(string arg, int maxJob)
        {
            var result = new List<int>();
            string completeArg = arg;
            // Handle range (e.g., "1-3")
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
            // Handle list (e.g., "1;2;4")
            else if (completeArg.Contains(';'))
            {
                var parts = completeArg.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var p in parts)
                    if (int.TryParse(p, out int idx))
                        result.Add(idx);
            }
            // Handle single index
            else if (int.TryParse(completeArg, out int single))
            {
                result.Add(single);
            }
            return result;
        }
    }
}
