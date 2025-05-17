using System;
using System.Collections.Generic;
using System.IO;

namespace EasySave
{
    public class BackupManager
    {
        private static BackupManager instance; // Singleton instance of BackupManager
        private List<BackupJob> backupJobs; // List of backup jobs
        private LanguageManager languageManager; // Language manager for translations
        private readonly Lazy<ConfigManager> lazyConfigManager = new(() => ConfigManager.GetInstance());
        private ConfigManager ConfigManager => lazyConfigManager.Value; // Lazy-loaded configuration manager

        // Private constructor to enforce singleton pattern
        private BackupManager()
        {
            backupJobs = new List<BackupJob>();
            languageManager = LanguageManager.GetInstance();

            // Load configuration settings
            ConfigManager.LoadConfiguration();

            // Create the directory for log and state files if it doesn't exist
            string logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        // Get the singleton instance of BackupManager
        public static BackupManager GetInstance()
        {
            if (instance == null)
            {
                instance = new BackupManager();
            }
            return instance;
        }

        // Add a backup job using string representation of the backup type
        public void AddBackupJob(string name, string source, string target, string backupTypeStr)
        {
            BackupType type;
            if (Enum.TryParse(backupTypeStr, out type))
            {
                AddBackupJob(name, source, target, type);
            }
            else
            {
                Console.WriteLine($"Invalid backup type: {backupTypeStr}");
            }
        }

        // Add a backup job with specified parameters
        public bool AddBackupJob(string name, string source, string target, BackupType type)
        {
            // Check if the maximum number of backup jobs (5) is reached
            if (backupJobs.Count >= 5)
            {
                Console.WriteLine(languageManager.GetTranslation("MaxBackupJobsReached"));
                return false;
            }

            // Validate the source directory
            if (!Directory.Exists(source))
            {
                Console.WriteLine(languageManager.GetTranslation("SourceDirNotFound"));
                return false;
            }

            // Create the target directory if it doesn't exist
            if (!Directory.Exists(target))
            {
                try
                {
                    Directory.CreateDirectory(target);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{languageManager.GetTranslation("TargetDirCreateFailed")}: {ex.Message}");
                    return false;
                }
            }

            // Check if a job with the same name already exists
            if (backupJobs.Exists(job => job.Name == name))
            {
                Console.WriteLine(languageManager.GetTranslation("JobNameExists"));
                return false;
            }

            // Create the appropriate backup strategy
            AbstractBackupStrategy strategy = CreateBackupStrategy(type);

            // Create and add the backup job
            BackupJob job = new BackupJob(name, source, target, type, strategy);

            // Configure observers for the job
            string stateFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "state.json");

            string logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");

            StateManager stateManager = new StateManager(stateFilePath);
            LogManager logManager = new LogManager(logDirectory);

            job.AttachObserver(stateManager);
            job.AttachObserver(logManager);

            // Initialize the job state
            job.NotifyObservers("create");

            backupJobs.Add(job);

            // Save the updated list of backup jobs to the configuration
            SaveBackupJobsToConfig();

            return true;
        }

        // Remove a backup job by its index
        public bool RemoveBackupJob(int index)
        {
            if (index >= 0 && index < backupJobs.Count)
            {
                backupJobs.RemoveAt(index);
                SaveBackupJobsToConfig();
                return true;
            }

            return false;
        }

        // Update an existing backup job
        public bool UpdateBackupJob(int index, string name, string source, string target, BackupType type)
        {
            if (index < 0 || index >= backupJobs.Count)
            {
                return false;
            }

            // Validate the source directory
            if (!Directory.Exists(source))
            {
                Console.WriteLine(languageManager.GetTranslation("SourceDirNotFound"));
                return false;
            }

            // Create the target directory if it doesn't exist
            if (!Directory.Exists(target))
            {
                try
                {
                    Directory.CreateDirectory(target);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{languageManager.GetTranslation("TargetDirCreateFailed")}: {ex.Message}");
                    return false;
                }
            }

            // Check if a job with the same name already exists (excluding the current job)
            if (backupJobs.Exists(job => job.Name == name && backupJobs.IndexOf(job) != index))
            {
                Console.WriteLine(languageManager.GetTranslation("JobNameExists"));
                return false;
            }

            // Create the appropriate backup strategy
            AbstractBackupStrategy strategy = CreateBackupStrategy(type);

            // Update the backup job
            BackupJob job = backupJobs[index];

            // Create a new job with updated parameters
            BackupJob updatedJob = new BackupJob(name, source, target, type, strategy);

            // Copy observers from the old job to the new job
            foreach (var observer in job.Observers)
            {
                updatedJob.AttachObserver(observer);
            }

            // Replace the old job with the updated job
            backupJobs[index] = updatedJob;

            // Save the updated list of backup jobs to the configuration
            SaveBackupJobsToConfig();

            return true;
        }

        // List all backup jobs
        public List<BackupJob> ListBackups()
        {
            return backupJobs;
        }

        // Execute specified backup jobs by their indices
        public void ExecuteBackupJob(List<int> backupIndices)
        {
            foreach (int index in backupIndices)
            {
                if (index >= 0 && index < backupJobs.Count)
                {
                    try
                    {
                        BackupJob job = backupJobs[index];
                        Console.WriteLine($"{languageManager.GetTranslation("ExecutingJob")}: {job.Name}");
                        job.Execute();
                        job.NotifyObservers("end");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{languageManager.GetTranslation("ErrorExecutingJob")}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"{languageManager.GetTranslation("InvalidJobIndex")}: {index + 1}");
                }
            }
        }

        // Create the appropriate backup strategy based on the backup type
        private AbstractBackupStrategy CreateBackupStrategy(BackupType type)
        {
            string stateFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "state.json");

            string logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");

            StateManager stateManager = new StateManager(stateFilePath);
            LogManager logManager = new LogManager(logDirectory);

            switch (type)
            {
                case BackupType.Complete:
                    return new CompleteBackupStrategy(stateManager, logManager);
                case BackupType.Differential:
                    return new DifferentialBackupStrategy(stateManager, logManager);
                default:
                    return new CompleteBackupStrategy(stateManager, logManager);
            }
        }

        // Save the list of backup jobs to the configuration file
        private void SaveBackupJobsToConfig()
        {
            // Get the instance of ConfigManager
            ConfigManager configManager = ConfigManager.GetInstance();

            // Prepare configuration data
            var configData = new
            {
                Settings = new
                {
                    Language = configManager.GetSetting("Language"),
                    MaxBackupJobs = configManager.GetSetting("MaxBackupJobs")
                },
                BackupJobs = backupJobs.ConvertAll(job => new
                {
                    Name = job.Name,
                    SourcePath = job.SourcePath,
                    TargetPath = job.TargetPath,
                    Type = job.Type.ToString(),
                    State = job.State.ToString(),
                    TotalFiles = job.TotalFiles,
                    TotalSize = job.TotalSize,
                    Progression = job.Progression,
                    LastFileTime = job.LastFileTime
                })
            };

            // Serialize data to JSON
            string json = System.Text.Json.JsonSerializer.Serialize(configData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Define the configuration file path
            string configFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "config.json");

            // Write data to the file
            File.WriteAllText(configFilePath, json);
        }
    }
}
