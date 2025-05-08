using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace EasySave
{
    public class CompleteBackupStrategy : AbstractBackupStrategy
    {
        public CompleteBackupStrategy(StateManager stateManager, LogManager logManager) : base(stateManager, logManager)
        {
        }

        public override bool Execute(string source, string target, string name)
        {
            try
            {
                if (!Directory.Exists(source))
                {
                    throw new DirectoryNotFoundException($"Le répertoire source n'existe pas: {source}");
                }

                // Créer le répertoire cible s'il n'existe pas
                if (!Directory.Exists(target))
                {
                    Directory.CreateDirectory(target);
                }

                // Obtenir tous les fichiers du répertoire source et des sous-répertoires
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

                // Créer un objet BackupJob temporaire pour suivre la progression
                // Dans une implémentation réelle, cela serait lié à la tâche BackupJob réelle
                BackupJob job = new BackupJob(name, source, target, BackupType.Complete, this);
                job.TotalFiles = totalFiles;
                job.TotalSize = totalSize;

                // Traiter chaque fichier
                foreach (string sourceFile in files)
                {
                    // Calculer le chemin relatif
                    string relativePath = sourceFile.Substring(source.Length).TrimStart('\\', '/');
                    string targetFile = Path.Combine(target, relativePath);

                    // Créer le répertoire cible s'il n'existe pas
                    string targetDirectory = Path.GetDirectoryName(targetFile);
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    // Mettre à jour les informations du fichier courant
                    job.UpdateCurrentFile(sourceFile, targetFile);

                    // Copier le fichier et mesurer le temps
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    try
                    {
                        File.Copy(sourceFile, targetFile, true);
                        stopwatch.Stop();
                        job.LastFileTime = stopwatch.ElapsedMilliseconds;
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        job.LastFileTime = -1; // Un temps négatif indique une erreur
                        Console.WriteLine($"Erreur lors de la copie du fichier {sourceFile}: {ex.Message}");
                    }

                    // Mettre à jour la progression
                    long fileSize = GetFileSize(sourceFile);
                    remainingFiles--;
                    remainingSize -= fileSize;
                    job.UpdateProgress(remainingFiles, remainingSize);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'exécution de la sauvegarde complète: {ex.Message}");
                return false;
            }
        }
    }
}
