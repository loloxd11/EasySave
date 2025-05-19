using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Progression percentage of the backup job.
        /// </summary>
        protected int progression;

        /// <summary>
        /// Timestamp of the last file processed (in ticks).
        /// </summary>
        protected long LastFileTime;

        /// <summary>
        /// Name of the backup job.
        /// </summary>
        protected string name;

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
        /// Notify all attached observers about a backup job update.
        /// </summary>
        /// <param name="action">The action performed (e.g., start, stop, update).</param>
        /// <param name="name">The name of the backup job.</param>
        /// <param name="sourcePath">The source path of the backup.</param>
        /// <param name="targetPath">The target path of the backup.</param>
        /// <param name="fileSize">The size of the file being processed.</param>
        /// <param name="transferTime">The time taken to transfer the file.</param>
        /// <param name="encryptionTime">The time taken to encrypt the file.</param>
        public void NotifyObserver(string action, string name, string sourcePath, string targetPath,
            long fileSize, long transferTime, long encryptionTime)
        {
            foreach (var observer in observers)
            {
                observer.Update(action, name, BackupType.Complete, state, sourcePath, targetPath,
                    totalFiles, totalSize, progression);
            }
        }

        /// <summary>
        /// Update the progression of the backup job.
        /// </summary>
        /// <param name="files">Number of files processed.</param>
        /// <param name="size">Total size processed (in bytes).</param>
        public void UpdateProgress(int files, long size)
        {
            progression = files;
            // Update logic
        }

        /// <summary>
        /// Update the current file being processed and notify observers.
        /// </summary>
        /// <param name="source">Source path of the current file.</param>
        /// <param name="target">Target path of the current file.</param>
        public void UpdateCurrentFile(string source, string target)
        {
            // Update current file being processed
            LastFileTime = DateTime.Now.Ticks;
            NotifyObserver("processing", name, source, target, 0, 0, 0);
        }

        /// <summary>
        /// Execute the backup strategy.
        /// </summary>
        /// <param name="name">Name of the backup job.</param>
        /// <param name="src">Source directory path.</param>
        /// <param name="dst">Destination directory path.</param>
        /// <param name="order">Order or mode of execution.</param>
        /// <returns>True if the backup was successful, false otherwise.</returns>
        public abstract bool Execute(string name, string src, string dst, string order);

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
                // Log exception
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
