using EasySave.Commands;
using EasySave.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Threading;
using System.Linq;
using System;
using System.Net.Sockets;
using System.IO;
using MessageBox = System.Windows.MessageBox;
using System.Windows;

namespace EasySave.ViewModels
{
    /// <summary>
    /// ViewModel for the remote console, allowing monitoring and control of backup jobs on a remote server.
    /// Handles connection, job status updates, and job commands (start, pause, resume, stop).
    /// </summary>
    public class RemoteConsoleViewModel : INotifyPropertyChanged
    {
        private string _host = "127.0.0.1";
        private int _port = 9000;
        private RemoteConsoleClient _client;
        private bool _isConnected;
        private ObservableCollection<BackupManager.BackupJobStatusDto> _jobs = new();
        private BackupManager.BackupJobStatusDto _selectedJob;
        private CancellationTokenSource _listenCts;
        private ObservableCollection<int> _selectedJobIndices = new ObservableCollection<int>();
        private bool _areAllJobsSelected;
        private bool _manualDisconnect = false;

        /// <summary>
        /// Singleton instance for language management.
        /// </summary>
        public LanguageViewModel LanguageViewModel { get; } = LanguageViewModel.Instance;

        /// <summary>
        /// Host address of the remote server.
        /// </summary>
        public string Host { get => _host; set { _host = value; OnPropertyChanged(); } }
        /// <summary>
        /// Port number of the remote server.
        /// </summary>
        public int Port { get => _port; set { _port = value; OnPropertyChanged(); } }
        /// <summary>
        /// List of backup jobs retrieved from the remote server.
        /// </summary>
        public ObservableCollection<BackupManager.BackupJobStatusDto> Jobs { get => _jobs; set { _jobs = value; OnPropertyChanged(); } }
        /// <summary>
        /// Currently selected backup job.
        /// </summary>
        public BackupManager.BackupJobStatusDto SelectedJob { get => _selectedJob; set { _selectedJob = value; OnPropertyChanged(); } }
        /// <summary>
        /// Indices of selected jobs (for multi-selection).
        /// </summary>
        public ObservableCollection<int> SelectedJobIndices { get => _selectedJobIndices; set { _selectedJobIndices = value; OnPropertyChanged(); } }
        /// <summary>
        /// Indicates if all jobs are selected.
        /// </summary>
        public bool AreAllJobsSelected
        {
            get => _areAllJobsSelected;
            set { _areAllJobsSelected = value; SelectAllJobs(value); OnPropertyChanged(); }
        }

        // Commands for UI binding
        public ICommand ConnectCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExecuteJobsCommand { get; }
        public ICommand PauseAllJobsCommand { get; }
        public ICommand ResumeAllJobsCommand { get; }
        public ICommand PauseJobCommand { get; }
        public ICommand ResumeJobCommand { get; }
        public ICommand StopJobCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand ToggleJobSelectionCommand { get; }

        // Events for navigation
        public event Action BackRequested;
        public event Action RequestBackToMainView;

