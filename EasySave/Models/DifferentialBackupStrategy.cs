using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasySave.Models
{
    public class DifferentialBackupStrategy : AbstractBackupStrategy
    {
        private string currentFile;
        private string destinationFile;
        private int totalFiles;
        private int remainFiles;
        private BackupType backupType = BackupType.Differential;

        public override bool Execute(string name, string sourcePath, string targetPath, string order)
        {
            this.name = name;
            state = JobState.active;
            EncryptionService encryptionService = EncryptionService.GetInstance();

            try
            {
                // Obtenir tous les fichiers de la source
                List<string> sourceFiles = ScanDirectory(sourcePath);
                
                // Calculer d'abord les fichiers qui devront être copiés
                List<string> filesToCopy = new List<string>();
                
                foreach (string sourceFile in sourceFiles)
                {
                    string relativePath = sourceFile.Substring(sourcePath.Length);
                    if (relativePath.StartsWith("\\") || relativePath.StartsWith("/"))
                    {
                        relativePath = relativePath.Substring(1);
                    }
                    string destFile = Path.Combine(targetPath, relativePath);
                    
                    // Vérifier si ce fichier doit être copié
                    bool shouldCopy = !File.Exists(destFile) || 
                                     File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(destFile);
                    
                    if (shouldCopy)
                    {
                        filesToCopy.Add(sourceFile);
                    }
                }
                
                // Calculate total files and size (seulement pour les fichiers à copier)
                totalFiles = sourceFiles.Count; // Pour la progression totale, on garde tous les fichiers
                totalSize = CalculateTotalSize(sourcePath);
                remainFiles = totalFiles;
                currentProgress = 0;

                // Notify observers
                NotifyObserver(BackupActions.Start, name, state, sourcePath, targetPath, totalFiles, totalSize, 0, 0, 0);

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

                        // Update current file
                        currentFile = sourceFile;
                        destinationFile = destFile;

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
           

                    // Update progress (une seule fois par fichier)
                    remainFiles--;
                    currentProgress = totalFiles - remainFiles;
                    
                     // Notify observers
                    NotifyObserver(BackupActions.Processing, name, state, sourceFile, destFile, totalFiles, totalSize, transferTime, 0, currentProgress);
                }

                // Mark job as completed
                state = JobState.completed;
                NotifyObserver(BackupActions.Complete, name, state, sourcePath, targetPath, totalFiles, totalSize, 0, 0, totalFiles);
                return true;
            }
            catch (Exception ex)
            {
                // Handle errors
                state = JobState.error;
                NotifyObserver(BackupActions.Error, name, state, sourcePath, targetPath, totalFiles, totalSize, 0, 0, currentProgress);
                Console.WriteLine($"Error executing differential backup: {ex.Message}");
                return false;
            }
        }
    }
}

