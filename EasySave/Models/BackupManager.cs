using System.ComponentModel;
using System.IO;
using System.Windows;

namespace EasySave.Models
{
    /// <summary>
    /// Singleton class responsible for managing backup jobs, their configuration, and execution.
    /// Handles adding, updating, removing, listing, and executing backup jobs.
    /// </summary>
    public class BackupManager
    {
        // Singleton instance of BackupManager
        private static BackupManager _instance;
        // List of all backup jobs managed by this manager
        private List<BackupJob> backupJobs;
        // Singleton instance of ConfigManager for configuration management
        private static ConfigManager _configManager;
        // Lock object for thread safety
        private readonly object lockObject = new object();
        // Business software detector instance
        private SM_Detector _smDetector;
        // CancellationTokenSource for the business software detector
        private CancellationTokenSource _detectorTokenSource;
        // Thread for monitoring the business software
        private Thread _detectorThread;

        // Dictionary to track paused state of jobs by their indices
        private Dictionary<int, bool> _pausedJobIndices;

        // Dictionary of ManualResetEventSlim for resuming paused jobs
        private Dictionary<int, ManualResetEventSlim> _resumeEvents;

        // Dictionary to track CancellationTokenSource for running jobs
        private Dictionary<int, CancellationTokenSource> _jobCancellationTokens = new Dictionary<int, CancellationTokenSource>();

        // List of global observers (e.g., for remote monitoring)
        private List<IObserver> _globalObservers = new List<IObserver>();


        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// Initializes the backup jobs list and starts the business software detector.
        /// </summary>
        private BackupManager()
        {
            backupJobs = new List<BackupJob>();
            _pausedJobIndices = new Dictionary<int, bool>();
            _resumeEvents = new Dictionary<int, ManualResetEventSlim>();

            // Initialize the business software detector
            _detectorTokenSource = new CancellationTokenSource();
            _smDetector = new SM_Detector(this);

            // Create and start the monitoring thread
            _detectorThread = new Thread(() => _smDetector.StartMonitoring(_detectorTokenSource.Token));
            _detectorThread.IsBackground = true;
            _detectorThread.Start();
        }

