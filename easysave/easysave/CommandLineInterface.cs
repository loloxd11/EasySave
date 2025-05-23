using System;
using System.Collections.Generic;
using System.IO;

namespace EasySave
{
    /// <summary>
    /// CommandLineInterface class provides a command-line interface for managing backup jobs.
    /// It allows users to add, update, remove, execute, and list backup jobs, as well as change application settings.
    /// </summary>
    public class CommandLineInterface
    {
        private readonly BackupManager backupManager;
        private readonly LanguageManager languageManager;
        private readonly ConfigManager configManager;
        private readonly LogManager logManager;

        /// <summary>
        /// Constructor to initialize the CommandLineInterface and its dependencies.
        /// It sets up the backup manager, language manager, configuration manager, and log manager.
        /// </summary>
        public CommandLineInterface()
        {
            backupManager = BackupManager.GetInstance();
            languageManager = LanguageManager.GetInstance();
            configManager = ConfigManager.GetInstance();

            string logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");
            logManager = LogManager.GetInstance(logDirectory);

            // Set the language based on the configuration settings
            string language = configManager.GetSetting("Language");
            if (!string.IsNullOrEmpty(language))
            {
                languageManager.SetLanguage(language);
            }
        }

        /// <summary>
        /// Main entry point for the Command Line Interface.
        /// Displays the main menu and handles user input to navigate through different functionalities.
        /// </summary>
        public void Start()
        {
            bool exit = false;

            while (!exit)
            {
                DisplayMainMenu(); // Display the main menu
                string choice = Console.ReadLine();

                // Handle user input and navigate to the appropriate menu
                switch (choice)
                {
                    case "1":
                        AddBackupMenu();
                        break;
                    case "2":
                        UpdateOrRemoveBackupMenu();
                        break;
                    case "3":
                        ExecuteBackupMenu();
                        break;
                    case "4":
                        ListBackups();
                        break;
                    case "5":
                        ChangeLanguageMenu();
                        break;
                    case "6":
                        ChangeLogFormatMenu();
                        break;
                    case "7":
                        exit = true;
                        Console.WriteLine(languageManager.GetTranslation("MenuExit"));
                        break;
                    default:
                        Console.WriteLine(languageManager.GetTranslation("UnknownCommand"));
                        break;
                }
            }
        }

        /// <summary>
        /// Displays the main menu options to the user.
        /// </summary>
        private void DisplayMainMenu()
        {
            Console.WriteLine("===== EasySave =====");
            Console.WriteLine("1. " + languageManager.GetTranslation("MenuAddJob")); // Add a backup job
            Console.WriteLine("2. " + languageManager.GetTranslation("MenuUpdateJob")); // Update or remove a backup job
            Console.WriteLine("3. " + languageManager.GetTranslation("MenuExecuteJob")); // Execute a backup job
            Console.WriteLine("4. " + languageManager.GetTranslation("MenuListJobs")); // List all backup jobs
            Console.WriteLine("5. " + languageManager.GetTranslation("MenuChangeLanguage")); // Change the language
            Console.WriteLine("6. " + languageManager.GetTranslation("MenuFormatLog"));
            Console.WriteLine("7. " + languageManager.GetTranslation("MenuExit")); // Exit the application
            Console.WriteLine("=============================");
            Console.Write("Your choice: ");
        }

        /// <summary>
        /// Menu to add a new backup job.
        /// Prompts the user for job details and validates the input before adding the job.
        /// </summary>
        private void AddBackupMenu()
        {
            Console.Clear();
            Console.WriteLine("=========== " + languageManager.GetTranslation("MenuAddJob") + " ===========");
            Console.Write(languageManager.GetTranslation("PromptJobName")); // Prompt for job name
            string name = Console.ReadLine();

            Console.Write(languageManager.GetTranslation("PromptJobSrc")); // Prompt for source path
            string source = Console.ReadLine();

            Console.Write(languageManager.GetTranslation("PromptJobDst")); // Prompt for target path
            string target = Console.ReadLine();

            Console.Write(languageManager.GetTranslation("PromptJobType")); // Prompt for backup type
            string typeInput = Console.ReadLine();

            // Validate input and ensure all fields are correctly filled
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target) || !int.TryParse(typeInput, out int type) || (type != 0 && type != 1))
            {
                Console.WriteLine(languageManager.GetTranslation("InvalidInput"));
                return;
            }

