using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EasySave.Models
{
    /// <summary>
    /// Singleton class responsible for managing backup jobs, their configuration, and execution.
    /// Handles adding, updating, removing, listing, and executing backup jobs.
    /// </summary>
    public class BackupManager
    {
        private static BackupManager _instance;
        private List<BackupJob> backupJobs;
        private static ConfigManager _configManager;
        private readonly object lockObject = new object();

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// Initializes the backup jobs list.
        /// </summary>
        private BackupManager()
        {
            backupJobs = new List<BackupJob>();
        }

        /// <summary>
        /// Gets the singleton instance of BackupManager.
        /// Loads configuration on first instantiation.
        /// </summary>
        /// <returns>The singleton BackupManager instance.</returns>
        public static BackupManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new BackupManager();
                _configManager = ConfigManager.GetInstance();
                _configManager.LoadConfiguration(); // Load configuration on instance creation
            }
            return _instance;
        }

        /// <summary>
        /// Adds a new backup job if it does not already exist.
        /// Attaches state and log observers to the backup strategy.
        /// Saves the updated job list to configuration.
        /// </summary>
        /// <param name="name">Name of the backup job.</param>
        /// <param name="source">Source directory path.</param>
        /// <param name="target">Target directory path.</param>
        /// <param name="type">Type of backup (Complete or Differential).</param>
        /// <returns>True if the job was added, false if a job with the same name exists.</returns>
        public bool AddBackupJob(string name, string source, string target, BackupType type)
        {
            lock (lockObject)
            {
                // Verify the job doesn't already exist
                if (backupJobs.Any(job => job.Name == name))
                {
                    return false;
                }

                // Create the appropriate strategy based on the backup type
                var strategy = CreateBackupStrategy(type);

                // Create and add the new backup job
                var job = new BackupJob(name, source, target, type, strategy);
                backupJobs.Add(job);

                // Add a state observer to the backup strategy
                StateManager stateManager = StateManager.GetInstance();
                strategy.AttachObserver(stateManager);

                // Add log observer to the backup strategy
                LogManager logManager = LogManager.GetInstance();
                strategy.AttachObserver(logManager);

                // Save the backup job to configuration
                SaveBackupJobsToConfig();

                return true;
            }
        }

        /// <summary>
        /// Updates an existing backup job with new parameters.
        /// Replaces the old job and attaches observers to the new strategy.
        /// Saves the updated job list to configuration.
        /// </summary>
        /// <param name="name">Name of the backup job to update.</param>
        /// <param name="source">New source directory path.</param>
        /// <param name="target">New target directory path.</param>
        /// <param name="type">New backup type.</param>
        /// <returns>True if the job was updated, false if not found.</returns>
        public bool UpdateBackupJob(string name, string source, string target, BackupType type)
        {
            lock (lockObject)
            {
                // Find the job to update
                int index = backupJobs.FindIndex(job => job.Name == name);
                if (index == -1)
                {
                    return false;
                }

                // Remove the old job
                backupJobs.RemoveAt(index);

                // Create a new job with updated parameters
                var strategy = CreateBackupStrategy(type);
                var job = new BackupJob(name, source, target, type, strategy);
                backupJobs.Insert(index, job);

                // Add a state observer to the backup strategy
                StateManager stateManager = StateManager.GetInstance();
                strategy.AttachObserver(stateManager);

                // Add log observer to the backup strategy
                LogManager logManager = LogManager.GetInstance();
                strategy.AttachObserver(logManager);

                // Save the backup job to configuration
                SaveBackupJobsToConfig();

                return true;
            }
        }

        /// <summary>
        /// Removes a backup job by its index in the list.
        /// Saves the updated job list to configuration.
        /// </summary>
        /// <param name="index">Index of the backup job to remove.</param>
        /// <returns>True if the job was removed, false if index is invalid.</returns>
        public bool RemoveBackup(int index)
        {
            lock (lockObject)
            {
                if (index < 0 || index >= backupJobs.Count)
                {
                    return false;
                }

                backupJobs.RemoveAt(index);
                SaveBackupJobsToConfig();

                return true;
            }
        }

        /// <summary>
        /// Returns the list of all backup jobs.
        /// </summary>
        /// <returns>List of BackupJob objects.</returns>
        public List<BackupJob> ListBackups()
        {
            return backupJobs;
        }

        /// <summary>
        /// Executes the backup jobs specified by their indices.
        /// </summary>
        /// <param name="backupIndices">List of indices of jobs to execute.</param>
        /// <param name="order">Execution order (not used in current implementation).</param>
        public void ExecuteBackupJob(List<int> backupIndices, string order)
        {
            lock (lockObject)
            {
                foreach (int index in backupIndices)
                {
                    if (index >= 0 && index < backupJobs.Count)
                    {
                        BackupJob job = backupJobs[index];
                        job.Execute();
                    }
                }
            }
        }

        /// <summary>
        /// Creates a backup strategy instance based on the backup type.
        /// </summary>
        /// <param name="type">Backup type (Complete or Differential).</param>
        /// <returns>Instance of AbstractBackupStrategy.</returns>
        /// <exception cref="ArgumentException">Thrown if the backup type is invalid.</exception>
        private AbstractBackupStrategy CreateBackupStrategy(BackupType type)
        {
            AbstractBackupStrategy strategy;

            switch (type)
            {
                case BackupType.Complete:
                    strategy = new CompleteBackupStrategy();
                    break;
                case BackupType.Differential:
                    strategy = new DifferentialBackupStrategy();
                    break;
                default:
                    throw new ArgumentException("Invalid backup type");
            }

            return strategy;
        }

        /// <summary>
        /// Serializes the current backup jobs and settings to a JSON configuration file.
        /// </summary>
        private void SaveBackupJobsToConfig()
        {
            // Prepare configuration data
            var configData = new
            {
                Settings = new
                {
                    Language = _configManager.GetSetting("Language"),
                    LogFormat = _configManager.GetSetting("LogFormat")
                },
                BackupJobs = backupJobs.ConvertAll(job => new
                {
                    Name = job.Name,
                    Source = job.Source,
                    Destination = job.Destination,
                    Type = job.Type.ToString()
                })
            };

            // Serialize data to JSON
            string json = System.Text.Json.JsonSerializer.Serialize(configData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } // Add enum converter
            });

            // Define the configuration file path
            string configFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "config.json");

            // Write data to the file
            File.WriteAllText(configFilePath, json);
        }

        public bool CanExecuteJobs()
        {
            if (_configManager.PriorityProcessIsRunning())
            {
               return false;
            }
            else
            {
                return true;
            }
            
        }

        /// <summary>
        /// Adds an observer to be notified of backup job state changes.
        /// </summary>
        /// <param name="observer">The observer to add.</param>
        public void AddToStateObserver(IStateObserver observer)
        {
            // Logic to add state observer
        }
    }
}
