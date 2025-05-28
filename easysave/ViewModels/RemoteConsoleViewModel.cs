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

namespace EasySave.ViewModels
{
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

        public LanguageViewModel LanguageViewModel { get; } = LanguageViewModel.Instance;

        public string Host { get => _host; set { _host = value; OnPropertyChanged(); } }
        public int Port { get => _port; set { _port = value; OnPropertyChanged(); } }
        public ObservableCollection<BackupManager.BackupJobStatusDto> Jobs { get => _jobs; set { _jobs = value; OnPropertyChanged(); } }
        public BackupManager.BackupJobStatusDto SelectedJob { get => _selectedJob; set { _selectedJob = value; OnPropertyChanged(); } }
        public ObservableCollection<int> SelectedJobIndices { get => _selectedJobIndices; set { _selectedJobIndices = value; OnPropertyChanged(); } }
        public bool AreAllJobsSelected
        {
            get => _areAllJobsSelected;
            set { _areAllJobsSelected = value; SelectAllJobs(value); OnPropertyChanged(); }
        }

        // Commands
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

        // Event pour navigation retour
        public event Action BackRequested;
        public event Action RequestBackToMainView;

        public RemoteConsoleViewModel()
        {
            ConnectCommand = new RelayCommand(async () => await ConnectAsync());
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
        private void OnPropertyChanged([CallerMemberName] string prop = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        private void Back()
        {
            _manualDisconnect = true;
            _client?.Disconnect();
            RequestBackToMainView?.Invoke();
        }

        private void SubscribeClientEvents()
        {
            if (_client != null)
            {
                _client.OnDisconnected += OnClientDisconnected;
            }
        }

        private void OnClientDisconnected()
        {
            if (!_manualDisconnect)
            {
                ShowError(LanguageViewModel["ConnectionLost"]);
            }
            RequestBackToMainView?.Invoke();
        }

        private async Task ConnectAsync()
        {
            try
            {
                // Vérification du port
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

        private void ShowError(string message)
        {
            // Ici, tu peux remplacer par un event, un binding, ou une MessageBox selon l'UI
            MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }

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

        private async Task ExecuteSelectedJobsAsync()
        {
            if (_client == null || SelectedJobIndices.Count == 0) return;
            foreach (var idx in SelectedJobIndices)
            {
                await _client.StartJobAsync(idx); // TODO: améliorer pour batch
            }
            await RefreshJobsAsync();
        }

        private async Task PauseAllJobsAsync()
        {
            if (_client == null) return;
            await _client.PauseAllJobsAsync(); // À implémenter côté client/serveur
            await RefreshJobsAsync();
        }

        private async Task ResumeAllJobsAsync()
        {
            if (_client == null) return;
            await _client.ResumeAllJobsAsync(); // À implémenter côté client/serveur
            await RefreshJobsAsync();
        }

        private async Task PauseJobAsync(int idx)
        {
            if (_client == null) return;
            await _client.PauseJobAsync(idx); // À implémenter côté client/serveur
            await RefreshJobsAsync();
        }

        private async Task ResumeJobAsync(int idx)
        {
            if (_client == null) return;
            await _client.ResumeJobAsync(idx); // À implémenter côté client/serveur
            await RefreshJobsAsync();
        }

        private async Task StopJobAsync(int idx)
        {
            if (_client == null) return;
            await _client.StopJobAsync(idx); // À implémenter côté client/serveur
            await RefreshJobsAsync();
        }

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
            // Synchroniser la sélection visuelle après chaque update
            foreach (var job in Jobs)
            {
                job.IsSelected = SelectedJobIndices.Contains(job.Index);
            }
        }

        // Sélection multiple (checkboxes)
        public void ToggleJobSelection(int index)
        {
            if (_selectedJobIndices.Contains(index))
                _selectedJobIndices.Remove(index);
            else
                _selectedJobIndices.Add(index);
            // Mettre à jour la propriété IsSelected du job concerné
            var job = Jobs.FirstOrDefault(j => j.Index == index);
            if (job != null)
                job.IsSelected = _selectedJobIndices.Contains(index);
            OnPropertyChanged(nameof(SelectedJobIndices));
            OnPropertyChanged(nameof(Jobs));
            UpdateAllJobsSelectedState();
        }

        public void SelectAllJobs(bool select)
        {
            _selectedJobIndices.Clear();
            if (select)
            {
                foreach (var job in Jobs)
                    _selectedJobIndices.Add(job.Index);
            }
            // Synchroniser la propriété IsSelected de tous les jobs
            foreach (var job in Jobs)
            {
                job.IsSelected = _selectedJobIndices.Contains(job.Index);
            }
            OnPropertyChanged(nameof(SelectedJobIndices));
            var temp = Jobs;
            Jobs = null;
            Jobs = temp;
        }

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
