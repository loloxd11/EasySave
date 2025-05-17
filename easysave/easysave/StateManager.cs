using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave
{
    /// <summary>
    /// Manages the state of backup jobs by observing their progress and saving their state to a file.
    /// </summary>
    public class StateManager : IObserver
    {
        private string stateFilePath; // Path to the state file where job states are saved
        private Dictionary<string, JobStateInfo> stateData; // Dictionary to store the state of each backup job

        /// <summary>
        /// Initializes a new instance of the <see cref="StateManager"/> class.
        /// </summary>
        /// <param name="path">The file path where the state data will be saved.</param>
        public StateManager(string path)
        {
            stateFilePath = path;
            LoadStateFile();
        }

        /// <summary>
        /// Updates the state of a backup job based on the specified action.
        /// </summary>
        /// <param name="job">The backup job whose state needs to be updated.</param>
        /// <param name="action">The action performed on the job (e.g., "create", "end").</param>
        public void Update(BackupJob job, string action)
        {
            if (action == "create")
            {
                InitializeJobState(job);
            }
            else if (action == "end")
            {
                FinalizeJobState(job);
            }
            else
            {
                UpdateJobState(job);
            }
        }

        /// <summary>
        /// Updates the state of an existing backup job with its current details.
        /// </summary>
        /// <param name="job">The backup job to update.</param>
        public void UpdateJobState(BackupJob job)
        {
            JobStateInfo jobState = GetOrCreateJobState(job.Name);

            // Update job state details
            jobState.Name = job.Name;
            jobState.State = job.State.ToString();
            jobState.LastUpdated = DateTime.Now;
            jobState.TotalFiles = job.TotalFiles;
            jobState.TotalSize = job.TotalSize;
            jobState.FilesRemaining = job.RemainingFiles;
            jobState.SizeRemaining = job.RemainingSize;
            jobState.Progression = job.Progression;
            jobState.CurrentSourceFile = job.CurrentSourceFile;
            jobState.CurrentTargetFile = job.CurrentTargetFile;

            SaveStateFile();
        }

        /// <summary>
        /// Initializes a new state for a backup job with default values.
        /// </summary>
        /// <param name="job">The backup job to initialize.</param>
        public void InitializeJobState(BackupJob job)
        {
            JobStateInfo jobState = GetOrCreateJobState(job.Name);

            // Set default values for the job state
            jobState.Name = job.Name;
            jobState.State = JobState.Inactive.ToString();
            jobState.LastUpdated = DateTime.Now;
            jobState.TotalFiles = 0;
            jobState.TotalSize = 0;
            jobState.FilesRemaining = 0;
            jobState.SizeRemaining = 0;
            jobState.Progression = 0;
            jobState.CurrentSourceFile = string.Empty;
            jobState.CurrentTargetFile = string.Empty;

            SaveStateFile();
        }

        /// <summary>
        /// Finalizes the state of a backup job, marking it as completed or updating its final details.
        /// </summary>
        /// <param name="job">The backup job to finalize.</param>
        public void FinalizeJobState(BackupJob job)
        {
            JobStateInfo jobState = GetOrCreateJobState(job.Name);

            // Update final job state details
            jobState.State = job.State.ToString();
            jobState.LastUpdated = DateTime.Now;
            jobState.TotalFiles = job.TotalFiles;
            jobState.TotalSize = job.TotalSize;
            jobState.Progression = (job.State == JobState.Completed) ? 100 : job.Progression;
            jobState.FilesRemaining = 0;
            jobState.SizeRemaining = 0;
            jobState.CurrentSourceFile = null;
            jobState.CurrentTargetFile = null;

            SaveStateFile();
        }

        /// <summary>
        /// Retrieves the state of a backup job or creates a new state if it doesn't exist.
        /// </summary>
        /// <param name="jobName">The name of the backup job.</param>
        /// <returns>The state information of the backup job.</returns>
        private JobStateInfo GetOrCreateJobState(string jobName)
        {
            if (!stateData.ContainsKey(jobName))
            {
                stateData[jobName] = new JobStateInfo { Name = jobName };
            }

            return stateData[jobName];
        }

        /// <summary>
        /// Loads the state data from the state file, or initializes an empty state if the file doesn't exist.
        /// </summary>
        private void LoadStateFile()
        {
            try
            {
                if (File.Exists(stateFilePath))
                {
                    string json = File.ReadAllText(stateFilePath);
                    stateData = JsonSerializer.Deserialize<Dictionary<string, JobStateInfo>>(json);
                }
                else
                {
                    stateData = new Dictionary<string, JobStateInfo>();
                }
            }
            catch (Exception)
            {
                stateData = new Dictionary<string, JobStateInfo>();
            }
        }

        /// <summary>
        /// Saves the current state data to the state file in JSON format.
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
                    WriteIndented = true // Format JSON for readability
                };

                string json = JsonSerializer.Serialize(stateData, options);
                File.WriteAllText(stateFilePath, json);
            }
            catch (Exception ex)
            {
                // Handle exceptions silently
            }
        }

        /// <summary>
        /// Represents the state information of a backup job.
        /// </summary>
        private class JobStateInfo
        {
            public string Name { get; set; } // The name of the job
            public string State { get; set; } // The current state of the job
            public DateTime LastUpdated { get; set; } // The last time the state was updated
            public int TotalFiles { get; set; } // Total number of files in the job
            public long TotalSize { get; set; } // Total size of files in the job
            public int FilesRemaining { get; set; } // Number of files remaining to process
            public long SizeRemaining { get; set; } // Size of files remaining to process
            public int Progression { get; set; } // Progression percentage of the job
            public string CurrentSourceFile { get; set; } // The current source file being processed
            public string CurrentTargetFile { get; set; } // The current target file being processed
        }
    }
}
