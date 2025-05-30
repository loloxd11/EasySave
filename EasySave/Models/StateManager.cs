using EasySave.Views;
using Microsoft.VisualBasic.Logging;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace EasySave.Models
{
    /// <summary>
    /// Manages the state of backup jobs, persists their state to a file, and notifies observers of state changes.
    /// Implements the singleton pattern.
    /// </summary>
    public class StateManager : IObserver
    {
        /// <summary>
        /// List of observers interested in state changes.
        /// </summary>
        private List<IStateObserver> stateObservers;

        /// <summary>
        /// Singleton instance of StateManager.
        /// </summary>
        private static StateManager instance;

        /// <summary>
        /// Path to the state file on disk.
        /// </summary>
        private string stateFilePath;

        /// <summary>
        /// Dictionary holding the state information for each job, keyed by job name.
        /// </summary>
        private Dictionary<string, JobStateInfo> stateData;

        /// <summary>
        /// Lock object for thread safety.
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// Internal class representing the state information of a backup job.
        /// </summary>
        private class JobStateInfo
        {
            /// <summary>
            /// Name of the backup job.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Type of backup (Complete or Differential).
            /// </summary>
            public BackupType Type { get; set; }

            /// <summary>
            /// Current state of the job (inactive, active, completed, error).
            /// </summary>
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public JobState State { get; set; }

            /// <summary>
            /// Source path of the backup.
            /// </summary>
            public string SourcePath { get; set; }

            /// <summary>
            /// Target path of the backup.
            /// </summary>
            public string TargetPath { get; set; }

            /// <summary>
            /// Total number of files to backup.
            /// </summary>
            public int TotalFiles { get; set; }

            /// <summary>
            /// Total size of files to backup.
            /// </summary>
            public long TotalSize { get; set; }

            /// <summary>
            /// Time taken to transfer files (in ms).
            /// </summary>
            public long TransfertTime { get; set; }

            /// <summary>
            /// Time taken to encrypt files (in ms).
            /// </summary>
            public long EncryptionTime { get; set; }

            /// <summary>
            /// Progression of the backup in percent (0-100).
            /// </summary>
            public int Progression { get; set; } // In percentage (0-100)
        }

        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        /// <param name="path">Path to the state file.</param>
        private StateManager(string path)
        {
            stateObservers = new List<IStateObserver>();
            stateFilePath = path;
            stateData = new Dictionary<string, JobStateInfo>();
            LoadStateFile();
        }

        /// <summary>
        /// Gets the singleton instance of StateManager.
        /// </summary>
        /// <returns>StateManager instance.</returns>
        public static StateManager GetInstance()
        {
            if (instance == null)
            {
                string defaultPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EasySave", "state.json");
                instance = new StateManager(defaultPath);
            }
            return instance;
        }

        /// <summary>
        /// Updates the state of a job based on the action and notifies observers.
        /// </summary>
        /// <param name="action">Action type ("start", "complete", "error", or other).</param>
        /// <param name="name">Job name.</param>
        /// <param name="type">Backup type.</param>
        /// <param name="state">Job state.</param>
        /// <param name="sourcePath">Source path.</param>
        /// <param name="targetPath">Target path.</param>
        /// <param name="totalFiles">Total files.</param>
        /// <param name="totalSize">Total size.</param>
        /// <param name="transfertTime">The time to transfer file.</param>
        /// <param name="encryptionTime">The time to encrypt the file if needed.</param>
        /// <param name="progression">Progression value.</param>
        public void Update(string action, string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, long transfertTime, long encryptionTime, int progression)
        {
            lock (lockObject)
            {
                // Handle job state update based on action
                if (action == "start")
                {
                    InitializeJobState(name, type, state, sourcePath, targetPath, totalFiles, totalSize, progression);
                }
                else if (action == "complete" || action == "error")
                {
                    FinalizeJobState(name, type, state, sourcePath, targetPath, totalFiles, totalSize, progression);
                }
                else if (action == "transfer" || action == "processing")
                {
                    UpdateJobState(name, type, state, sourcePath, targetPath, totalFiles, totalSize, progression);
                }
                else if (action == "pause" || action == "resume")
                {
                    // For pause and resume, preserve the original action
                    JobStateInfo jobState = GetOrCreateJobState(name);
                    jobState.Type = type;
                    jobState.State = state;
                    jobState.SourcePath = sourcePath;
                    jobState.TargetPath = targetPath;

                    // Notify with the original action preserved
                    foreach (var observer in stateObservers)
                    {
                        observer.Update(action, name, type, state, sourcePath, targetPath,
                            totalFiles, totalSize, progression);
                    }

                    SaveStateFile();
                    return; // Return directly to avoid calling NotifyObservers at the end
                }
                else
                {
                    UpdateJobState(name, type, state, sourcePath, targetPath, totalFiles, totalSize, progression);
                }

                NotifyObservers();
            }
        }

        /// <summary>
        /// Updates the state of an existing job.
        /// </summary>
        /// <param name="name">Job name.</param>
        /// <param name="type">Backup type.</param>
        /// <param name="state">Job state.</param>
        /// <param name="sourcePath">Source path.</param>
        /// <param name="targetPath">Target path.</param>
        /// <param name="totalFiles">Total files.</param>
        /// <param name="totalSize">Total size.</param>
        /// <param name="progression">Progression value.</param>
        public void UpdateJobState(string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, int progression)
        {
            JobStateInfo jobState = GetOrCreateJobState(name);

            jobState.Type = type;
            jobState.State = state;
            jobState.SourcePath = sourcePath;
            jobState.TargetPath = targetPath;

            // Ensure TotalFiles is always initialized with the correct value and does not change during execution
            if (jobState.TotalFiles == 0 && totalFiles > 0)
            {
                jobState.TotalFiles = totalFiles;
            }

            jobState.TotalSize = totalSize;
            // Calculate progression as a percentage
            jobState.Progression = (int)Math.Min(100, Math.Round((double)progression / jobState.TotalFiles * 100));

            SaveStateFile();
        }

        /// <summary>
        /// Initializes the state of a job at the start.
        /// </summary>
        /// <param name="name">Job name.</param>
        /// <param name="type">Backup type.</param>
        /// <param name="state">Job state.</param>
        /// <param name="sourcePath">Source path.</param>
        /// <param name="targetPath">Target path.</param>
        /// <param name="totalFiles">Total files.</param>
        /// <param name="totalSize">Total size.</param>
        /// <param name="progression">Progression value.</param>
        public void InitializeJobState(string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, int progression)
        {
            JobStateInfo jobState = GetOrCreateJobState(name);

            jobState.Type = type;
            jobState.State = JobState.active;
            jobState.SourcePath = sourcePath;
            jobState.TargetPath = targetPath;
            jobState.TotalFiles = totalFiles;
            jobState.TotalSize = totalSize;
            jobState.Progression = 0; // Initial progression at 0%

            SaveStateFile();
        }

        /// <summary>
        /// Finalizes the state of a job when completed or errored.
        /// </summary>
        /// <param name="name">Job name.</param>
        /// <param name="type">Backup type.</param>
        /// <param name="state">Job state.</param>
        /// <param name="sourcePath">Source path.</param>
        /// <param name="targetPath">Target path.</param>
        /// <param name="totalFiles">Total files.</param>
        /// <param name="totalSize">Total size.</param>
        /// <param name="progression">Progression value.</param>
        public void FinalizeJobState(string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, int progression)
        {
            JobStateInfo jobState = GetOrCreateJobState(name);

            jobState.Type = type;
            jobState.State = state;
            jobState.SourcePath = sourcePath;
            jobState.TargetPath = targetPath;

            // Do not modify TotalFiles during finalization
            if (jobState.TotalFiles == 0 && totalFiles > 0)
            {
                jobState.TotalFiles = totalFiles;
            }

            jobState.TotalSize = totalSize;

            // For a completed job, no files remain to be processed
            if (state == JobState.completed)
            {
                jobState.Progression = 100;
            }
            else
            {
                // Calculate progression as a percentage
                jobState.Progression = jobState.TotalFiles > 0
                    ? (int)Math.Min(100, Math.Round((double)progression / jobState.TotalFiles * 100))
                    : 0;
            }
            SaveStateFile();
        }

        /// <summary>
        /// Gets the state info for a job, or creates it if it does not exist.
        /// </summary>
        /// <param name="jobName">Job name.</param>
        /// <returns>JobStateInfo instance.</returns>
        private JobStateInfo GetOrCreateJobState(string jobName)
        {
            if (!stateData.ContainsKey(jobName))
            {
                stateData[jobName] = new JobStateInfo { Name = jobName };
            }

            return stateData[jobName];
        }

        /// <summary>
        /// Loads the state file from disk into memory.
        /// </summary>
        private void LoadStateFile()
        {
            try
            {
                if (File.Exists(stateFilePath))
                {
                    string json = File.ReadAllText(stateFilePath);
                    var options = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() }
                    };

                    stateData = JsonSerializer.Deserialize<Dictionary<string, JobStateInfo>>(json, options)
                        ?? new Dictionary<string, JobStateInfo>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading state file: {ex.Message}");
                stateData = new Dictionary<string, JobStateInfo>();
            }
        }

        /// <summary>
        /// Saves the current state data to the state file on disk.
        /// </summary>
        private void SaveStateFile()
        {
            try
            {
                string directory = Path.GetDirectoryName(stateFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                string json = JsonSerializer.Serialize(stateData, options);
                File.WriteAllText(stateFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving state file: {ex.Message}");
            }
        }

        /// <summary>
        /// Notifies all attached observers of the current state of all jobs.
        /// </summary>
        public void NotifyObservers()
        {
            foreach (var jobState in stateData.Values)
            {
                foreach (var observer in stateObservers)
                {
                    observer.Update("update", jobState.Name, jobState.Type, jobState.State,
                        jobState.SourcePath, jobState.TargetPath, jobState.TotalFiles,
                        jobState.TotalSize, jobState.Progression);
                }
            }
        }

        /// <summary>
        /// Attaches an observer to be notified of state changes.
        /// </summary>
        /// <param name="observer">Observer to attach.</param>
        public void AttachObserver(IStateObserver observer)
        {
            if (!stateObservers.Contains(observer))
            {
                stateObservers.Add(observer);
            }
        }
    }
}
