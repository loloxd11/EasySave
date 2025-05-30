using System.ComponentModel;
using System.IO;

namespace EasySave.Models
{
    /// <summary>
    /// Represents a backup job, including its configuration, execution logic, and observer notification.
    /// </summary>
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
        // Current state of the job
        private JobState _state = JobState.inactive;
        // Progress percentage of the job
        private int _progress = 0;

        // Reference to the BackupManager to check pause state
        private BackupManager _backupManager;
        // Index of the job in the BackupManager's list
        private int _jobIndex = -1;

        // Indicates if the job is selected in the UI
        private bool _isSelected;
        /// <summary>
        /// Gets or sets whether the job is selected in the UI.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        /// <summary>
        /// Event triggered when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Configures the connection with the BackupManager.
        /// </summary>
        /// <param name="jobIndex">Index of the job in the manager's list.</param>
        /// <param name="backupManager">Reference to the BackupManager.</param>
        public void SetManagerInfo(int jobIndex, BackupManager backupManager)
        {
            _jobIndex = jobIndex;
            _backupManager = backupManager;
        }

        /// <summary>
        /// Indicates if the job is currently paused.
        /// </summary>
        public bool IsPaused => _backupManager != null && _jobIndex >= 0 && _backupManager.IsJobPaused(_jobIndex);

