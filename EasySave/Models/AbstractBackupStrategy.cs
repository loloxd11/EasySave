using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public abstract class AbstractBackupStrategy
    {
        protected List<IObserver> observers = new List<IObserver>();
        protected JobState state;
        protected int totalFiles;
        protected long totalSize;
        protected int currentProgress;
        protected long LastFileTime;
        protected string name;

        // Constantes pour standardiser les valeurs d'action
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

        public void UpdateCurrentFile(string source, string target)
        {
            // Update current file being processed
            LastFileTime = DateTime.Now.Ticks;
            // Utiliser les constantes standardisées et passer l'état correctement
            NotifyObserver(BackupActions.Processing, name, state, source, target);
        }

        public abstract bool Execute(string name, string src, string dst, string order);

        public int CalculateTotalFiles(string source)
        {
            var files = ScanDirectory(source);
            return files.Count;
        }

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