            // Remove surrounding quotes from paths if present
            if (source.StartsWith("\"") && source.EndsWith("\""))
            {
                source = source.Trim('"');
            }

            if (target.StartsWith("\"") && target.EndsWith("\""))
            {
                target = target.Trim('"');
            }

            // Determine the backup type (Complete or Differential)
            BackupType backupType = type == 0 ? BackupType.Complete : BackupType.Differential;
            bool success = backupManager.AddBackupJob(name, source, target, backupType);

            // Display success or error message
            if (success)
            {
                Console.WriteLine(languageManager.GetTranslation("JobAdded"));
            }
            else
            {
                Console.WriteLine(languageManager.GetTranslation("JobNameExists"));
            }
        }

        /// <summary>
        /// Menu to update or remove an existing backup job.
        /// Displays the list of jobs and allows the user to select a job to update or remove.
        /// </summary>
        private void UpdateOrRemoveBackupMenu()
        {
            Console.Clear();
            Console.WriteLine("=====" + languageManager.GetTranslation("AddOrDeletetTitle") + "=====");
            ListBackups(); // Display the list of backup jobs

            Console.Write(languageManager.GetTranslation("PromptAddOrDelete")); // Prompt for job index
            string input = Console.ReadLine();

            // Validate the selected index
            if (!int.TryParse(input, out int index) || index < 1 || index > backupManager.ListBackups().Count)
            {
                Console.WriteLine(languageManager.GetTranslation("InvalidJobIndex"));
                return;
            }

            // Display options to update or remove the job
            Console.WriteLine("1. " + languageManager.GetTranslation("MenuUpdateJob")); // Update job
            Console.WriteLine("2. " + languageManager.GetTranslation("MenuRemoveJob")); // Remove job
            Console.Write("Your choice: ");
            string choice = Console.ReadLine();

            if (choice == "1")
            {
                UpdateBackup(index - 1); // Update the selected job
            }
            else if (choice == "2")
            {
                RemoveBackup(index - 1); // Remove the selected job
            }
            else
            {
                Console.WriteLine(languageManager.GetTranslation("UnknownCommand"));
            }
        }

        /// <summary>
        /// Updates an existing backup job with new parameters.
        /// Prompts the user for updated job details and validates the input.
        /// </summary>
        private void UpdateBackup(int index)
        {
            Console.Clear();
            Console.WriteLine("=====" + languageManager.GetTranslation("MenuUpdateJob") + "=====");
            Console.Write(languageManager.GetTranslation("PromptJobName")); // Prompt for new job name
            string name = Console.ReadLine();

            Console.Write(languageManager.GetTranslation("PromptJobSrc")); // Prompt for new source path
            string source = Console.ReadLine();

            Console.Write(languageManager.GetTranslation("PromptJobDst")); // Prompt for new target path
            string target = Console.ReadLine();

            Console.Write(languageManager.GetTranslation("PromptJobType")); // Prompt for new backup type
            string typeInput = Console.ReadLine();

            // Validate input and ensure all fields are correctly filled
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target) || !int.TryParse(typeInput, out int type) || (type != 0 && type != 1))
            {
                Console.WriteLine(languageManager.GetTranslation("InvalidInput"));
                return;
            }

            // Determine the backup type (Complete or Differential)
            BackupType backupType = type == 0 ? BackupType.Complete : BackupType.Differential;
            bool success = backupManager.UpdateBackupJob(index, name, source, target, backupType);

            // Display success or error message
            if (success)
            {
                Console.WriteLine(languageManager.GetTranslation("JobUpdated"));
            }
            else
            {
                Console.WriteLine(languageManager.GetTranslation("JobNotFound"));
            }
        }

        /// <summary>
        /// Removes an existing backup job by its index.
        /// Displays a success or error message based on the operation result.
        /// </summary>
        private void RemoveBackup(int index)
        {
            Console.Clear();
            bool success = backupManager.RemoveBackupJob(index);

            // Display success or error message
            if (success)
            {
                Console.WriteLine(languageManager.GetTranslation("JobRemoved"));
            }
            else
            {
                Console.WriteLine(languageManager.GetTranslation("JobNotFound"));
            }
        }

        /// <summary>
        /// Menu to execute one or more backup jobs.
        /// Allows the user to execute all jobs or specific jobs by their indices.
        /// </summary>
        private void ExecuteBackupMenu()
        {
            Console.Clear();
            Console.WriteLine("=====" + languageManager.GetTranslation("MenuExecuteJob") + "=====");
            ListBackups(); // Display the list of backup jobs

            Console.Write(languageManager.GetTranslation("PromptExecuteJobIndex")); // Prompt for job index or "all"
            string input = Console.ReadLine();

            List<int> indices = new List<int>();

            // Handle "all" input to execute all jobs
            if (input.ToLower() == "all")
            {
                indices = new List<int>();
                for (int i = 0; i < backupManager.ListBackups().Count; i++)
                {
                    indices.Add(i);
                }
            }
            // Handle specific job index
            else if (int.TryParse(input, out int index) && index >= 1 && index <= backupManager.ListBackups().Count)
            {
                indices.Add(index - 1);
            }
            else
            {
                Console.WriteLine(languageManager.GetTranslation("InvalidJobIndex"));
                return;
            }

            // Execute the selected backup jobs
            backupManager.ExecuteBackupJob(indices);
            Console.WriteLine(languageManager.GetTranslation("ExecutingJob"));
        }

        /// <summary>
        /// Displays the list of all backup jobs.
        /// If no jobs are defined, it shows a message indicating the absence of jobs.
        /// </summary>
        private void ListBackups()
        {
            Console.Clear();
            Console.WriteLine("=====" + languageManager.GetTranslation("ListJobs") + "=====");
            var backups = backupManager.ListBackups();

            // Check if there are no jobs defined
            if (backups.Count == 0)
            {
                Console.WriteLine(languageManager.GetTranslation("NoJobsDefined"));
                return;
            }

            // Display each backup job with its details
            for (int i = 0; i < backups.Count; i++)
            {
                var job = backups[i];
                Console.WriteLine($"{i + 1}. {job.Name} ({job.Type}): {job.SourcePath} -> {job.TargetPath}");
            }
        }

        /// <summary>
        /// Menu to change the application's language.
        /// Prompts the user for a new language and updates the configuration accordingly.
        /// </summary>
        private void ChangeLanguageMenu()
        {
            Console.Clear();
            Console.WriteLine("=====" + languageManager.GetTranslation("MenuChangeLanguage") + "=====");
            Console.WriteLine(languageManager.GetTranslation("PromptChangeLanguage")); // Prompt for new language
            string language = Console.ReadLine();

            // Validate and set the new language
            if (language.ToLower() == "en" || language.ToLower() == "fr")
            {
                languageManager.SetLanguage(language.ToLower());
                configManager.SetSetting("Language", language.ToLower());
                Console.WriteLine(languageManager.GetTranslation("LanguageChanged"));
            }
            else
            {
                Console.WriteLine(languageManager.GetTranslation("InvalidLanguage"));
            }
        }

        /// <summary>
        /// Menu to change the log format.
        /// Allows the user to select between JSON and XML formats for logging.
        /// </summary>
        private void ChangeLogFormatMenu()
        {
            Console.Clear();
            Console.WriteLine("===== Change Log Format =====");
            Console.WriteLine("1. JSON");
            Console.WriteLine("2. XML");
            Console.Write("Your choice: ");
            string input = Console.ReadLine();

            if (input == "1")
            {
                logManager.SetFormat(LogLibrary.Enums.LogFormat.JSON);
                configManager.SetSetting("LogFormat", "JSON");
                Console.WriteLine("Log format changed to JSON.");
            }
            else if (input == "2")
            {
                logManager.SetFormat(LogLibrary.Enums.LogFormat.XML);
                configManager.SetSetting("LogFormat", "XML");
                Console.WriteLine("Log format changed to XML.");
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }
    }
}
