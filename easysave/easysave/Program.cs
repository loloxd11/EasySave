using System;
using System.Collections.Generic;
using LogLibrary.Enums;

namespace EasySave
{
    public class Program
    {
        private static CommandLineInterface cli;

        /// <summary>
        /// Entry point of the application.
        /// Initializes configuration, logging, and backup management.
        /// Handles command-line arguments or starts the CLI for user interaction.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        public static void Main(string[] args)
        {
            try
            {
                // Retrieve singleton instances of ConfigManager and BackupManager
                var configManager = ConfigManager.GetInstance();
                var backupManager = BackupManager.GetInstance();

                // Initialize LogManager with the format from the configuration
                string logFormat = configManager.GetSetting("LogFormat") ?? "XML";
                LogFormat format = logFormat.Equals("JSON", StringComparison.OrdinalIgnoreCase)
                    ? LogLibrary.Enums.LogFormat.JSON
                    : LogLibrary.Enums.LogFormat.XML;

                string logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EasySave", "Logs");

                LogManager.GetInstance(logDirectory, format);

                // Load backup jobs from the configuration file
                configManager.LoadBackupJobs();

                // Initialize the Command Line Interface (CLI)
                cli = new CommandLineInterface();

                // Check if command-line arguments are provided
                if (args.Length > 0)
                {
                    // Parse and execute commands from the arguments
                    ParseArgs(args);
                }
                else
                {
                    // Display the CLI menu and handle user input
                    bool exit = false;
                    while (!exit)
                    {
                        cli.Start();
                        exit = true; // Exit after one iteration (can be modified for continuous interaction)
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle and display any unexpected errors
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses and executes commands provided as command-line arguments.
        /// Supports single job execution, ranges, and lists of jobs.
        /// </summary>
        /// <param name="args">Array of command-line arguments.</param>
        public static void ParseArgs(string[] args)
        {
            // Display the detected command-line arguments
            Console.WriteLine("Command-line arguments detected: " + string.Join(", ", args));
            if (args.Length > 0)
            {
                string command = args[0];

                // If the command specifies a range (e.g., "1-3")
                if (command.Contains("-"))
                {
                    string[] range = command.Split('-');
                    if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                    {
                        List<int> indices = new List<int>();
                        for (int i = start; i <= end; i++)
                        {
                            indices.Add(i - 1); // Convert to 0-based index
                        }

                        // Execute the backup jobs for the specified range
                        BackupManager.GetInstance().ExecuteBackupJob(indices);
                    }
                }
                // If the command specifies specific jobs (e.g., "1,3")
                else if (command.Contains(","))
                {
                    string[] jobs = command.Split(',');
                    List<int> indices = new List<int>();

                    foreach (string job in jobs)
                    {
                        if (int.TryParse(job.Trim(), out int index))
                        {
                            indices.Add(index - 1); // Convert to 0-based index
                        }
                    }

                    if (indices.Count > 0)
                    {
                        // Execute the backup jobs for the specified indices
                        BackupManager.GetInstance().ExecuteBackupJob(indices);
                    }
                }
                // If the command specifies a single job number
                else if (int.TryParse(command, out int index))
                {
                    List<int> indices = new List<int> { index - 1 }; // Convert to 0-based index
                                                                     // Execute the backup job for the specified index
                    BackupManager.GetInstance().ExecuteBackupJob(indices);
                }
                else
                {
                    // Display an error message for unrecognized commands
                    Console.WriteLine("Unrecognized command. Use a valid format: single number, range (1-3), or list (1,2,3)");
                }
            }
        }
    }
}
