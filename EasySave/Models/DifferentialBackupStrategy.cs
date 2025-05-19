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
        private BackupType backupType = BackupType.Differential;

        public override bool Execute(string name, string sourcePath, string targetPath, string order)
        {
            this.name = name;
            state = JobState.active;

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

                // Traiter tous les fichiers pour la progression
                foreach (string sourceFile in sourceFiles)
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

                        // Copy file
                        long fileSize = GetFileSize(sourceFile);
                        long startTime = DateTime.Now.Ticks;

                        File.Copy(sourceFile, destFile, true);

                        long endTime = DateTime.Now.Ticks;
                        long transferTime = endTime - startTime;

                        // Notify observers
                        NotifyObserver(BackupActions.Processing, name, state, sourceFile, destFile, totalFiles, totalSize, transferTime, 0, currentProgress);
                    }

                    // Update progress (une seule fois par fichier)
                    remainFiles--;
                    currentProgress = totalFiles - remainFiles;
                }

                state = JobState.completed;
                NotifyObserver(BackupActions.Complete, name, state, sourcePath, targetPath, totalFiles, totalSize, 0, 0, totalFiles);
                return true;
            }
            catch (Exception ex)
            {
                state = JobState.error;
                NotifyObserver(BackupActions.Error, name, state, sourcePath, targetPath, totalFiles, totalSize, 0, 0, currentProgress);
                Console.WriteLine($"Error executing differential backup: {ex.Message}");
                return false;
            }
        }
    }
}
