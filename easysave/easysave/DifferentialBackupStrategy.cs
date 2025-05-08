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

        public override bool Execute(string source, string target, string name)
        {
            try
            {
                if (!Directory.Exists(source))
                {
                    throw new DirectoryNotFoundException($"Le r�pertoire source n'existe pas: {source}");
                }

                // Cr�er le r�pertoire cible s'il n'existe pas
                if (!Directory.Exists(target))
                {
                    Directory.CreateDirectory(target);
                }

                // Obtenir tous les fichiers du r�pertoire source et des sous-r�pertoires
                List<string> files = ScanDirectory(source);

                int totalFiles = files.Count;
                int remainingFiles = totalFiles;
                long totalSize = 0;
                long remainingSize = 0;

                // Calculer la taille totale
                foreach (string file in files)
                {
                    totalSize += GetFileSize(file);
                }

                remainingSize = totalSize;

                // Cr�er un objet BackupJob pour suivre la progression
                BackupJob job = new BackupJob(name, source, target, BackupType.Differential, this);
                job.TotalFiles = totalFiles;
                job.TotalSize = totalSize;

                // Attacher notre LogManager comme observateur
                job.AttachObserver(logManager);

                // Traiter chaque fichier
                foreach (string sourceFile in files)
                {
                    // Calculer le chemin relatif
                    string relativePath = sourceFile.Substring(source.Length).TrimStart('\\', '/');
                    string targetFile = Path.Combine(target, relativePath);

                    // Cr�er le r�pertoire cible s'il n'existe pas
                    string targetDirectory = Path.GetDirectoryName(targetFile);
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    // V�rifier si nous devons copier le fichier (s'il n'existe pas ou a �t� modifi�)
                    bool needsCopy = !File.Exists(targetFile) || !CompareFiles(sourceFile, targetFile);

                    if (needsCopy)
                    {
                        // Copier le fichier et mesurer le temps
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();

                        try
                        {
                            File.Copy(sourceFile, targetFile, true);
                            stopwatch.Stop();
                            job.LastFileTime = stopwatch.ElapsedMilliseconds;

                            // Mettre � jour les informations du fichier courant et d�clencher la journalisation
                            job.UpdateCurrentFile(sourceFile, targetFile);
                        }
                        catch (Exception ex)
                        {
                            stopwatch.Stop();
                            job.LastFileTime = -1; // Un temps n�gatif indique une erreur
                            Console.WriteLine($"Erreur lors de la copie du fichier {sourceFile}: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Le fichier n'a pas besoin d'�tre copi�, donc ne pas l'enregistrer dans les logs
                        job.LastFileTime = 0;
                    }

                    // Mettre � jour la progression
                    long fileSize = GetFileSize(sourceFile);
                    remainingFiles--;
                    remainingSize -= fileSize;
                    job.UpdateProgress(remainingFiles, remainingSize);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'ex�cution de la sauvegarde diff�rentielle: {ex.Message}");
                return false;
            }
        }
            

        private bool CompareFiles(string sourcePath, string targetPath)
        {
            // Comparer les hash des fichiers
            string sourceHash = ComputeFileHash(sourcePath);
            string targetHash = ComputeFileHash(targetPath);

            return sourceHash == targetHash;
        }

        private string ComputeFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private bool AreFilesModified(string sourceFilePath, string targetFilePath)
        {
            // V�rifie si les fichiers existent  
            if (!File.Exists(sourceFilePath) || !File.Exists(targetFilePath))
            {
                return true; // Consid�rer comme modifi� si l'un des fichiers n'existe pas  
            }

            // Compare les hash des fichiers  
            string sourceHash = ComputeFileHash(sourceFilePath);
            string targetHash = ComputeFileHash(targetFilePath);

            return !sourceHash.Equals(targetHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
