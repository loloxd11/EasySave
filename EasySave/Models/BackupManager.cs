using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Linq;
using System.ComponentModel;

namespace EasySave.Models
{
    /// <summary>
    /// Singleton class responsible for managing backup jobs, their configuration, and execution.
    /// Handles adding, updating, removing, listing, and executing backup jobs.
    /// </summary>
    public class BackupManager
    {
        private static BackupManager _instance;
        private List<BackupJob> backupJobs;
        private static ConfigManager _configManager;
        private readonly object lockObject = new object();
        private SM_Detector _smDetector;
        private CancellationTokenSource _detectorTokenSource;
        private Thread _detectorThread;

        // Structure pour le suivi de la pause des jobs
        private Dictionary<int, bool> _pausedJobIndices;

        // Événements de signalisation pour la reprise
        private Dictionary<int, ManualResetEventSlim> _resumeEvents;

        // Dictionnaire pour suivre les CancellationTokenSource des jobs en cours d'exécution
        private Dictionary<int, CancellationTokenSource> _jobCancellationTokens = new Dictionary<int, CancellationTokenSource>();

        private List<IObserver> _globalObservers = new List<IObserver>();


        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// Initializes the backup jobs list.
        /// </summary>
        private BackupManager()
        {
            backupJobs = new List<BackupJob>();
            _pausedJobIndices = new Dictionary<int, bool>();
            _resumeEvents = new Dictionary<int, ManualResetEventSlim>();

            // Initialisation du détecteur de logiciel métier
            _detectorTokenSource = new CancellationTokenSource();
            _smDetector = new SM_Detector(this);

            // Création et démarrage du thread de surveillance
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
        /// Vérifie si un job spécifique est actuellement en pause
        /// </summary>
        /// <param name="index">Index du job dans la liste</param>
        /// <returns>True si le job est en pause, sinon False</returns>
        public bool IsJobPaused(int index)
        {
            lock (lockObject)
            {
                // Vérifier d'abord que l'index est valide
                if (index < 0 || index >= backupJobs.Count)
                    return false;

                return _pausedJobIndices.TryGetValue(index, out bool isPaused) && isPaused;
            }
        }

        /// <summary>
        /// Vérifie si tous les jobs sont en pause
        /// </summary>
        /// <returns>True si tous les jobs sont en pause, sinon False</returns>
        public bool AreAllJobsPaused()
        {
            lock (lockObject)
            {
                if (backupJobs.Count == 0 || _pausedJobIndices.Count == 0)
                    return false;

                // Vérifier que tous les jobs existants sont en pause
                for (int i = 0; i < backupJobs.Count; i++)
                {
                    if (!_pausedJobIndices.TryGetValue(i, out bool isPaused) || !isPaused)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Met en pause des jobs de sauvegarde spécifiques par leurs indices
        /// </summary>
        /// <param name="indices">Collection d'indices des jobs à mettre en pause (null pour tous les jobs)</param>
        /// <param name="reason">Raison de la mise en pause</param>
        public void PauseBackupJobs(IEnumerable<int> indices = null, string reason = "")
        {
            lock (lockObject)
            {
                // Si indices est null, on met en pause tous les jobs
                if (indices == null)
                {
                    indices = Enumerable.Range(0, backupJobs.Count);
                }

                foreach (var index in indices)
                {
                    // Vérifier que l'index est valide
                    if (index < 0 || index >= backupJobs.Count)
                        continue;

                    // Créer l'événement s'il n'existe pas encore
                    if (!_resumeEvents.ContainsKey(index))
                    {
                        _resumeEvents[index] = new ManualResetEventSlim(true);
                    }

                    // Mettre le job en pause
                    _pausedJobIndices[index] = true;
                    _resumeEvents[index].Reset(); // Met l'événement en état non-signalé

                    Console.WriteLine($"Job '{backupJobs[index].Name}' (indice {index}) mis en pause. Raison : {reason}");
                }
            }
        }

        /// <summary>
        /// Reprend des jobs de sauvegarde spécifiques qui sont en pause
        /// </summary>
        /// <param name="indices">Collection d'indices des jobs à reprendre (null pour tous les jobs)</param>
        public void ResumeBackupJobs(IEnumerable<int> indices = null)
        {
            lock (lockObject)
            {
                // Si indices est null, on reprend tous les jobs
                if (indices == null)
                {
                    indices = _pausedJobIndices.Keys.ToList();
                }

                foreach (var index in indices)
                {
                    // Vérifier si le job est en pause
                    if (!_pausedJobIndices.TryGetValue(index, out bool isPaused) || !isPaused)
                        continue;

                    // Vérifier que l'index est toujours valide
                    if (index >= 0 && index < backupJobs.Count)
                    {
                        // Marquer le job comme non pausé
                        _pausedJobIndices[index] = false;

                        // Signaler l'événement pour débloquer le job s'il existe
                        if (_resumeEvents.TryGetValue(index, out var resumeEvent))
                        {
                            resumeEvent.Set();
                        }

                        Console.WriteLine($"Job '{backupJobs[index].Name}' (indice {index}) repris");
                    }
                }
            }
        }

        /// <summary>
        /// Attend la reprise d'un job spécifique s'il est en pause
        /// </summary>
        /// <param name="index">Index du job</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>True si la reprise a lieu, False si l'opération est annulée</returns>
        public bool WaitForJobResume(int index, CancellationToken cancellationToken)
        {
            try
            {
                // Si l'index est invalide ou le job n'est pas en pause, retourne immédiatement true
                if (index < 0 || index >= backupJobs.Count || !IsJobPaused(index))
                    return true;

                // S'il n'y a pas d'événement pour ce job, on en crée un
                lock (lockObject)
                {
                    if (!_resumeEvents.TryGetValue(index, out var _))
                    {
                        _resumeEvents[index] = new ManualResetEventSlim(true);
                    }
                }

                // Attendre que l'événement soit signalé (reprise) ou que l'annulation soit demandée
                return _resumeEvents[index].Wait(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        /// <summary>
        /// Ajoute un nouvel événement de pause/reprise pour un nouveau job
        /// </summary>
        /// <param name="index">Index du job</param>
        private void InitializeJobPauseState(int index)
        {
            if (index >= 0)
            {
                _pausedJobIndices[index] = false;
                _resumeEvents[index] = new ManualResetEventSlim(true);
            }
        }

        /// <summary>
        /// Met à jour les indices après une suppression de job
        /// </summary>
        /// <param name="removedIndex">Index du job supprimé</param>
        private void UpdateIndicesAfterRemoval(int removedIndex)
        {
            lock (lockObject)
            {
                // Supprimer l'état et l'événement du job supprimé
                _pausedJobIndices.Remove(removedIndex);
                if (_resumeEvents.TryGetValue(removedIndex, out var eventToDispose))
                {
                    eventToDispose.Dispose();
                    _resumeEvents.Remove(removedIndex);
                }

                // Créer de nouveaux dictionnaires avec les indices mis à jour
                var newPausedJobIndices = new Dictionary<int, bool>();
                var newResumeEvents = new Dictionary<int, ManualResetEventSlim>();

                // Décaler tous les indices supérieurs à celui supprimé
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

                // Remplacer les dictionnaires
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

                // Add global observers (ex: RemoteConsoleServer)
                foreach (var obs in _globalObservers)
                    job.AttachObserver(obs);

                // Initialiser l'état de pause du nouveau job
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

                // Sauvegarder l'état de pause
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

                // Add global observers (ex: RemoteConsoleServer)
                foreach (var obs in _globalObservers)
                    job.AttachObserver(obs);

                // Restaurer l'état de pause
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

                // Mettre à jour les indices après la suppression
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
        /// <param name="jobIndexes">Liste des indices des jobs à exécuter.</param>
        /// <returns>Tuple contenant le résultat de l'opération (succès/échec) et un message explicatif</returns>
        public async Task<(bool Success, string Message)> ExecuteJobsAsync(List<int> jobIndexes)
        {
            // Vérifier si le logiciel métier est en cours d'exécution
            if (_smDetector.IsRunning)
            {
                // Annuler l'exécution et retourner un message explicatif
                return (false, "Impossible d'exécuter les jobs de sauvegarde : logiciel métier en cours d'exécution");
            }

            var tasks = new List<Task<bool>>();

            foreach (var index in jobIndexes)
            {
                // Vérifie que l'index est valide
                if (index >= 0 && index < backupJobs.Count)
                {
                    var job = backupJobs[index];
                    int jobIndex = index;  // Capture de la variable pour le lambda

                    // Créer un nouveau CancellationTokenSource pour ce job
                    var tokenSource = new CancellationTokenSource();

                    // Stocker le tokenSource dans le dictionnaire (remplacer l'ancien s'il existe)
                    lock (lockObject)
                    {
                        if (_jobCancellationTokens.ContainsKey(jobIndex))
                        {
                            // Si un tokenSource existe déjà pour ce job, le disposer d'abord
                            _jobCancellationTokens[jobIndex].Dispose();
                        }
                        _jobCancellationTokens[jobIndex] = tokenSource;
                    }

                    // Configure le job avec son index et une référence au BackupManager
                    job.SetManagerInfo(jobIndex, this);

                    // Lance chaque job dans un thread séparé avec son propre token d'annulation
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            // Le job va maintenant vérifier lui-même s'il doit être en pause
                            return job.ExecuteJob(tokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            Console.WriteLine($"Job '{job.Name}' (index {jobIndex}) annulé.");
                            job.State = JobState.inactive; // Réinitialiser l'état du job
                            job.Progress = 0; // Réinitialiser la progression
                            return false;
                        }
                        finally
                        {
                            // Nettoyer le tokenSource du dictionnaire quand le job est terminé
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
                        return (true, "Jobs de sauvegarde exécutés avec succès");
                    else
                        return (false, "Certains jobs de sauvegarde ont échoué");
                }
                catch (OperationCanceledException)
                {
                    return (false, "Jobs de sauvegarde annulés");
                }
            }
            else
            {
                return (false, "Aucun job valide à exécuter");
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
        /// Méthode appelée lorsque le logiciel métier est détecté
        /// </summary>
        public void SM_Detected()
        {
            // Vérifier si au moins un job est actif (en cours d'exécution)
            if (backupJobs.Any(job => job.State == JobState.active))
            {
                System.Windows.MessageBox.Show(
                "Sauvegardes mises en pause",
                "Sauvegarde en pause",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
                // Mettre en pause tous les jobs si au moins un est actif
                PauseBackupJobs(reason: "Logiciel métier détecté");
                Console.WriteLine("Jobs mis en pause : logiciel métier détecté");
            }
        }

        /// <summary>
        /// Méthode appelée lorsque le logiciel métier n'est plus détecté
        /// </summary>
        public void SM_Undetected()
        {
            // Ne reprendre les jobs que si au moins un job est en pause
            if (_pausedJobIndices.Any(kvp => kvp.Value))
            {
                System.Windows.MessageBox.Show(
                "Sauvegardes reprises",
                "Sauvegarde reprise",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
                // Reprendre tous les jobs qui étaient en pause
                ResumeBackupJobs();
                Console.WriteLine("Jobs repris : logiciel métier terminé");
            }
        }

        /// <summary>
        /// Met à jour le nom du logiciel métier surveillé
        /// </summary>
        /// <param name="newName">Nouveau nom du logiciel métier</param>
        public void UpdatePriorityProcess(string newName)
        {
            if (_smDetector != null)
            {
                _smDetector.UpdateBusinessSoftwareName(newName);
            }
        }

        /// <summary>
        /// Arrête immédiatement un job de sauvegarde en annulant son thread d'exécution
        /// </summary>
        /// <param name="index">Index du job à arrêter</param>
        /// <returns>True si le job a été arrêté, False si l'index est invalide ou si le job n'est pas en cours d'exécution</returns>
        public bool KillBackupJob(int index)
        {
            lock (lockObject)
            {
                // Vérifier que l'index est valide
                if (index < 0 || index >= backupJobs.Count)
                    return false;

                var job = backupJobs[index];

                // Si un tokenSource existe pour ce job, l'annuler
                if (_jobCancellationTokens.TryGetValue(index, out var tokenSource))
                {
                    try
                    {
                        // Annuler le token pour interrompre le thread
                        tokenSource.Cancel();

                        // Si le job était en pause, le reprendre pour qu'il puisse traiter l'annulation
                        if (IsJobPaused(index))
                        {
                            ResumeBackupJobs(new[] { index });
                        }

                        Console.WriteLine($"Job '{job.Name}' (indice {index}) tué.");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors de l'arrêt du job '{job.Name}': {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    // Si aucun tokenSource n'existe, le job n'est probablement pas en cours d'exécution
                    // On peut quand même réinitialiser son état si nécessaire
                    if (job.State != JobState.inactive)
                    {
                        job.State = JobState.inactive;
                        job.Progress = 0;

                        Console.WriteLine($"Job '{job.Name}' (indice {index}) réinitialisé.");
                    }
                    return false;
                }
            }
        }
        /// <summary>
        /// Data Transfer Object for exposing job status remotely
        /// </summary>
        public class BackupJobStatusDto : INotifyPropertyChanged
        {
            private int _index;
            private string _name;
            private string _state;
            private int _progress;

            public int Index { get => _index; set { if (_index != value) { _index = value; OnPropertyChanged(nameof(Index)); } } }
            public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } } }
            public string State { get => _state; set { if (_state != value) { _state = value; OnPropertyChanged(nameof(State)); } } }
            public int Progress { get => _progress; set { if (_progress != value) { _progress = value; OnPropertyChanged(nameof(Progress)); } } }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Returns a list of job status DTOs for remote monitoring
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

        public void DetachObserver(IObserver observer)
        {
            lock (lockObject)
            {
                if (_globalObservers.Contains(observer))
                {
                    _globalObservers.Remove(observer);
                    // Il n'y a pas de Detach dans BackupJob, donc il restera sur les jobs existants, mais ce n'est pas bloquant.
                }
            }
        }
    }
}