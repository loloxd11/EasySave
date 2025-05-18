using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EasySave.Models
{
    public class BackupManager
    {
        private static BackupManager _instance;
        private List<BackupJob> backupJobs;
        private static ConfigManager _configManager;
        private readonly object lockObject = new object();

        private BackupManager()
        {
            backupJobs = new List<BackupJob>();

        }

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

                // Save the backup job to configuration
                SaveBackupJobsToConfig();

                return true;
            }
        }

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

                // Save the backup job to configuration
                SaveBackupJobsToConfig();

                return true;
            }
        }

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

        public List<BackupJob> ListBackups()
        {
                return backupJobs;
        }

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
                    SourcePath = job.Source,
                    TargetPath = job.Destination,
                    Type = job.Type.ToString()
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

        public void AddToStateObserver(IStateObserver observer)
        {
            // Logic to add state observer
        }
    }
}
