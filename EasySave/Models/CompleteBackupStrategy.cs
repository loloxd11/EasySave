using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public class CompleteBackupStrategy : AbstractBackupStrategy
    {
        private string currentFile;
        private string destinationFile;
        private int totalFiles;
        private int remainFiles;
        private BackupType backupType = BackupType.Complete;

        public override bool Execute(string name, string sourcePath, string targetPath, string order)
        {
            this.name = name;
            state = JobState.active;
            EncryptionService encryptionService = EncryptionService.GetInstance();

            try
            {
                // Calculate total files and size
                totalFiles = CalculateTotalFiles(sourcePath);
                totalSize = CalculateTotalSize(sourcePath);
                remainFiles = totalFiles;
                currentProgress = 0;

                // Notify observers
                NotifyObserver("start", name, state, sourcePath, targetPath, totalFiles, totalSize,0, 0, 0);

                // Get all files from source directory
                List<string> files = ScanDirectory(sourcePath);

                // Create target directory if it doesn't exist
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                // Copy each file
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

                    // Create destination directory if it doesn't exist
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    // Update current file
                    currentFile = sourceFile;
                    destinationFile = destFile;

                    // Copy file
                    long fileSize = GetFileSize(sourceFile);
                    long startTime = DateTime.Now.Ticks;

                    File.Copy(sourceFile, destFile, true);

                    long endTime = DateTime.Now.Ticks;
                    long transferTime = endTime - startTime;

                    // Vérifier si le fichier doit être chiffré
                    long encryptionTime = 0;
                    if (encryptionService.ShouldEncryptFile(sourceFile))
                    {
                        // Chiffrer le fichier copié
                        encryptionTime = encryptionService.EncryptFile(destFile);
                    }

                    // Update progress
                    remainFiles--;
                    currentProgress = totalFiles - remainFiles;

                    // Notifier les observateurs après chaque fichier copié pour mettre à jour la progression en temps réel
                    NotifyObserver(BackupActions.Processing, name, state, sourceFile, destinationFile, totalFiles, totalSize, transferTime, 0, currentProgress);
                    if (ConfigManager.PriorityProcessIsRunning() == true)
                    {
                        // Si le processus prioritaire est en cours d'exécution, attendre 1 seconde avant de continuer
                        throw new InvalidOperationException("Jobs Canceled, Priority Process is running");
                    }
                }

                state = JobState.completed;
                NotifyObserver("complete", name, state, sourcePath, targetPath, totalFiles, totalSize, 0, 0, 0);
                return true;
            }
            catch (Exception ex)
            {
                state = JobState.error;
                NotifyObserver("error", name, state, sourcePath, targetPath, totalFiles, totalSize, 0, 0, 0);
                throw new InvalidOperationException(ex.Message);
            }
        }

    }
}
