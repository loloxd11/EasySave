using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using Octokit;
using Microsoft.Extensions.DependencyModel;

namespace EasySave
{
    public class DifferentialBackupStrategy : AbstractBackupStrategy
    {
        public DifferentialBackupStrategy(StateManager stateManager, LogManager logManager)
            : base(stateManager, logManager)
        {
        }

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

        private bool CompareFiles(string sourcePath, string targetPath)
        {
            // Compare the hash of the files
            string sourceHash = ComputeFileHash(sourcePath);
            string targetHash = ComputeFileHash(targetPath);

            return sourceHash == targetHash;
        }

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
