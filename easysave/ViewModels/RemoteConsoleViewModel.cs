using EasySave.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EasySave.Commands;
using System.Threading;

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

        public string Host { get => _host; set { _host = value; OnPropertyChanged(); } }
        public int Port { get => _port; set { _port = value; OnPropertyChanged(); } }
        public ObservableCollection<BackupManager.BackupJobStatusDto> Jobs { get => _jobs; set { _jobs = value; OnPropertyChanged(); } }
        public BackupManager.BackupJobStatusDto SelectedJob { get => _selectedJob; set { _selectedJob = value; OnPropertyChanged(); } }
        public ICommand ConnectCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand StartJobCommand { get; }

        public RemoteConsoleViewModel()
        {
            ConnectCommand = new RelayCommand(async () => await ConnectAsync());
            RefreshCommand = new RelayCommand(async () => await RefreshJobsAsync(), () => _isConnected);
            StartJobCommand = new RelayCommand(async () => await StartJobAsync(), () => _isConnected && SelectedJob != null);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        private async Task ConnectAsync()
        {
            try
            {
                _client = new RemoteConsoleClient(Host, Port);
                await _client.ConnectAsync();
                _isConnected = true;
                await RefreshJobsAsync();
                StartListeningPush();
            }
            catch
            {
                System.Windows.MessageBox.Show("Connexion impossible au serveur distant.");
            }
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
                System.Windows.MessageBox.Show("Erreur lors de la récupération des jobs.");
            }
        }

        private async Task StartJobAsync()
        {
            if (_client == null || SelectedJob == null) return;
            var (success, message) = await _client.StartJobAsync(SelectedJob.Index);
            System.Windows.MessageBox.Show(message, success ? "Succès" : "Erreur");
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
            // Si la liste reçue est différente en nombre ou en contenu, on remplace toute la collection
            bool mustReplace = newJobs.Count != _jobs.Count || !_jobs.Select(j => j.Index).SequenceEqual(newJobs.Select(j => j.Index));
            if (mustReplace)
            {
                Jobs = new ObservableCollection<BackupManager.BackupJobStatusDto>(newJobs);
                return;
            }
            // Sinon, on met à jour les objets existants
            foreach (var newJob in newJobs)
            {
                var existing = _jobs.FirstOrDefault(j => j.Index == newJob.Index);
                if (existing != null)
                {
                    existing.Name = newJob.Name;
                    existing.State = newJob.State;
                    existing.Progress = newJob.Progress;
                }
            }
        }
    }
}