        /// <summary>
        /// Constructor. Initializes commands and sets up the ViewModel.
        /// </summary>
        public RemoteConsoleViewModel()
        {
            ConnectCommand = new Commands.AsyncRelayCommand(async () => await ConnectAsync());
            RefreshCommand = new RelayCommand(async () => await RefreshJobsAsync(), () => _isConnected);
            ExecuteJobsCommand = new RelayCommand(async () => await ExecuteSelectedJobsAsync(), () => _isConnected && SelectedJobIndices.Count > 0);
            PauseAllJobsCommand = new RelayCommand(async () => await PauseAllJobsAsync(), () => _isConnected);
            ResumeAllJobsCommand = new RelayCommand(async () => await ResumeAllJobsAsync(), () => _isConnected);
            PauseJobCommand = new RelayCommand<int>(async idx => await PauseJobAsync(idx), idx => _isConnected);
            ResumeJobCommand = new RelayCommand<int>(async idx => await ResumeJobAsync(idx), idx => _isConnected);
            StopJobCommand = new RelayCommand<int>(async idx => await StopJobAsync(idx), idx => _isConnected);
            BackCommand = new RelayCommand(Back);
            ToggleJobSelectionCommand = new RelayCommand<int>(ToggleJobSelection);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="prop">Name of the property.</param>
        private void OnPropertyChanged([CallerMemberName] string prop = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        /// <summary>
        /// Handles the back navigation, disconnecting from the server and returning to the main view.
        /// </summary>
        private void Back()
        {
            _manualDisconnect = true;
            _client?.Disconnect();
            RequestBackToMainView?.Invoke();
        }

        /// <summary>
        /// Subscribes to client events (e.g., disconnection).
        /// </summary>
        private void SubscribeClientEvents()
        {
            if (_client != null)
            {
                _client.OnDisconnected += OnClientDisconnected;
            }
        }

        /// <summary>
        /// Handles unexpected client disconnection.
        /// </summary>
        private void OnClientDisconnected()
        {
            if (!_manualDisconnect)
            {
                ShowError(LanguageViewModel["ConnectionLost"]);
            }
            RequestBackToMainView?.Invoke();
        }

        /// <summary>
        /// Connects to the remote server asynchronously.
        /// </summary>
        private async Task ConnectAsync()
        {
            try
            {
                // Check port validity
                if (Port < 1 || Port > 65535)
                {
                    ShowError("Port invalide. Veuillez entrer un port entre 1 et 65535.");
                    return;
                }
                _client = new RemoteConsoleClient(Host, Port);
                await _client.ConnectAsync();
                SubscribeClientEvents();
                _isConnected = true;
                await RefreshJobsAsync();
                StartListeningPush();
            }
            catch (SocketException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur Socket connexion remote: {ex.Message}\n{ex.StackTrace}");
                ShowError($"Impossible de se connecter au serveur distant (Socket).\n{ex.Message}");
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur IO connexion remote: {ex.Message}\n{ex.StackTrace}");
                ShowError($"Erreur d'entrée/sortie lors de la connexion au serveur distant.\n{ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur connexion remote: {ex.Message}\n{ex.StackTrace}");
                ShowError(LanguageViewModel["ConnectionFailed"] + "\n" + ex.Message);
                RequestBackToMainView?.Invoke();
            }
        }

        /// <summary>
        /// Displays an error message to the user.
        /// </summary>
        /// <param name="message">Error message to display.</param>
        private void ShowError(string message)
        {
            // You can replace this with an event, binding, or MessageBox depending on the UI
            MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Refreshes the list of jobs from the remote server asynchronously.
        /// </summary>
        private async Task RefreshJobsAsync()
        {
            if (_client == null) return;
            try
            {
                var jobs = await _client.GetJobStatusesAsync();
                UpdateJobsInPlace(jobs);
            }
            catch
            {
                System.Windows.MessageBox.Show(LanguageViewModel["RemoteJobsError"] ?? "Erreur lors de la récupération des jobs.");
            }
        }

        /// <summary>
        /// Executes all selected jobs asynchronously.
        /// </summary>
        private async Task ExecuteSelectedJobsAsync()
        {
            if (_client == null || SelectedJobIndices.Count == 0) return;
            foreach (var idx in SelectedJobIndices)
            {
                await _client.StartJobAsync(idx); // TODO: improve for batch execution
            }
            await RefreshJobsAsync();
        }

        /// <summary>
        /// Pauses all jobs asynchronously.
        /// </summary>
        private async Task PauseAllJobsAsync()
        {
            if (_client == null) return;
            await _client.PauseAllJobsAsync(); // To be implemented on client/server side
            await RefreshJobsAsync();
        }

        /// <summary>
        /// Resumes all jobs asynchronously.
        /// </summary>
        private async Task ResumeAllJobsAsync()
        {
            if (_client == null) return;
            await _client.ResumeAllJobsAsync(); // To be implemented on client/server side
            await RefreshJobsAsync();
        }

        /// <summary>
        /// Pauses a specific job asynchronously.
        /// </summary>
        /// <param name="idx">Index of the job to pause.</param>
        private async Task PauseJobAsync(int idx)
        {
            if (_client == null) return;
            await _client.PauseJobAsync(idx); // To be implemented on client/server side
            await RefreshJobsAsync();
        }

        /// <summary>
        /// Resumes a specific job asynchronously.
        /// </summary>
        /// <param name="idx">Index of the job to resume.</param>
        private async Task ResumeJobAsync(int idx)
        {
            if (_client == null) return;
            await _client.ResumeJobAsync(idx); // To be implemented on client/server side
            await RefreshJobsAsync();
        }

        /// <summary>
        /// Stops a specific job asynchronously.
        /// </summary>
        /// <param name="idx">Index of the job to stop.</param>
        private async Task StopJobAsync(int idx)
        {
            if (_client == null) return;
            await _client.StopJobAsync(idx); // To be implemented on client/server side
            await RefreshJobsAsync();
        }

        /// <summary>
        /// Starts listening for real-time job updates from the server.
        /// </summary>
        private void StartListeningPush()
        {
            _listenCts?.Cancel();
            _listenCts = new CancellationTokenSource();
            _client.JobsUpdated += jobs =>
            {
                App.Current.Dispatcher.Invoke(() => UpdateJobsInPlace(jobs));
            };
            _client.StartListening(_listenCts.Token);
        }

        /// <summary>
        /// Updates the Jobs collection in place, preserving selection and UI state.
        /// </summary>
        /// <param name="newJobs">New list of job statuses.</param>
        private void UpdateJobsInPlace(System.Collections.Generic.List<BackupManager.BackupJobStatusDto> newJobs)
        {
            bool mustReplace = newJobs.Count != _jobs.Count || !_jobs.Select(j => j.Index).SequenceEqual(newJobs.Select(j => j.Index));
            if (mustReplace)
            {
                Jobs = new ObservableCollection<BackupManager.BackupJobStatusDto>(newJobs);
            }
            else
            {
                for (int i = 0; i < newJobs.Count; i++)
                {
                    var newJob = newJobs[i];
                    var existing = _jobs.FirstOrDefault(j => j.Index == newJob.Index);
                    if (existing != null)
                    {
                        existing.Name = newJob.Name;
                        existing.State = newJob.State;
                        existing.Progress = newJob.Progress;
                    }
                }
            }
            // Synchronize visual selection after each update
            foreach (var job in Jobs)
            {
                job.IsSelected = SelectedJobIndices.Contains(job.Index);
            }
        }

        /// <summary>
        /// Toggles the selection state of a job (for multi-selection with checkboxes).
        /// </summary>
        /// <param name="index">Index of the job to toggle.</param>
        public void ToggleJobSelection(int index)
        {
            if (_selectedJobIndices.Contains(index))
                _selectedJobIndices.Remove(index);
            else
                _selectedJobIndices.Add(index);
            // Update the IsSelected property of the affected job
            var job = Jobs.FirstOrDefault(j => j.Index == index);
            if (job != null)
                job.IsSelected = _selectedJobIndices.Contains(index);
            OnPropertyChanged(nameof(SelectedJobIndices));
            OnPropertyChanged(nameof(Jobs));
            UpdateAllJobsSelectedState();
        }

        /// <summary>
        /// Selects or deselects all jobs.
        /// </summary>
        /// <param name="select">True to select all, false to deselect all.</param>
        public void SelectAllJobs(bool select)
        {
            _selectedJobIndices.Clear();
            if (select)
            {
                foreach (var job in Jobs)
                    _selectedJobIndices.Add(job.Index);
            }
            // Synchronize the IsSelected property of all jobs
            foreach (var job in Jobs)
            {
                job.IsSelected = _selectedJobIndices.Contains(job.Index);
            }
            OnPropertyChanged(nameof(SelectedJobIndices));
            var temp = Jobs;
            Jobs = null;
            Jobs = temp;
        }

        /// <summary>
        /// Updates the AreAllJobsSelected property based on the current selection.
        /// </summary>
        private void UpdateAllJobsSelectedState()
        {
            bool allSelected = Jobs.Count > 0 && _selectedJobIndices.Count == Jobs.Count;
            if (_areAllJobsSelected != allSelected)
            {
                _areAllJobsSelected = allSelected;
                OnPropertyChanged(nameof(AreAllJobsSelected));
            }
        }
    }
}
