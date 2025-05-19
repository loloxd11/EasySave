using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EasySave.Views;

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
        /// <param name="progression">Progression value.</param>
        public void Update(string action, string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, int progression)
        {
            lock (lockObject)
            {
                Console.WriteLine($"Received update: action={action}, name={name}, progression={progression}");

                if (action == "start")
                {
                    InitializeJobState(name, type, state, sourcePath, targetPath, totalFiles, totalSize, progression);
                }
                else if (action == "complete" || action == "error")
                {
                    FinalizeJobState(name, type, state, sourcePath, targetPath, totalFiles, totalSize, progression);
                }
                else if (action == "transfer" || action == "processing")  // Assurez-vous que "transfer" est bien traité
                {
                    UpdateJobState(name, type, state, sourcePath, targetPath, totalFiles, totalSize, progression);
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

            // Assurez-vous que TotalFiles soit toujours initialisé avec la valeur correcte
            // et ne change pas pendant l'exécution
            if (jobState.TotalFiles == 0 && totalFiles > 0)
            {
                jobState.TotalFiles = totalFiles;
            }

            jobState.TotalSize = totalSize;
            // Calculer la progression en pourcentage
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

            // Ne pas modifier TotalFiles lors de la finalisation
            if (jobState.TotalFiles == 0 && totalFiles > 0)
            {
                jobState.TotalFiles = totalFiles;
            }

            jobState.TotalSize = totalSize;

            // Pour un job complété, aucun fichier ne reste à traiter
            if (state == JobState.completed)
            {
                jobState.Progression = 100;
            }
            else
            {

                // Calculer la progression en pourcentage
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
