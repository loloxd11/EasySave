using System.IO;

namespace EasySave.Models
{
    /// <summary>
    /// Abstract base class for backup strategies.
    /// Implements observer pattern and provides utility methods for backup operations.
    /// </summary>
    public abstract class AbstractBackupStrategy
    {
        /// <summary>
        /// List of observers to notify about backup job updates.
        /// </summary>
        protected List<IObserver> observers = new List<IObserver>();

        /// <summary>
        /// Current state of the backup job.
        /// </summary>
        protected JobState state;

        /// <summary>
        /// Total number of files to process in the backup.
        /// </summary>
        protected int totalFiles;

        /// <summary>
        /// Total size of all files to process in the backup (in bytes).
        /// </summary>
        protected long totalSize;

        /// <summary>
        /// Current progress value of the backup job.
        /// </summary>
        protected int currentProgress;

        /// <summary>
        /// Timestamp of the last file processed (in ticks).
        /// </summary>
        protected long LastFileTime;

        /// <summary>
        /// Name of the backup job.
        /// </summary>
        protected string name;

        /// <summary>
        /// Static class containing standardized action names for backup notifications.
        /// </summary>
        public static class BackupActions
        {
            public const string Start = "start";
            public const string Processing = "processing";
            public const string Transfer = "transfer";
            public const string Complete = "complete";
            public const string Error = "error";
            public const string Progress = "progress";
        }

        /// <summary>
        /// Attach an observer to receive updates about the backup job.
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
        /// Abstract method to get the list of files to copy for the backup.
        /// </summary>
        /// <param name="sourcePath">Source directory path.</param>
        /// <param name="targetPath">Target directory path.</param>
        /// <returns>List of file paths to copy.</returns>
        public abstract List<string> GetFilesToCopy(string sourcePath, string targetPath);

        /// <summary>
        /// Unified method to notify all observers with different types of backup job updates.
        /// </summary>
        /// <param name="action">Type of action (start, processing, transfer, complete, error, progress).</param>
        /// <param name="name">Name of the backup job.</param>
        /// <param name="state">Current state of the backup job.</param>
        /// <param name="sourcePath">Source path (can be empty for progress updates).</param>
        /// <param name="targetPath">Target path (can be empty for progress updates).</param>
        /// <param name="totalFiles">Total number of files involved in the backup.</param>
        /// <param name="totalSize">Total size of files in bytes.</param>
        /// <param name="transferTime">Time taken for file transfer (for transfers).</param>
        /// <param name="encryptionTime">Time taken for encryption (for transfers).</param>
        /// <param name="currentProgress">Current progress value (optional, used for progress updates).</param>
        public void NotifyObserver(
            string action,
            string name,
            JobState state,
            string sourcePath = "",
            string targetPath = "",
            int totalFiles = 0,
            long totalSize = 0,
            long transferTime = 0,
            long encryptionTime = 0,
            int currentProgress = 0)
        {
            // Determine the type of backup currently running
            BackupType backupType = this is CompleteBackupStrategy ? BackupType.Complete : BackupType.Differential;

            // Notify all observers with the update
            foreach (var observer in observers)
            {
                observer.Update(action, name, backupType, state, sourcePath, targetPath,
                    totalFiles, totalSize, transferTime, encryptionTime, currentProgress);
            }
        }

        /// <summary>
        /// Update the current file being processed and notify observers.
        /// </summary>
        /// <param name="source">Source path of the current file.</param>
        /// <param name="target">Target path of the current file.</param>
        public void UpdateCurrentFile(string source, string target)
        {
            // Update the timestamp for the last file processed
            LastFileTime = DateTime.Now.Ticks;
            // Notify observers about the file being processed
            NotifyObserver(BackupActions.Processing, name, state, source, target);
        }

        /// <summary>
        /// Calculate the total number of files in the source directory (recursively).
        /// </summary>
        /// <param name="source">Source directory path.</param>
        /// <returns>Total number of files.</returns>
        public int CalculateTotalFiles(string source)
        {
            var files = ScanDirectory(source);
            return files.Count;
        }

        /// <summary>
        /// Recursively scan a directory and return a list of all file paths.
        /// </summary>
        /// <param name="path">Directory path to scan.</param>
        /// <returns>List of file paths.</returns>
        protected List<string> ScanDirectory(string path)
        {
            var result = new List<string>();

            try
            {
                // Add files in the current directory
                result.AddRange(Directory.GetFiles(path));

                // Recursively add files from subdirectories
                foreach (var directory in Directory.GetDirectories(path))
                {
                    result.AddRange(ScanDirectory(directory));
                }
            }
            catch (Exception ex)
            {
                // Log exception if directory cannot be accessed
                Console.WriteLine($"Error scanning directory {path}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Get the size of a file in bytes.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns>File size in bytes, or 0 if the file cannot be accessed.</returns>
        protected long GetFileSize(string path)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                return fileInfo.Length;
            }
            catch (Exception)
            {
                // Return 0 if file cannot be accessed
                return 0;
            }
        }

        /// <summary>
        /// Calculate the total size of all files in the source directory (recursively).
        /// </summary>
        /// <param name="source">Source directory path.</param>
        /// <returns>Total size in bytes.</returns>
        public long CalculateTotalSize(string source)
        {
            long size = 0;
            List<string> files = ScanDirectory(source);

            foreach (string file in files)
            {
                size += GetFileSize(file);
            }

            return size;
        }
    }
}
