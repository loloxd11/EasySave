using System;
using System.Collections.Generic;
using System.IO;

namespace EasySave
{
    public class BackupJob
    {
        private string name;
        private string sourcePath;
        private string targetPath;
        private BackupType type;
        private List<IObserver> observers;
        private JobState state;
        private int totalFiles;
        private long totalSize;
        private int progression;
        private AbstractBackupStrategy backupStrategy;
        private float lastFileTime;

        public string Name { get => name; }
        public string SourcePath { get => sourcePath; }
        public string TargetPath { get => targetPath; }
        public BackupType Type { get => type; }
        public JobState State { get => state; set => state = value; }
        public int TotalFiles { get => totalFiles; set => totalFiles = value; }
        public long TotalSize { get => totalSize; set => totalSize = value; }
        public int Progression { get => progression; set => progression = value; }
        public float LastFileTime { get => lastFileTime; set => lastFileTime = value; }
        public string CurrentSourceFile { get; private set; }
        public string CurrentTargetFile { get; private set; }
        public int RemainingFiles { get; private set; }
        public long RemainingSize { get; private set; }
        public IEnumerable<IObserver> Observers => observers;

        public BackupJob(string name, string source, string target, BackupType type, AbstractBackupStrategy strategy)
        {
            this.name = name;
            this.sourcePath = source;
            this.targetPath = target;
            this.type = type;
            this.backupStrategy = strategy;
            this.observers = new List<IObserver>();
            this.state = JobState.Inactive;
            this.totalFiles = 0;
            this.totalSize = 0;
            this.progression = 0;
            this.lastFileTime = 0;
            this.RemainingFiles = 0;
            this.RemainingSize = 0;
        }

        public bool Execute()
        {
            try
            {
                // Mettre à jour l'état comme actif
                state = JobState.Active;

                // Calculer le nombre total de fichiers et la taille
                totalFiles = backupStrategy.CalculateTotalFiles(sourcePath);
                RemainingFiles = totalFiles;

                // Notifier les observateurs que la tâche a démarré
                NotifyObservers("start");

                // Exécuter la stratégie de sauvegarde
                bool result = backupStrategy.Execute(sourcePath, targetPath, name);

                // Mettre à jour l'état en fonction du résultat
                state = result ? JobState.Completed : JobState.Error;

                // Notifier les observateurs que la tâche est terminée
                NotifyObservers("finish");

                return result;
            }
            catch (Exception ex)
            {
                state = JobState.Error;
                Console.WriteLine($"Erreur lors de l'exécution de la tâche de sauvegarde {name}: {ex.Message}");
                NotifyObservers("error");
                return false;
            }
        }

        public void AttachObserver(IObserver observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
        }

        public void NotifyObservers(string action)
        {
            foreach (var observer in observers)
            {
                observer.Update(this, action);
            }
        }

        public void UpdateProgress(int files, long size)
        {
            int filesProcessed = totalFiles - files;
            progression = (totalFiles > 0) ? (int)((double)filesProcessed / totalFiles * 100) : 0;
            RemainingFiles = files;
            RemainingSize = size;

            // Notifier les observateurs de la mise à jour de progression
            NotifyObservers("progress");
        }

        public void UpdateCurrentFile(string source, string target)
        {
            CurrentSourceFile = source;
            CurrentTargetFile = target;

            // Notifier les observateurs de la mise à jour du fichier
            NotifyObservers("file");
        }
    }
}
