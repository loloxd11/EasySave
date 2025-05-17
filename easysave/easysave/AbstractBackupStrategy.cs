using System;
using System.Collections.Generic;
using System.IO;

namespace EasySave
{
    /// <summary>
    /// Abstract class representing a backup strategy.
    /// Provides common methods for calculating file counts, sizes, and scanning directories.
    /// </summary>
    public abstract class AbstractBackupStrategy
    {
        /// <summary>
        /// Manages the state of backup jobs.
        /// </summary>
        protected StateManager stateManager;

        /// <summary>
        /// Manages logging of backup operations.
        /// </summary>
        protected LogManager logManager;

        /// <summary>
        /// Constructor to initialize the backup strategy with state and log managers.
        /// </summary>
        /// <param name="stateManager">The state manager instance.</param>
        /// <param name="logManager">The log manager instance.</param>
        public AbstractBackupStrategy(StateManager stateManager, LogManager logManager)
        {
            this.stateManager = stateManager;
            this.logManager = logManager;
        }

        /// <summary>
        /// Executes the backup job using the specific strategy.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <param name="job">The backup job to execute.</param>
        /// <returns>True if the backup was successful, otherwise false.</returns>
        public abstract bool Execute(BackupJob job);

        /// <summary>
        /// Calculates the total number of files in a directory, including subdirectories.
        /// </summary>
        /// <param name="source">The source directory path.</param>
        /// <returns>The total number of files.</returns>
        public int CalculateTotalFiles(string source)
        {
            if (!Directory.Exists(source))
            {
                return 0;
            }

            int count = 0;

            try
            {
                // Get all files in the directory
                string[] files = Directory.GetFiles(source);
                count += files.Length;

                // Recursively count files in subdirectories
                string[] subdirectories = Directory.GetDirectories(source);
                foreach (string subdirectory in subdirectories)
                {
                    count += CalculateTotalFiles(subdirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while calculating total number of files: {ex.Message}");
            }

            return count;
        }

        /// <summary>
        /// Scans a directory and retrieves a list of all files, including those in subdirectories.
        /// </summary>
        /// <param name="path">The directory path to scan.</param>
        /// <returns>A list of file paths.</returns>
        protected List<string> ScanDirectory(string path)
        {
            List<string> fileList = new List<string>();

            try
            {
                // Add all files in this directory
                fileList.AddRange(Directory.GetFiles(path));

                // Recursively scan subdirectories
                foreach (string subdirectory in Directory.GetDirectories(path))
                {
                    fileList.AddRange(ScanDirectory(subdirectory));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while scanning directory {path}: {ex.Message}");
            }

            return fileList;
        }

        /// <summary>
        /// Retrieves the size of a file in bytes.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The size of the file in bytes, or 0 if an error occurs.</returns>
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
        /// Calculates the total size of all files in a directory, including subdirectories.
        /// </summary>
        /// <param name="source">The source directory path.</param>
        /// <returns>The total size of files in bytes.</returns>
        public long CalculateTotalSize(string source)
        {
            if (!Directory.Exists(source))
            {
                return 0;
            }

            long size = 0;

            try
            {
                // Calculate size of all files in the directory
                foreach (string file in Directory.GetFiles(source))
                {
                    size += GetFileSize(file);
                }

                // Recursively calculate size in subdirectories
                foreach (string subdirectory in Directory.GetDirectories(source))
                {
                    size += CalculateTotalSize(subdirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while calculating total size: {ex.Message}");
            }

            return size;
        }
    }
}
