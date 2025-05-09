using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave
{
    public class StateManager : IObserver
    {
        private string stateFilePath;
        private Dictionary<string, JobStateInfo> stateData;

        public StateManager(string path)
        {
            stateFilePath = path;
            LoadStateFile();
        }

        public void Update(BackupJob job, string action)
        {
            // Update the state for the given job based on the action
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

        public void UpdateJobState(BackupJob job)
        {
            // Retrieve or create the state for the given job
            JobStateInfo jobState = GetOrCreateJobState(job.Name);

            // Update the job state with the current job details
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

            // Save the updated state to the file
            SaveStateFile();
        }

        public void InitializeJobState(BackupJob job)
        {
            // Initialize a new state for the given job
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

            // Save the initialized state to the file
            SaveStateFile();
        }

        public void FinalizeJobState(BackupJob job)
        {
            // Finalize the state for the given job
            JobStateInfo jobState = GetOrCreateJobState(job.Name);

            // Update the job state with final details
            jobState.State = job.State.ToString();
            jobState.LastUpdated = DateTime.Now;
            jobState.TotalFiles = job.TotalFiles;
            jobState.TotalSize = job.TotalSize;
            jobState.Progression = (job.State == JobState.Completed) ? 100 : job.Progression;
            jobState.FilesRemaining = 0;
            jobState.SizeRemaining = 0;

            // Clear the current file details
            jobState.CurrentSourceFile = null;
            jobState.CurrentTargetFile = null;

            // Save the finalized state to the file
            SaveStateFile();
        }

        private JobStateInfo GetOrCreateJobState(string jobName)
        {
            // Retrieve the state for the given job or create a new one if it doesn't exist
            if (!stateData.ContainsKey(jobName))
            {
                stateData[jobName] = new JobStateInfo { Name = jobName };
            }

            return stateData[jobName];
        }

        private void LoadStateFile()
        {
            try
            {
                // Load the state data from the file if it exists
                if (File.Exists(stateFilePath))
                {
                    string json = File.ReadAllText(stateFilePath);
                    stateData = JsonSerializer.Deserialize<Dictionary<string, JobStateInfo>>(json);
                }
                else
                {
                    // Initialize an empty state if the file doesn't exist
                    stateData = new Dictionary<string, JobStateInfo>();
                }
            }
            catch (Exception)
            {
                // Handle any errors by initializing an empty state
                stateData = new Dictionary<string, JobStateInfo>();
            }
        }

        private void SaveStateFile()
        {
            try
            {
                // Ensure the directory for the state file exists
                string directory = Path.GetDirectoryName(stateFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize the state data to JSON and save it to the file
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true // Format the JSON with indentation for readability
                };

                string json = JsonSerializer.Serialize(stateData, options);
                File.WriteAllText(stateFilePath, json);
            }
            catch (Exception ex)
            {
            }
        }

        // Internal class to represent the state information of a backup job
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
