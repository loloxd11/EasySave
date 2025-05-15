using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace EasySave
{
    /// <summary>
    /// Implements the complete backup strategy, which copies all files from the source directory to the target directory.
    /// </summary>
    public class CompleteBackupStrategy : AbstractBackupStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompleteBackupStrategy"/> class.
        /// </summary>
        /// <param name="stateManager">The state manager to track the backup job's state.</param>
        /// <param name="logManager">The log manager to log backup operations.</param>
        public CompleteBackupStrategy(StateManager stateManager, LogManager logManager) : base(stateManager, logManager)
        {
        }

        /// <summary>
        /// Executes the complete backup process for the given backup job.
        /// </summary>
        /// <param name="job">The backup job to execute.</param>
        /// <returns>True if the backup was successful, otherwise false.</returns>
        public override bool Execute(BackupJob job)
        {
            try
            {
                // Check if the source directory exists
                if (!Directory.Exists(job.SourcePath))
                {
                    throw new DirectoryNotFoundException($"The source directory does not exist: {job.SourcePath}");
                }

                string source = job.SourcePath;
                string target = job.TargetPath;

                // Create the target directory if it does not exist
                if (!Directory.Exists(target))
                {
                    Directory.CreateDirectory(target);
                }

                // Retrieve all files from the source directory and its subdirectories
                List<string> files = ScanDirectory(source);

                int totalFiles = files.Count;
                int remainingFiles = totalFiles;
                long totalSize = 0;
                long remainingSize = 0;

                // Calculate the total size of all files
                foreach (string file in files)
                {
                    totalSize += GetFileSize(file);
                }

                remainingSize = totalSize;

                // Update the job's properties with total files and size
                job.TotalFiles = totalFiles;
                job.TotalSize = totalSize;

                // Process each file
                foreach (string sourceFile in files)
                {
                    // Compute the relative path of the file
                    string relativePath = sourceFile.Substring(source.Length).TrimStart('\\', '/');
                    string targetFile = Path.Combine(target, relativePath);

                    // Create the target directory if it does not exist
                    string targetDirectory = Path.GetDirectoryName(targetFile);
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    // Copy the file and measure the time taken
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    try
                    {
                        File.Copy(sourceFile, targetFile, true);
                        stopwatch.Stop();
                        job.LastFileTime = stopwatch.ElapsedMilliseconds;

                        // Update the current file information and trigger logging
                        job.UpdateCurrentFile(sourceFile, targetFile);
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        job.LastFileTime = -1; // A negative time indicates an error
                        Console.WriteLine($"Error while copying the file {sourceFile}: {ex.Message}");
                    }

                    // Update the progress of the backup
                    long fileSize = GetFileSize(sourceFile);
                    remainingFiles--;
                    remainingSize -= fileSize;
                    job.UpdateProgress(remainingFiles, remainingSize);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during the execution of the complete backup: {ex.Message}");
                return false;
            }
        }
    }
}
