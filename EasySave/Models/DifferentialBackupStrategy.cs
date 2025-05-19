using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasySave.Models
{
    public class DifferentialBackupStrategy : AbstractBackupStrategy
    {
        private int remainingFiles;

        public override bool Execute(string name, string sourcePath, string targetPath, string order)
        {
            this.name = name;
            state = JobState.active;
            EncryptionService encryptionService = EncryptionService.GetInstance();

            try
            {
                // Calculate total files and size first to update UI
                List<string> allSourceFiles = ScanDirectory(sourcePath);
                totalFiles = allSourceFiles.Count;  // Total number of files in source
                totalSize = CalculateTotalSize(sourcePath);  // Total size of source

                NotifyObserver("start", name, sourcePath, targetPath, totalSize, 0, 0);

                // Create target directory if it doesn't exist
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                // Identify files that need to be copied
                List<string> filesToCopy = new List<string>();
                int filesProcessed = 0;

                foreach (string sourceFile in allSourceFiles)
                {
                    // Create relative path
                    string relativePath = sourceFile.Substring(sourcePath.Length).TrimStart('\\', '/');
                    string destinationFile = Path.Combine(targetPath, relativePath);

                    // If file doesn't exist in destination or has been modified, copy it
                    bool needsCopy = !File.Exists(destinationFile) ||
                                    File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(destinationFile);

                    if (needsCopy)
                    {
                        filesToCopy.Add(sourceFile);
                    }

                    // Update UI progress
                    filesProcessed++;
                    progression = filesProcessed;
                }

                // Set counter for remaining files to copy
                remainingFiles = filesToCopy.Count;

                // Copy and process each needed file
                foreach (string sourceFile in filesToCopy)
                {
                    // Create directory structure
                    string relativePath = sourceFile.Substring(sourcePath.Length).TrimStart('\\', '/');
                    string destinationFile = Path.Combine(targetPath, relativePath);
                    string destinationDir = Path.GetDirectoryName(destinationFile);

                    if (!Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    // Update current file being processed
                    UpdateCurrentFile(sourceFile, destinationFile);

                    // Copy file and measure time
                    long fileSize = GetFileSize(sourceFile);
                    long startTime = DateTime.Now.Ticks;

                    File.Copy(sourceFile, destinationFile, true);

                    long endTime = DateTime.Now.Ticks;
                    long transferTime = endTime - startTime;

                    // Vérifier si le fichier doit être chiffré
                    long encryptionTime = 0;
                    if (encryptionService.ShouldEncryptFile(sourceFile))
                    {
                        // Chiffrer le fichier copié
                        encryptionTime = encryptionService.EncryptFile(destinationFile);
                    }

                    // Update progress
                    remainingFiles--;

                    // Notify observers
                    NotifyObserver("transfer", name, sourceFile, destinationFile, fileSize, transferTime, encryptionTime);
                }

                // Mark job as completed
                state = JobState.completed;
                NotifyObserver("complete", name, sourcePath, targetPath, totalSize, 0, 0);
                return true;
            }
            catch (Exception ex)
            {
                // Handle errors
                state = JobState.error;
                NotifyObserver("error", name, sourcePath, targetPath, 0, 0, 0);
                Console.WriteLine($"Error in differential backup: {ex.Message}");
                return false;
            }
        }
    }
}

