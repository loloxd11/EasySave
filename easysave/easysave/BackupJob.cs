using System;
using System.Collections.Generic;
using System.IO;

namespace EasySave
{
    public class BackupJob
    {
        private string name; // The name of the backup job
        private string sourcePath; // The source directory path for the backup
        private string targetPath; // The target directory path for the backup
        private BackupType type; // The type of backup (Complete or Differential)
        private List<IObserver> observers; // List of observers attached to the job
        private JobState state; // The current state of the backup job
        private int totalFiles; // Total number of files to be backed up
        private long totalSize; // Total size of files to be backed up
        private int progression; // Progression percentage of the backup job
        private AbstractBackupStrategy backupStrategy; // The strategy used to execute the backup
        private long lastFileTime; // Timestamp of the last file processed

        // Properties to expose private fields
        public string Name { get => name; } // Gets the name of the backup job
        public string SourcePath { get => sourcePath; } // Gets the source directory path
        public string TargetPath { get => targetPath; } // Gets the target directory path
        public BackupType Type { get => type; } // Gets the type of backup
        public JobState State { get => state; set => state = value; } // Gets or sets the current state of the job
        public int TotalFiles { get => totalFiles; set => totalFiles = value; } // Gets or sets the total number of files
        public long TotalSize { get => totalSize; set => totalSize = value; } // Gets or sets the total size of files
        public int Progression { get => progression; set => progression = value; } // Gets or sets the progression percentage
        public long LastFileTime { get => lastFileTime; set => lastFileTime = value; } // Gets or sets the timestamp of the last file processed
        public string CurrentSourceFile { get; private set; } // Gets the current source file being processed
        public string CurrentTargetFile { get; private set; } // Gets the current target file being processed
        public int RemainingFiles { get; private set; } // Gets the number of remaining files to process
        public long RemainingSize { get; private set; } // Gets the remaining size of files to process
        public IEnumerable<IObserver> Observers => observers; // Gets the list of observers

        // Constructor to initialize the backup job
        public BackupJob(string name, string source, string target, BackupType type, AbstractBackupStrategy strategy)
        {
            this.name = name;
            this.sourcePath = source;
            this.targetPath = target;
            this.type = type;
            this.backupStrategy = strategy;
            this.observers = new List<IObserver>();
            this.state = JobState.Inactive; // Default state is inactive
            this.totalFiles = 0;
            this.totalSize = 0;
            this.progression = 0;
            this.lastFileTime = 0;
            this.RemainingFiles = 0;
            this.RemainingSize = 0;
        }

        // Executes the backup job
        public bool Execute()
        {
            try
            {
                // Update the state to active
                state = JobState.Active;

                // Calculate the total number of files and their size
                totalFiles = backupStrategy.CalculateTotalFiles(sourcePath);
                totalSize = backupStrategy.CalculateTotalSize(sourcePath);
                RemainingFiles = totalFiles;
                RemainingSize = totalSize;

                // Notify observers that the job has started
                NotifyObservers("start");

                // Execute the backup strategy
                bool result = backupStrategy.Execute(this);

                // Update the state based on the result
                state = result ? JobState.Completed : JobState.Error;

                // Notify observers that the job has finished
                NotifyObservers("finish");

                return result;
            }
            catch (Exception ex)
            {
                // Handle errors and update the state to error
                state = JobState.Error;
                Console.WriteLine($"Error while executing the backup job {name}: {ex.Message}");
                NotifyObservers("error");
                return false;
            }
        }

        // Attach an observer to the job
        public void AttachObserver(IObserver observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
        }

        // Notify all observers about a specific action
        public void NotifyObservers(string action)
        {
            foreach (var observer in observers)
            {
                observer.Update(this, action);
            }
        }

        // Update the progress of the backup job
        public void UpdateProgress(int files, long size)
        {
            int filesProcessed = totalFiles - files; // Calculate the number of files processed
            progression = (totalFiles > 0) ? (int)((double)filesProcessed / totalFiles * 100) : 0; // Calculate progression percentage
            RemainingFiles = files; // Update remaining files
            RemainingSize = size; // Update remaining size

            // Notify observers about the progress update
            NotifyObservers("progress");
        }

        // Update the current file being processed
        public void UpdateCurrentFile(string source, string target)
        {
            CurrentSourceFile = source; // Update the current source file
            CurrentTargetFile = target; // Update the current target file

            // Notify observers about the file update
            NotifyObservers("file");
        }
    }
}
