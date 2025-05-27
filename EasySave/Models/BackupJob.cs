using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public class BackupJob : INotifyPropertyChanged
    {
        // Name of the backup job
        private string name;
        // Source directory path
        private string src;
        // Destination directory path
        private string dst;
        // Type of backup (Complete or Differential)
        private BackupType type;
        // Strategy used to execute the backup
        private AbstractBackupStrategy backupStrategy;
        private JobState _state = JobState.inactive;
        private int _progress = 0;

        // Référence au BackupManager pour vérifier l'état de pause
        private BackupManager _backupManager;
        // Index du job dans la liste du BackupManager
        private int _jobIndex = -1;

        public event PropertyChangedEventHandler PropertyChanged;


        // Méthode pour configurer la connexion avec le BackupManager
        public void SetManagerInfo(int jobIndex, BackupManager backupManager)
        {
            _jobIndex = jobIndex;
            _backupManager = backupManager;
        }

        // Méthode pour vérifier si le job devrait être en pause
        public bool IsPaused => _backupManager != null && _jobIndex >= 0 && _backupManager.IsJobPaused(_jobIndex);

        // Méthode pour attendre si le job est en pause
        public void WaitIfPaused()
        {
            if (_backupManager == null || _jobIndex < 0)
                return;

            // Tant que le job est en pause, on attend
            while (_backupManager.IsJobPaused(_jobIndex))
            {
                Console.WriteLine($"Job '{Name}' (indice {_jobIndex}) en attente de reprise...");
                
                // Utiliser CancellationToken.None car nous voulons juste attendre la reprise
                _backupManager.WaitForJobResume(_jobIndex, CancellationToken.None);
                
                // Courte pause pour éviter une consommation CPU excessive
                Thread.Sleep(100);
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private List<IObserver> observers = new List<IObserver>();

        public void AttachObserver(IObserver observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);
        }

        private void NotifyObservers(string action, string name, JobState state,
                                        string sourcePath = "", string targetPath = "", int totalFiles = 0,
                                        long totalSize = 0, long transferTime = 0, long encryptionTime = 0,
                                        int currentProgress = 0)
        {
            foreach (var observer in observers)
            {
                observer.Update(
                    action,
                    name,
                    type,
                    state,
                    sourcePath,
                    targetPath,
                    totalFiles,
                    totalSize,
                    transferTime,
                    encryptionTime,
                    currentProgress
                );
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="BackupJob"/> class.
        /// </summary>
        /// <param name="name">The name of the backup job.</param>
        /// <param name="source">The source directory path.</param>
        /// <param name="target">The destination directory path.</param>
        /// <param name="type">The type of backup (Complete or Differential).</param>
        /// <param name="strategy">The backup strategy to use.</param>
        public BackupJob(string name, string source, string target, BackupType type, AbstractBackupStrategy strategy)
        {
            this.name = name;
            this.src = source;
            this.dst = target;
            this.type = type;
            this.backupStrategy = strategy;
        }

        public bool ExecuteJob()
        {
            var filesToCopy = backupStrategy.GetFilesToCopy(src, dst);
            return CopyFiles(filesToCopy, src, dst);
        }


        public bool CopyFiles(List<string> filesToCopy, string sourcePath, string targetPath)
        {
            try
            {
                int totalFiles = filesToCopy.Count;
                long totalSize = filesToCopy.Sum(f => new FileInfo(f).Length);
                int currentProgress = 0;
                State = JobState.active;

                // Notifier le début
                NotifyObservers("start", Name, State, sourcePath, targetPath, totalFiles, totalSize, 0, 0, 0);

                var encryptionService = EncryptionService.GetInstance();
                var transferCoordinator = TransferCoordinator.Instance;

                // 1. Enregistrer tous les fichiers prioritaires en attente
                foreach (var file in filesToCopy)
                {
                    string ext = Path.GetExtension(file);
                    if (transferCoordinator.IsPriorityExtension(ext))
                    {
                        transferCoordinator.RegisterPendingPriorityFile(file);
                    }
                }

                filesToCopy = filesToCopy
                    .OrderByDescending(f => transferCoordinator.IsPriorityExtension(Path.GetExtension(f)))
                    .ToList();

                foreach (var sourceFile in filesToCopy)
                {
                    // Vérifier si le job doit être en pause et attendre si nécessaire
                    WaitIfPaused();
                    
                    string relativePath = Path.GetRelativePath(sourcePath, sourceFile);
                    string destFile = Path.Combine(targetPath, relativePath);
                    string destDir = Path.GetDirectoryName(destFile);
                    if (!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    long fileSize = new FileInfo(sourceFile).Length;
                    string ext = Path.GetExtension(sourceFile);
                    bool isPriority = transferCoordinator.IsPriorityExtension(ext);

                    // 2. Demander l'autorisation de transfert
                    transferCoordinator.RequestTransfer(sourceFile, fileSize);

                    // Mesurer le temps de transfert
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    File.Copy(sourceFile, destFile, true);
                    sw.Stop();
                    long transferTime = sw.ElapsedMilliseconds;

                    long encryptionTime = 0;
                    // Vérifier si le fichier doit être chiffré
                    if (encryptionService.ShouldEncryptFile(destFile))
                    {
                        encryptionTime = encryptionService.EncryptFile(destFile);
                    }

                    // 3. Libérer la ressource après transfert
                    transferCoordinator.ReleaseTransfer(sourceFile);

                    // 4. Si prioritaire, désenregistrer
                    if (isPriority)
                    {
                        transferCoordinator.UnregisterPendingPriorityFile(sourceFile);
                    }

                    currentProgress++;
                    // Notifier la progression avec le temps de transfert et de chiffrement
                    NotifyObservers("processing", Name, State, sourceFile, destFile, totalFiles, totalSize, transferTime, encryptionTime, currentProgress);
                }

                // Notifier la fin
                State = JobState.completed;
                NotifyObservers("complete", Name, State, sourcePath, targetPath, totalFiles, totalSize, 0, 0, currentProgress);
                return true;
            }
            catch (Exception ex)
            {
                State = JobState.error;
                NotifyObservers("error", Name, State, sourcePath, targetPath, 0, 0, 0, 0, 0);
                return false;
            }
        }


        // Properties to access private fields

        /// <summary>
        /// Gets the name of the backup job.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Gets the source directory path.
        /// </summary>
        public string Source => src;

        /// <summary>
        /// Gets the destination directory path.
        /// </summary>
        public string Destination => dst;

        /// <summary>
        /// Gets the type of backup.
        /// </summary>
        public BackupType Type => type;

        public JobState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged(nameof(State));
                }
            }
        }

        /// <summary>
        /// Gets or sets the progress of the backup job as a percentage.
        /// Notifies property change listeners when the value changes.
        /// </summary>
        public int Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }
    }
}
