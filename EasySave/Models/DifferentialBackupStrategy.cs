using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public class DifferentialBackupStrategy : AbstractBackupStrategy
    {
        private string currentFile;
        private string destinationFile;
        private int totalFiles;
        private int remainFiles;

        public override bool Execute(string name, string sourcePath, string targetPath, string order)
        {
            this.name = name;
            state = JobState.active;

            try
            {
                // Calculate total files and size
                totalFiles = CalculateTotalFiles(sourcePath);
                totalSize = CalculateTotalSize(sourcePath);
                remainFiles = totalFiles;
                progression = 0;

                // Notify observers
                NotifyObserver("start", name, sourcePath, targetPath, totalSize, 0, 0);

                // Get all files from source directory
                List<string> files = ScanDirectory(sourcePath);

                // Create target directory if it doesn't exist
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                // Copy each file only if it's new or modified
                foreach (string sourceFile in files)
                {
                    // Create relative path
                    string relativePath = sourceFile.Substring(sourcePath.Length);
                    if (relativePath.StartsWith("\\") || relativePath.StartsWith("/"))
                    {
                        relativePath = relativePath.Substring(1);
                    }

                    // Create destination file path
                    string destFile = Path.Combine(targetPath, relativePath);
                    string destDir = Path.GetDirectoryName(destFile);

                    // Only copy if the file doesn't exist or is newer
                    bool shouldCopy = !File.Exists(destFile) ||
                                     File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(destFile);

                    if (shouldCopy)
                    {
                        // Create destination directory if it doesn't exist
                        if (!Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }

                        // Update current file
                        currentFile = sourceFile;
                        destinationFile = destFile;
                        UpdateCurrentFile(sourceFile, destFile);

                        // Copy file
                        long fileSize = GetFileSize(sourceFile);
                        long startTime = DateTime.Now.Ticks;

                        File.Copy(sourceFile, destFile, true);

                        long endTime = DateTime.Now.Ticks;
                        long transferTime = endTime - startTime;

                        remainFiles--;
                        progression = totalFiles - remainFiles;

                        // Notify observers
                        NotifyObserver("transfer", name, sourceFile, destFile, fileSize, transferTime, 0, progression);
                    }

                    // Update progress
                    remainFiles--;
                    progression = totalFiles - remainFiles;
                }

                state = JobState.completed;
                NotifyObserver("complete", name, sourcePath, targetPath, totalSize, 0, 0);
                return true;
            }
            catch (Exception ex)
            {
                state = JobState.error;
                NotifyObserver("error", name, sourcePath, targetPath, 0, 0, 0);
                Console.WriteLine($"Error executing differential backup: {ex.Message}");
                return false;
            }
        }
    }
}