        /// <summary>
        /// Waits if the job is paused, supporting cancellation and observer notification.
        /// </summary>
        /// <param name="cancellationToken">Token to support cancellation.</param>
        /// <param name="sourcePath">Source path for observer notification.</param>
        /// <param name="targetPath">Target path for observer notification.</param>
        /// <param name="totalFiles">Total files for observer notification.</param>
        /// <param name="totalSize">Total size for observer notification.</param>
        /// <param name="currentProgress">Current progress for observer notification.</param>
        public void WaitIfPaused(CancellationToken cancellationToken = default, string sourcePath = "", string targetPath = "", int totalFiles = 0,
                                  long totalSize = 0, int currentProgress = 0)
        {
            if (_backupManager == null || _jobIndex < 0)
                return;

            // Wait while the job is paused
            while (_backupManager.IsJobPaused(_jobIndex))
            {
                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                Console.WriteLine($"Job '{Name}' (index {_jobIndex}) waiting for resume...");
                _state = JobState.paused;

                NotifyObservers("pause", Name, State, sourcePath, targetPath, totalFiles, totalSize, 0, 0, currentProgress);

                try
                {
                    // Use the provided CancellationToken to allow cancellation during pause
                    _backupManager.WaitForJobResume(_jobIndex, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Propagate the exception to cancel the operation
                    throw;
                }
                if (State == JobState.paused)
                {
                    _state = JobState.active;
                    NotifyObservers("resume", Name, State, sourcePath, targetPath, totalFiles, totalSize, 0, 0, currentProgress);
                }
                // Short sleep to avoid high CPU usage
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // List of observers to notify about job updates
        private List<IObserver> observers = new List<IObserver>();

        /// <summary>
        /// Attaches an observer to receive job updates.
        /// </summary>
        /// <param name="observer">The observer to attach.</param>
        public void AttachObserver(IObserver observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);
        }

        /// <summary>
        /// Notifies all attached observers of a job update.
        /// </summary>
        /// <param name="action">Action type (start, processing, pause, resume, complete, error, cancelled).</param>
        /// <param name="name">Job name.</param>
        /// <param name="state">Current job state.</param>
        /// <param name="sourcePath">Source path.</param>
        /// <param name="targetPath">Target path.</param>
        /// <param name="totalFiles">Total number of files.</param>
        /// <param name="totalSize">Total size in bytes.</param>
        /// <param name="transferTime">Transfer time in ms.</param>
        /// <param name="encryptionTime">Encryption time in ms.</param>
        /// <param name="currentProgress">Current progress value.</param>
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

        /// <summary>
        /// Executes the backup job using the configured strategy.
        /// </summary>
        /// <param name="cancellationToken">Token to support cancellation.</param>
        /// <returns>True if the job completed successfully, false otherwise.</returns>
        public bool ExecuteJob(CancellationToken cancellationToken = default)
        {
            try
            {
                var filesToCopy = backupStrategy.GetFilesToCopy(src, dst);
                return CopyFiles(filesToCopy, src, dst, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                State = JobState.inactive;
                Progress = 0;
                NotifyObservers("cancelled", Name, State, "", "", 0, 0, 0, 0, 0);
                return false;
            }
        }

        /// <summary>
        /// Copies the specified files from the source to the target directory, handling encryption, progress, and observer notification.
        /// </summary>
        /// <param name="filesToCopy">List of files to copy.</param>
        /// <param name="sourcePath">Source directory path.</param>
        /// <param name="targetPath">Target directory path.</param>
        /// <param name="cancellationToken">Token to support cancellation.</param>
        /// <returns>True if all files were copied successfully, false otherwise.</returns>
        public bool CopyFiles(List<string> filesToCopy, string sourcePath, string targetPath, CancellationToken cancellationToken = default)
        {
            try
            {
                int totalFiles = filesToCopy.Count;
                long totalSize = filesToCopy.Sum(f => new FileInfo(f).Length);
                int currentProgress = 0;
                State = JobState.active;

                // Notify observers of the start
                NotifyObservers("start", Name, State, sourcePath, targetPath, totalFiles, totalSize, 0, 0, 0);

                var encryptionService = EncryptionService.GetInstance();
                var transferCoordinator = TransferCoordinator.Instance;

                // 1. Register all pending priority files
                foreach (var file in filesToCopy)
                {
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();

                    string ext = Path.GetExtension(file);
                    if (transferCoordinator.IsPriorityExtension(ext))
                    {
                        transferCoordinator.RegisterPendingPriorityFile(file);
                    }
                }

                // Sort files so priority files are processed first
                filesToCopy = filesToCopy
                    .OrderByDescending(f => transferCoordinator.IsPriorityExtension(Path.GetExtension(f)))
                    .ToList();

                foreach (var sourceFile in filesToCopy)
                {
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();

                    string relativePath = Path.GetRelativePath(sourcePath, sourceFile);
                    string destFile = Path.Combine(targetPath, relativePath);
                    string destDir = Path.GetDirectoryName(destFile);
                    if (!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    long fileSize = new FileInfo(sourceFile).Length;
                    string ext = Path.GetExtension(sourceFile);
                    bool isPriority = transferCoordinator.IsPriorityExtension(ext);

                    // Wait if the job is paused before each file
                    WaitIfPaused(cancellationToken, sourcePath, targetPath, totalFiles, totalSize, currentProgress);

                    // 2. Request transfer authorization
                    transferCoordinator.RequestTransfer(sourceFile, fileSize);

                    try
                    {
                        // Measure transfer time
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        File.Copy(sourceFile, destFile, true);
                        sw.Stop();
                        long transferTime = sw.ElapsedMilliseconds;

                        long encryptionTime = 0;
                        // Encrypt the file if required
                        if (encryptionService.ShouldEncryptFile(destFile))
                        {
                            encryptionTime = encryptionService.EncryptFile(destFile);
                        }

                        currentProgress++;

                        // Notify observers of progress, including transfer and encryption times
                        NotifyObservers("processing", Name, State, sourceFile, destFile, totalFiles, totalSize, transferTime, encryptionTime, currentProgress);
                    }
                    finally
                    {
                        // 3. Release the resource after transfer
                        transferCoordinator.ReleaseTransfer(sourceFile);

                        // 4. Unregister if priority
                        if (isPriority)
                        {
                            transferCoordinator.UnregisterPendingPriorityFile(sourceFile);
                        }
                    }
                }

                // Notify observers of completion
                State = JobState.completed;
                Progress = 100;
                NotifyObservers("complete", Name, State, sourcePath, targetPath, totalFiles, totalSize, 0, 0, currentProgress);
                return true;
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation and notify observers
                State = JobState.inactive;
                NotifyObservers("cancelled", Name, State, sourcePath, targetPath, 0, 0, 0, 0, 0);
                return false;
            }
            catch (Exception ex)
            {
                State = JobState.error;
                NotifyObservers("error", Name, State, sourcePath, targetPath, 0, 0, 0, 0, 0);
                Console.WriteLine($"Error while copying files for '{Name}': {ex.Message}");
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
        /// Gets the index of the job in the BackupManager's list.
        /// </summary>
        public int JobIndex => _jobIndex;

        /// <summary>
        /// Gets the type of backup.
        /// </summary>
        public BackupType Type => type;

        /// <summary>
        /// Gets or sets the current state of the backup job.
        /// Notifies property change listeners when the value changes.
        /// </summary>
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
