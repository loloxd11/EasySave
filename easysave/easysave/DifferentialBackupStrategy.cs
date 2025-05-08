using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

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

                // Créer un objet BackupJob pour suivre la progression
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

                    // Créer le répertoire cible s'il n'existe pas
                    string targetDirectory = Path.GetDirectoryName(targetFile);
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    // Vérifier si nous devons copier le fichier (s'il n'existe pas ou a été modifié)
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

                            // Mettre à jour les informations du fichier courant et déclencher la journalisation
                            job.UpdateCurrentFile(sourceFile, targetFile);
                        }
                        catch (Exception ex)
                        {
                            stopwatch.Stop();
                            job.LastFileTime = -1; // Un temps négatif indique une erreur
                            Console.WriteLine($"Erreur lors de la copie du fichier {sourceFile}: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Le fichier n'a pas besoin d'être copié, donc ne pas l'enregistrer dans les logs
                        job.LastFileTime = 0;
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
                Console.WriteLine($"Erreur lors de l'exécution de la sauvegarde différentielle: {ex.Message}");
                return false;
            }
        }

        private bool CompareFiles(string sourcePath, string targetPath)
        {
            // D'abord vérifier si les fichiers ont des tailles différentes
            FileInfo sourceInfo = new FileInfo(sourcePath);
            FileInfo targetInfo = new FileInfo(targetPath);

            if (sourceInfo.Length != targetInfo.Length)
            {
                return false;
            }

            // Ensuite comparer les dates de dernière modification
            if (sourceInfo.LastWriteTime != targetInfo.LastWriteTime)
            {
                return false;
            }

            // Pour une comparaison plus approfondie, on pourrait aussi comparer le contenu
            // des fichiers en utilisant un algorithme de hachage, mais cela pourrait être
            // trop lent pour les gros fichiers
            // Cette approche plus simple est basée sur la taille du fichier et la date de modification
            return true;
        }
    }
}
