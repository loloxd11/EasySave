using System;
using System.Collections.Generic;
using System.IO;

namespace EasySave
{
    /// <summary>
    /// Represents a backup job that manages the process of copying files from a source directory to a target directory.
    /// </summary>
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

        /// <summary>
        /// Gets the name of the backup job.
        /// </summary>
        public string Name { get => name; }

        /// <summary>
        /// Gets the source directory path for the backup.
        /// </summary>
        public string SourcePath { get => sourcePath; }

        /// <summary>
        /// Gets the target directory path for the backup.
        /// </summary>
        public string TargetPath { get => targetPath; }

        /// <summary>
        /// Gets the type of backup (Complete or Differential).
        /// </summary>
        public BackupType Type { get => type; }

        /// <summary>
        /// Gets or sets the current state of the backup job.
        /// </summary>
        public JobState State { get => state; set => state = value; }

        /// <summary>
        /// Gets or sets the total number of files to be backed up.
        /// </summary>
        public int TotalFiles { get => totalFiles; set => totalFiles = value; }

        /// <summary>
        /// Gets or sets the total size of files to be backed up.
        /// </summary>
        public long TotalSize { get => totalSize; set => totalSize = value; }

        /// <summary>
        /// Gets or sets the progression percentage of the backup job.
        /// </summary>
        public int Progression { get => progression; set => progression = value; }

        /// <summary>
        /// Gets or sets the timestamp of the last file processed.
        /// </summary>
        public long LastFileTime { get => lastFileTime; set => lastFileTime = value; }

        /// <summary>
        /// Gets the current source file being processed.
        /// </summary>
        public string CurrentSourceFile { get; private set; }

        /// <summary>
        /// Gets the current target file being processed.
        /// </summary>
        public string CurrentTargetFile { get; private set; }

        /// <summary>
        /// Gets the number of remaining files to process.
        /// </summary>
        public int RemainingFiles { get; private set; }

        /// <summary>
        /// Gets the remaining size of files to process.
        /// </summary>
        public long RemainingSize { get; private set; }

        /// <summary>
        /// Gets the list of observers attached to the backup job.
        /// </summary>
        public IEnumerable<IObserver> Observers => observers;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupJob"/> class.
        /// </summary>
        /// <param name="name">The name of the backup job.</param>
        /// <param name="source">The source directory path.</param>
        /// <param name="target">The target directory path.</param>
        /// <param name="type">The type of backup (Complete or Differential).</param>
        /// <param name="strategy">The backup strategy to use.</param>
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

        /// <summary>
        /// Executes the backup job using the specified backup strategy.
        /// </summary>
        /// <returns>True if the backup was successful, otherwise false.</returns>
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

        /// <summary>
        /// Attaches an observer to the backup job.
        /// </summary>
        /// <param name="observer">The observer to attach.</param>
        public void AttachObserver(IObserver observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
        }

        /// <summary>
        /// Notifies all observers about a specific action performed on the backup job.
        /// </summary>
        /// <param name="action">The action to notify observers about.</param>
        public void NotifyObservers(string action)
        {
            foreach (var observer in observers)
            {
                observer.Update(this, action);
            }
        }

        /// <summary>
        /// Updates the progress of the backup job.
        /// </summary>
        /// <param name="files">The number of remaining files to process.</param>
        /// <param name="size">The remaining size of files to process.</param>
        public void UpdateProgress(int files, long size)
        {
            int filesProcessed = totalFiles - files; // Calculate the number of files processed
            progression = (totalFiles > 0) ? (int)((double)filesProcessed / totalFiles * 100) : 0; // Calculate progression percentage
            RemainingFiles = files; // Update remaining files
            RemainingSize = size; // Update remaining size

            // Notify observers about the progress update
            NotifyObservers("progress");
        }

        /// <summary>
        /// Updates the current file being processed by the backup job.
        /// </summary>
        /// <param name="source">The current source file path.</param>
        /// <param name="target">The current target file path.</param>
        public void UpdateCurrentFile(string source, string target)
        {
            CurrentSourceFile = source; // Update the current source file
            CurrentTargetFile = target; // Update the current target file

            // Notify observers about the file update
            NotifyObservers("file");
        }
    }
}