        /// <summary>
        /// Gets the singleton instance of BackupManager.
        /// Loads configuration on first instantiation.
        /// </summary>
        /// <returns>The singleton BackupManager instance.</returns>
        public static BackupManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new BackupManager();
                _configManager = ConfigManager.GetInstance();
                _configManager.LoadConfiguration(); // Load configuration on instance creation
            }
            return _instance;
        }

        /// <summary>
        /// Checks if a specific job is currently paused.
        /// </summary>
        /// <param name="index">Index of the job in the list.</param>
        /// <returns>True if the job is paused, otherwise False.</returns>
        public bool IsJobPaused(int index)
        {
            lock (lockObject)
            {
                // Check if the index is valid
                if (index < 0 || index >= backupJobs.Count)
                    return false;

                return _pausedJobIndices.TryGetValue(index, out bool isPaused) && isPaused;
            }
        }

        /// <summary>
        /// Checks if all jobs are currently paused.
        /// </summary>
        /// <returns>True if all jobs are paused, otherwise False.</returns>
        public bool AreAllJobsPaused()
        {
            lock (lockObject)
            {
                if (backupJobs.Count == 0 || _pausedJobIndices.Count == 0)
                    return false;

                // Check that all existing jobs are paused
                for (int i = 0; i < backupJobs.Count; i++)
                {
                    if (!_pausedJobIndices.TryGetValue(i, out bool isPaused) || !isPaused)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Pauses specific backup jobs by their indices.
        /// </summary>
        /// <param name="indices">Collection of job indices to pause (null for all jobs).</param>
        /// <param name="reason">Reason for pausing.</param>
        public void PauseBackupJobs(IEnumerable<int> indices = null, string reason = "")
        {
            lock (lockObject)
            {
                // If indices is null, pause all jobs
                if (indices == null)
                {
                    indices = Enumerable.Range(0, backupJobs.Count);
                }

                foreach (var index in indices)
                {
                    // Check if the index is valid
                    if (index < 0 || index >= backupJobs.Count)
                        continue;

                    // Create the event if it does not exist yet
                    if (!_resumeEvents.ContainsKey(index))
                    {
                        _resumeEvents[index] = new ManualResetEventSlim(true);
                    }

                    // Pause the job
                    _pausedJobIndices[index] = true;
                    _resumeEvents[index].Reset(); // Set the event to non-signaled

                    Console.WriteLine($"Job '{backupJobs[index].Name}' (index {index}) paused. Reason: {reason}");
                }
            }
        }

        /// <summary>
        /// Resumes specific backup jobs that are paused.
        /// </summary>
        /// <param name="indices">Collection of job indices to resume (null for all jobs).</param>
        public void ResumeBackupJobs(IEnumerable<int> indices = null)
        {
            lock (lockObject)
            {
                // If indices is null, resume all jobs
                if (indices == null)
                {
                    indices = _pausedJobIndices.Keys.ToList();
                }

                foreach (var index in indices)
                {
                    // Check if the job is paused
                    if (!_pausedJobIndices.TryGetValue(index, out bool isPaused) || !isPaused)
                        continue;

                    // Check if the index is still valid
                    if (index >= 0 && index < backupJobs.Count)
                    {
                        // Mark the job as not paused
                        _pausedJobIndices[index] = false;

                        // Signal the event to unblock the job if it exists
                        if (_resumeEvents.TryGetValue(index, out var resumeEvent))
                        {
                            resumeEvent.Set();
                        }

                        Console.WriteLine($"Job '{backupJobs[index].Name}' (index {index}) resumed");
                    }
                }
            }
        }

        /// <summary>
        /// Waits for a specific job to be resumed if it is paused.
        /// </summary>
        /// <param name="index">Index of the job.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if resumed, False if the operation is cancelled.</returns>
        public bool WaitForJobResume(int index, CancellationToken cancellationToken)
        {
            try
            {
                // If the index is invalid or the job is not paused, return immediately
                if (index < 0 || index >= backupJobs.Count || !IsJobPaused(index))
                    return true;

                // If there is no event for this job, create one
                lock (lockObject)
                {
                    if (!_resumeEvents.TryGetValue(index, out var _))
                    {
                        _resumeEvents[index] = new ManualResetEventSlim(true);
                    }
                }

                // Wait for the event to be signaled (resume) or for cancellation
                return _resumeEvents[index].Wait(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        /// <summary>
        /// Adds a new pause/resume event for a new job.
        /// </summary>
        /// <param name="index">Index of the job.</param>
        private void InitializeJobPauseState(int index)
        {
            if (index >= 0)
            {
                _pausedJobIndices[index] = false;
                _resumeEvents[index] = new ManualResetEventSlim(true);
            }
        }

        /// <summary>
        /// Updates indices after a job is removed.
        /// </summary>
        /// <param name="removedIndex">Index of the removed job.</param>
        private void UpdateIndicesAfterRemoval(int removedIndex)
        {
            lock (lockObject)
            {
                // Remove the state and event of the removed job
                _pausedJobIndices.Remove(removedIndex);
                if (_resumeEvents.TryGetValue(removedIndex, out var eventToDispose))
                {
                    eventToDispose.Dispose();
                    _resumeEvents.Remove(removedIndex);
                }

                // Create new dictionaries with updated indices
                var newPausedJobIndices = new Dictionary<int, bool>();
                var newResumeEvents = new Dictionary<int, ManualResetEventSlim>();

                // Shift all indices greater than the removed one
                foreach (var kvp in _pausedJobIndices)
                {
                    int oldIndex = kvp.Key;
                    bool isPaused = kvp.Value;

                    int newIndex = oldIndex > removedIndex ? oldIndex - 1 : oldIndex;
                    newPausedJobIndices[newIndex] = isPaused;
                }

                foreach (var kvp in _resumeEvents)
                {
                    int oldIndex = kvp.Key;
                    var resumeEvent = kvp.Value;

                    int newIndex = oldIndex > removedIndex ? oldIndex - 1 : oldIndex;
                    newResumeEvents[newIndex] = resumeEvent;
                }

                // Replace the dictionaries
                _pausedJobIndices = newPausedJobIndices;
                _resumeEvents = newResumeEvents;
            }
        }

        /// <summary>
        /// Adds a new backup job if it does not already exist.
        /// Attaches state and log observers to the backup strategy.
        /// Saves the updated job list to configuration.
        /// </summary>
        /// <param name="name">Name of the backup job.</param>
        /// <param name="source">Source directory path.</param>
        /// <param name="target">Target directory path.</param>
        /// <param name="type">Type of backup (Complete or Differential).</param>
        /// <returns>True if the job was added, false if a job with the same name exists.</returns>
        public bool AddBackupJob(string name, string source, string target, BackupType type)
        {
            lock (lockObject)
            {
                // Verify the job doesn't already exist
                if (backupJobs.Any(job => job.Name == name))
                {
                    return false;
                }

                // Create the appropriate strategy based on the backup type
                var strategy = CreateBackupStrategy(type);

                // Create and add the new backup job
                var job = new BackupJob(name, source, target, type, strategy);
                backupJobs.Add(job);

                // Add a state observer to the backup strategy
                StateManager stateManager = StateManager.GetInstance();
                job.AttachObserver(stateManager);

                // Add log observer to the backup strategy
                LogManager logManager = LogManager.GetInstance();
                job.AttachObserver(logManager);

                // Add global observers (e.g., RemoteConsoleServer)
                foreach (var obs in _globalObservers)
                    job.AttachObserver(obs);

                // Initialize the pause state for the new job
                InitializeJobPauseState(backupJobs.Count - 1);

                // Save the backup job to configuration
                SaveBackupJobsToConfig();

                return true;
            }
        }

        /// <summary>
        /// Updates an existing backup job with new parameters.
        /// Replaces the old job and attaches observers to the new strategy.
        /// Saves the updated job list to configuration.
        /// </summary>
        /// <param name="name">Name of the backup job to update.</param>
        /// <param name="source">New source directory path.</param>
        /// <param name="target">New target directory path.</param>
        /// <param name="type">New backup type.</param>
        /// <returns>True if the job was updated, false if not found.</returns>
        public bool UpdateBackupJob(string name, string source, string target, BackupType type)
        {
            lock (lockObject)
            {
                // Find the job to update
                int index = backupJobs.FindIndex(job => job.Name == name);
                if (index == -1)
                {
                    return false;
                }

                // Save the pause state
                bool wasPaused = IsJobPaused(index);

                // Remove the old job
                backupJobs.RemoveAt(index);

                // Create a new job with updated parameters
                var strategy = CreateBackupStrategy(type);
                var job = new BackupJob(name, source, target, type, strategy);
                backupJobs.Insert(index, job);

                // Add a state observer to the backup strategy
                StateManager stateManager = StateManager.GetInstance();
                job.AttachObserver(stateManager);

                // Add log observer to the backup strategy
                LogManager logManager = LogManager.GetInstance();
                job.AttachObserver(logManager);

                // Add global observers (e.g., RemoteConsoleServer)
                foreach (var obs in _globalObservers)
                    job.AttachObserver(obs);

                // Restore the pause state
                _pausedJobIndices[index] = wasPaused;

                // Save the backup job to configuration
                SaveBackupJobsToConfig();

                return true;
            }
        }

        /// <summary>
        /// Removes a backup job by its index in the list.
        /// Saves the updated job list to configuration.
        /// </summary>
        /// <param name="index">Index of the backup job to remove.</param>
        /// <returns>True if the job was removed, false if index is invalid.</returns>
        public bool RemoveBackup(int index)
        {
            lock (lockObject)
            {
                if (index < 0 || index >= backupJobs.Count)
                {
                    return false;
                }

                backupJobs.RemoveAt(index);

                // Update indices after removal
                UpdateIndicesAfterRemoval(index);

                SaveBackupJobsToConfig();

                return true;
            }
        }

        /// <summary>
        /// Returns the list of all backup jobs.
        /// </summary>
        /// <returns>List of BackupJob objects.</returns>
        public List<BackupJob> ListBackups()
        {
            return backupJobs;
        }

        /// <summary>
        /// Executes the backup jobs specified by their indices.
        /// Checks if business software is running before executing backups.
        /// </summary>
        /// <param name="jobIndexes">List of job indices to execute.</param>
        /// <returns>Tuple containing the result of the operation (success/failure) and an explanatory message.</returns>
        public async Task<(bool Success, string Message)> ExecuteJobsAsync(List<int> jobIndexes)
        {
            // Check if the business software is running
            if (_smDetector.IsRunning)
            {
                // Cancel execution and return an explanatory message
                return (false, "Cannot execute backup jobs: business software is running");
            }

            var tasks = new List<Task<bool>>();

            foreach (var index in jobIndexes)
            {
                // Check if the index is valid
                if (index >= 0 && index < backupJobs.Count)
                {
                    var job = backupJobs[index];
                    int jobIndex = index;  // Capture variable for lambda

                    // Create a new CancellationTokenSource for this job
                    var tokenSource = new CancellationTokenSource();

                    // Store the tokenSource in the dictionary (replace the old one if it exists)
                    lock (lockObject)
                    {
                        if (_jobCancellationTokens.ContainsKey(jobIndex))
                        {
                            // If a tokenSource already exists for this job, dispose it first
                            _jobCancellationTokens[jobIndex].Dispose();
                        }
                        _jobCancellationTokens[jobIndex] = tokenSource;
                    }

                    // Configure the job with its index and a reference to the BackupManager
                    job.SetManagerInfo(jobIndex, this);

                    // Launch each job in a separate thread with its own cancellation token
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            // The job will now check itself if it should be paused
                            return job.ExecuteJob(tokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            Console.WriteLine($"Job '{job.Name}' (index {jobIndex}) cancelled.");
                            job.State = JobState.inactive; // Reset job state
                            job.Progress = 0; // Reset progress
                            return false;
                        }
                        finally
                        {
                            // Clean up the tokenSource from the dictionary when the job is finished
                            lock (lockObject)
                            {
                                if (_jobCancellationTokens.ContainsKey(jobIndex))
                                {
                                    _jobCancellationTokens.Remove(jobIndex);
                                }
                            }
                        }
                    }, tokenSource.Token));
                }
            }

            if (tasks.Count > 0)
            {
                try
                {
                    var results = await Task.WhenAll(tasks);
                    bool allSucceeded = results.All(r => r);

                    if (allSucceeded)
                        return (true, "Backup jobs executed successfully");
                    else
                        return (false, "Some backup jobs failed");
                }
                catch (OperationCanceledException)
                {
                    return (false, "Backup jobs cancelled");
                }
            }
            else
            {
                return (false, "No valid job to execute");
            }
        }

        /// <summary>
        /// Creates a backup strategy instance based on the backup type.
        /// </summary>
        /// <param name="type">Backup type (Complete or Differential).</param>
        /// <returns>Instance of AbstractBackupStrategy.</returns>
        /// <exception cref="ArgumentException">Thrown if the backup type is invalid.</exception>
        private AbstractBackupStrategy CreateBackupStrategy(BackupType type)
        {
            AbstractBackupStrategy strategy;

            switch (type)
            {
                case BackupType.Complete:
                    strategy = new CompleteBackupStrategy();
                    break;
                case BackupType.Differential:
                    strategy = new DifferentialBackupStrategy();
                    break;
                default:
                    throw new ArgumentException("Invalid backup type");
            }

            return strategy;
        }

        /// <summary>
        /// Serializes the current backup jobs and settings to a JSON configuration file.
        /// </summary>
        private void SaveBackupJobsToConfig()
        {
            // Prepare configuration data
            var configData = new
            {
                Settings = new
                {
                    Language = _configManager.GetSetting("Language"),
                    LogFormat = _configManager.GetSetting("LogFormat")
                },
                BackupJobs = backupJobs.ConvertAll(job => new
                {
                    Name = job.Name,
                    Source = job.Source,
                    Destination = job.Destination,
                    Type = job.Type.ToString()
                })
            };

            // Serialize data to JSON
            string json = System.Text.Json.JsonSerializer.Serialize(configData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } // Add enum converter
            });

            // Define the configuration file path
            string configFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "config.json");

            // Write data to the file
            File.WriteAllText(configFilePath, json);
        }

        /// <summary>
        /// Method called when the business software is detected.
        /// </summary>
        public void SM_Detected()
        {
            // Check if at least one job is active (running)
            if (backupJobs.Any(job => job.State == JobState.active))
            {
                System.Windows.MessageBox.Show(
                "Backups paused",
                "Backup paused",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
                // Pause all jobs if at least one is active
                PauseBackupJobs(reason: "Business software detected");
            }
        }

        /// <summary>
        /// Method called when the business software is no longer detected.
        /// </summary>
        public void SM_Undetected()
        {
            // Only resume jobs if at least one job is paused
            if (_pausedJobIndices.Any(kvp => kvp.Value))
            {
                System.Windows.MessageBox.Show(
                "Backups resumed",
                "Backup resumed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
                // Resume all jobs that were paused
                ResumeBackupJobs();
            }
        }

        /// <summary>
        /// Updates the name of the monitored business software.
        /// </summary>
        /// <param name="newName">New name of the business software.</param>
        public void UpdatePriorityProcess(string newName)
        {
            if (_smDetector != null)
            {
                _smDetector.UpdateBusinessSoftwareName(newName);
            }
        }

        /// <summary>
        /// Immediately stops a backup job by cancelling its execution thread.
        /// </summary>
        /// <param name="index">Index of the job to stop.</param>
        /// <returns>True if the job was stopped, False if the index is invalid or the job is not running.</returns>
        public bool KillBackupJob(int index)
        {
            lock (lockObject)
            {
                // Check if the index is valid
                if (index < 0 || index >= backupJobs.Count)
                    return false;

                var job = backupJobs[index];

                // If a tokenSource exists for this job, cancel it
                if (_jobCancellationTokens.TryGetValue(index, out var tokenSource))
                {
                    try
                    {
                        // Cancel the token to interrupt the thread
                        tokenSource.Cancel();

                        // If the job was paused, resume it so it can process the cancellation
                        if (IsJobPaused(index))
                        {
                            ResumeBackupJobs(new[] { index });
                        }

                        Console.WriteLine($"Job '{job.Name}' (index {index}) killed.");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error stopping job '{job.Name}': {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    // If no tokenSource exists, the job is probably not running
                    // We can still reset its state if necessary
                    if (job.State != JobState.inactive)
                    {
                        job.State = JobState.inactive;
                        job.Progress = 0;

                        Console.WriteLine($"Job '{job.Name}' (index {index}) reset.");
                    }
                    return false;
                }
            }
        }

        /// <summary>
        /// Data Transfer Object for exposing job status remotely.
        /// </summary>
        public class BackupJobStatusDto : INotifyPropertyChanged
        {
            private int _index;
            private string _name;
            private string _state;
            private int _progress;
            private bool _isSelected;

            /// <summary>
            /// Index of the job in the manager's list.
            /// </summary>
            public int Index { get => _index; set { if (_index != value) { _index = value; OnPropertyChanged(nameof(Index)); } } }
            /// <summary>
            /// Name of the backup job.
            /// </summary>
            public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } } }
            /// <summary>
            /// State of the backup job as a string.
            /// </summary>
            public string State { get => _state; set { if (_state != value) { _state = value; OnPropertyChanged(nameof(State)); } } }
            /// <summary>
            /// Progress percentage of the backup job.
            /// </summary>
            public int Progress { get => _progress; set { if (_progress != value) { _progress = value; OnPropertyChanged(nameof(Progress)); } } }
            /// <summary>
            /// Indicates if the job is selected in the UI.
            /// </summary>
            public bool IsSelected { get => _isSelected; set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } } }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Returns a list of job status DTOs for remote monitoring.
        /// </summary>
        public List<BackupJobStatusDto> GetJobStatuses()
        {
            lock (lockObject)
            {
                var result = new List<BackupJobStatusDto>();
                for (int i = 0; i < backupJobs.Count; i++)
                {
                    var job = backupJobs[i];
                    result.Add(new BackupJobStatusDto
                    {
                        Index = i,
                        Name = job.Name,
                        State = job.State.ToString(),
                        Progress = job.Progress
                    });
                }
                return result;
            }
        }

        /// <summary>
        /// Attaches a global observer to all jobs and to the manager.
        /// </summary>
        /// <param name="observer">Observer to attach.</param>
        public void AttachObserver(IObserver observer)
        {
            lock (lockObject)
            {
                if (!_globalObservers.Contains(observer))
                {
                    _globalObservers.Add(observer);
                    foreach (var job in backupJobs)
                        job.AttachObserver(observer);
                }
            }
        }

        /// <summary>
        /// Detaches a global observer from the manager.
        /// </summary>
        /// <param name="observer">Observer to detach.</param>
        public void DetachObserver(IObserver observer)
        {
            lock (lockObject)
            {
                if (_globalObservers.Contains(observer))
                {
                    _globalObservers.Remove(observer);
                }
            }
        }
    }
}