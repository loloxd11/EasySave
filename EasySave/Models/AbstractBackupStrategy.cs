using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    /// <summary>
    /// Abstract base class for backup strategies.
    /// Implements observer pattern and provides utility methods for backup operations.
    /// </summary>
    public abstract class AbstractBackupStrategy
    {
        /// <summary>
        /// List of observers to notify about backup job updates.
        /// </summary>
        protected List<IObserver> observers = new List<IObserver>();

        /// <summary>
        /// Current state of the backup job.
        /// </summary>
        protected JobState state;

        /// <summary>
        /// Total number of files to process in the backup.
        /// </summary>
        protected int totalFiles;

        /// <summary>
        /// Total size of all files to process in the backup (in bytes).
        /// </summary>
        protected long totalSize;
        protected int currentProgress;
        protected long LastFileTime;

        /// <summary>
        /// Name of the backup job.
        /// </summary>
        protected string name;

        /// <summary>
        /// Attach an observer to receive updates about the backup job.
        /// </summary>
        /// <param name="observer">The observer to attach.</param>
        public static class BackupActions
        {
            public const string Start = "start";
            public const string Processing = "processing";
            public const string Transfer = "transfer";
            public const string Complete = "complete";
            public const string Error = "error";
            public const string Progress = "progress";
        }
        public void AttachObserver(IObserver observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
        }

        /// <summary>
        /// Méthode unifiée pour notifier tous les observateurs avec différents types de mises à jour
        /// </summary>
        /// <param name="action">Type d'action (start, processing, transfer, complete, error, progress)</param>
        /// <param name="name">Nom du travail de sauvegarde</param>
        /// <param name="sourcePath">Chemin source (peut être vide pour les mises à jour de progression)</param>
        /// <param name="targetPath">Chemin cible (peut être vide pour les mises à jour de progression)</param>
        /// <param name="fileSize">Taille du fichier (pour les transferts)</param>
        /// <param name="transferTime">Temps de transfert (pour les transferts)</param>
        /// <param name="encryptionTime">Temps de chiffrement (pour les transferts)</param>
        /// <param name="currentProgress">Valeur de progression actuelle (optionnel, utilisé pour les mises à jour de progression)</param>
        public void NotifyObserver(
            string action,
            string name,
            JobState state, 
            string sourcePath = "",
            string targetPath = "",
            int totalFiles = 0,
            long totalSize = 0,
            long transferTime = 0,
            long encryptionTime = 0,
            int currentProgress = 0)
        {

            // Déterminer le type de sauvegarde en cours
            BackupType backupType = this is CompleteBackupStrategy ? BackupType.Complete : BackupType.Differential;

            // Les paramètres fileSize, transferTime et encryptionTime pourraient être ajoutés à l'interface IObserver
            // si nécessaire, mais pour l'instant ils ne sont pas utilisés dans l'appel à Update
            foreach (var observer in observers)
            {
                observer.Update(action, name, backupType, state, sourcePath, targetPath,
                    totalFiles, totalSize, currentProgress);
            }
        }

        /*public void UpdateProgress(int files, long size)
        {
            progression = files;
            // Notifier les observateurs du changement de progression
            NotifyObserver(BackupActions.Progress, name, currentProgress: files);
        }*/

        /// <summary>
        /// Update the current file being processed and notify observers.
        /// </summary>
        /// <param name="source">Source path of the current file.</param>
        /// <param name="target">Target path of the current file.</param>
        public void UpdateCurrentFile(string source, string target)
        {
            // Update current file being processed
            LastFileTime = DateTime.Now.Ticks;
            // Utiliser les constantes standardisées et passer l'état correctement
            NotifyObserver(BackupActions.Processing, name, state, source, target);
        }

        /// <summary>
        /// Execute the backup strategy.
        /// </summary>
        /// <param name="name">Name of the backup job.</param>
        /// <param name="src">Source directory path.</param>
        /// <param name="dst">Destination directory path.</param>
        /// <param name="order">Order or mode of execution.</param>
        /// <returns>True if the backup was successful, false otherwise.</returns>
        public abstract bool Execute(string name, string src, string dst, string order);

        /// <summary>
        /// Calculate the total number of files in the source directory (recursively).
        /// </summary>
        /// <param name="source">Source directory path.</param>
        /// <returns>Total number of files.</returns>
        public int CalculateTotalFiles(string source)
        {
            var files = ScanDirectory(source);
            return files.Count;
        }

        /// <summary>
        /// Recursively scan a directory and return a list of all file paths.
        /// </summary>
        /// <param name="path">Directory path to scan.</param>
        /// <returns>List of file paths.</returns>
        protected List<string> ScanDirectory(string path)
        {
            var result = new List<string>();

            try
            {
                // Add files in the current directory
                result.AddRange(Directory.GetFiles(path));

                // Recursively add files from subdirectories
                foreach (var directory in Directory.GetDirectories(path))
                {
                    result.AddRange(ScanDirectory(directory));
                }
            }
            catch (Exception ex)
            {
                // Log exception
                Console.WriteLine($"Error scanning directory {path}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Get the size of a file in bytes.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns>File size in bytes, or 0 if the file cannot be accessed.</returns>
        protected long GetFileSize(string path)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                return fileInfo.Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Calculate the total size of all files in the source directory (recursively).
        /// </summary>
        /// <param name="source">Source directory path.</param>
        /// <returns>Total size in bytes.</returns>
        public long CalculateTotalSize(string source)
        {
            long size = 0;
            List<string> files = ScanDirectory(source);

            foreach (string file in files)
            {
                size += GetFileSize(file);
            }

            return size;
        }
    }
}
