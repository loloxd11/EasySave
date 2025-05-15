using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using Octokit;
using Microsoft.Extensions.DependencyModel;

namespace EasySave
{
    /// <summary>
    /// Implements a differential backup strategy, which copies only files that have changed since the last backup.
    /// </summary>
    public class DifferentialBackupStrategy : AbstractBackupStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentialBackupStrategy"/> class.
        /// </summary>
        /// <param name="stateManager">The state manager to track job states.</param>
        /// <param name="logManager">The log manager to handle logging operations.</param>
        public DifferentialBackupStrategy(StateManager stateManager, LogManager logManager)
            : base(stateManager, logManager)
        {
        }

        /// <summary>
        /// Executes the differential backup for the given job.
        /// </summary>
        /// <param name="job">The backup job to execute.</param>
        /// <returns>True if the backup was successful, otherwise false.</returns>
        public override bool Execute(BackupJob job)
        {
            try
            {
                // Define 'source' and 'target' variables from the job
                string source = job.SourcePath;
                string target = job.TargetPath;

                // Ensure the source directory exists
                if (!Directory.Exists(source))
                {
                    throw new DirectoryNotFoundException($"The source directory does not exist: {source}");
                }

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

                // Update the job with total files and size
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

                    // Check if the file needs to be copied (if it does not exist or has been modified)
                    bool needsCopy = !File.Exists(targetFile) || !CompareFiles(sourceFile, targetFile);

                    if (needsCopy)
                    {
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
                    }
                    else
                    {
                        // If the file does not need to be copied, do not log it
                        job.LastFileTime = 0;
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
                Console.WriteLine($"Error during the execution of the differential backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Compares two files by computing their SHA256 hash values.
        /// </summary>
        /// <param name="sourcePath">The path of the source file.</param>
        /// <param name="targetPath">The path of the target file.</param>
        /// <returns>True if the files are identical, otherwise false.</returns>
        private bool CompareFiles(string sourcePath, string targetPath)
        {
            // Compare the hash of the files
            string sourceHash = ComputeFileHash(sourcePath);
            string targetHash = ComputeFileHash(targetPath);

            return sourceHash == targetHash;
        }

        /// <summary>
        /// Computes the SHA256 hash of a file.
        /// </summary>
        /// <param name="filePath">The path of the file to hash.</param>
        /// <returns>The hash value as a hexadecimal string.</returns>
        private string ComputeFileHash(string filePath)
        {
            // Compute the SHA256 hash of a file
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Determines if two files are modified by comparing their hash values.
        /// </summary>
        /// <param name="sourceFilePath">The path of the source file.</param>
        /// <param name="targetFilePath">The path of the target file.</param>
        /// <returns>True if the files are modified, otherwise false.</returns>
        private bool AreFilesModified(string sourceFilePath, string targetFilePath)
        {
            // Check if the files exist
            if (!File.Exists(sourceFilePath) || !File.Exists(targetFilePath))
            {
                return true; // Consider as modified if one of the files does not exist
            }

            // Compare the hash of the files
            string sourceHash = ComputeFileHash(sourceFilePath);
            string targetHash = ComputeFileHash(targetFilePath);

            return !sourceHash.Equals(targetHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
