using EasySave.Models;
using EasySave.ViewModels;
using EasySave.Views;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using MessageBox = System.Windows.MessageBox;

namespace EasySave
{
    public partial class MainWindow : Window, IStateObserver
    {
        // Main ViewModel instance for the main menu
        private MainMenuViewModel _viewModel;
        private StateManager _stateManager;

        public static object SharedLanguageViewModel { get; internal set; }

        private RemoteConsoleServer _remoteServer;
        private bool _isRemoteServerActive = false;
        private const int RemoteServerPort = 9000;
        private RemoteConsoleViewControl _remoteConsoleViewControl; // Ajout d'une référence pour éviter l'ouverture multiple

        /// <summary>
        /// Constructor for MainWindow.
        /// Initializes the DataContext with the MainMenuViewModel.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainMenuViewModel();
            DataContext = _viewModel;

            // S'abonner aux mises à jour d'état
            _stateManager = StateManager.GetInstance();
            _stateManager.AttachObserver(this);
        }

        /// <summary>
        /// Met à jour l'état et la progression des jobs dans l'interface utilisateur
        /// </summary>
        public void Update(string action, string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, int progression)
        {
            Dispatcher.Invoke(() =>
            {
                var job = _viewModel.BackupJobs.FirstOrDefault(j => j.Name == name);

                if (job != null)
                {
                    if (action == "start")
                    {
                        job.State = JobState.active;
                        job.Progress = 0;
                    }
                    else if (action == "complete")
                    {
                        job.State = JobState.completed;
                        job.Progress = 100;
                    }
                    else if (action == "error")
                    {
                        job.State = JobState.error;
                    }
                    else if (action == "update" || action == "transfer" || action == "processing")
                    {
                        job.State = state;
                        job.Progress = progression;
                    }
                }
            });
        }

        /// <summary>
        /// Resets the current view to the MainMenu.
        /// Clears the current content, reinitializes the ViewModel, and reloads the MainMenu XAML.
        /// </summary>
        public void ResetToMainMenu()
        {
            // Clear the current content
            Content = null;

            // Reinitialize the ViewModel
            _viewModel = new MainMenuViewModel();
            DataContext = _viewModel;

            // Reset the name scope for the window
            NameScope.SetNameScope(this, new NameScope());

            // Load the MainMenu XAML directly
            System.Windows.Application.LoadComponent(
                this,
                new Uri("/EasySave;component/Views/MainMenu.xaml", UriKind.Relative)
            );

            // Réattacher l'observateur après avoir rechargé la vue
            _stateManager.AttachObserver(this);
        }

        /// <summary>
        /// Navigates to the Add Backup Job page.
        /// Creates a new Frame and sets its content to the Jobs page.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void AddBackupJob_Click(object sender, RoutedEventArgs e)
        {
            // Create a new frame and navigate to the Jobs page
            Frame jobsFrame = new Frame();
            Jobs jobsPage = new Jobs();
            jobsFrame.Content = jobsPage;
            Content = jobsFrame;
        }

        /// <summary>
        /// Navigates to the Edit Backup Job page.
        /// Creates a new Frame and sets its content to the Jobs page.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void EditBackupJob_Click(object sender, RoutedEventArgs e)
        {
            // Create a new frame and navigate to the Jobs page
            Frame jobsFrame = new Frame();
            Jobs jobsPage = new Jobs();
            jobsFrame.Content = jobsPage;
            Content = jobsFrame;
        }


        /// <summary>
        /// Handles the event when a job checkbox is checked.
        /// Selects the corresponding job in the ViewModel.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void JobCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var row = DataGridRow.GetRowContainingElement(checkBox);
                if (row != null)
                {
                    int index = row.GetIndex();
                    var job = _viewModel.BackupJobs[index];
                    if (!job.IsSelected)
                    {
                        job.IsSelected = true;
                        _viewModel.ToggleJobSelection(index);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the event when a job checkbox is unchecked.
        /// Deselects the corresponding job in the ViewModel.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void JobCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var row = DataGridRow.GetRowContainingElement(checkBox);
                if (row != null)
                {
                    int index = row.GetIndex();
                    var job = _viewModel.BackupJobs[index];
                    if (job.IsSelected)
                    {
                        job.IsSelected = false;
                        _viewModel.ToggleJobSelection(index);
                    }
                }
            }
        }

        /// <summary>
        /// Executes the selected backup jobs.
        /// Disables the execute button during execution and shows a message box on completion or error.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private async void ExecuteBackupJob_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Exécuter les jobs sélectionnés et récupérer le résultat
                var result = await _viewModel.ExecuteSelectedJobsAsync();
                // Afficher le message approprié en fonction du résultat
                MessageBoxImage icon = result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning;
                MessageBox.Show(
                    result.Message,
                    result.Success ? "Succès" : "Attention",
                    MessageBoxButton.OK,
                    icon);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while executing the jobs: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens the settings page.
        /// Creates a new Frame and sets its content to the Settings page.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a new frame and navigate to the Settings page
            Frame settingsFrame = new Frame();
            SettingsView settingsPage = new SettingsView();
            settingsFrame.Content = settingsPage;
            Content = settingsFrame;
        }

        /// <summary>
        /// Reprend l'exécution d'un job en pause
        /// </summary>
        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var job = (BackupJob)button.DataContext;
            int index = _viewModel.BackupJobs.IndexOf(job);

            if (index >= 0)
            {
                BackupManager manager = BackupManager.GetInstance();

                // Vérifier si le job est en pause avant de reprendre
                if (manager.IsJobPaused(index))
                {
                    // Reprendre uniquement ce job
                    manager.ResumeBackupJobs(new[] { index });
                }
                // Si le job n'est pas en pause, le bouton n'a pas d'effet
            }
        }

        /// <summary>
        /// Met en pause ou reprend le job sélectionné dans la ligne
        /// </summary>
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var job = (BackupJob)button.DataContext;
            int index = _viewModel.BackupJobs.IndexOf(job);

            if (index >= 0)
            {
                BackupManager manager = BackupManager.GetInstance();

                // Vérifier si le job est déjà en pause
                if (manager.IsJobPaused(index))
                {
                    // Reprendre le job
                    manager.ResumeBackupJobs(new[] { index });
                }
                else
                {
                    // Mettre le job en pause
                    manager.PauseBackupJobs(new[] { index }, "Pause manuelle");
                }
            }
        }

        /// <summary>
        /// Met en pause tous les jobs actifs
        /// </summary>
        private void PauseAllJobs_Click(object sender, RoutedEventArgs e)
        {
            BackupManager manager = BackupManager.GetInstance();
            manager.PauseBackupJobs(reason: "Pause manuelle globale");
        }

        /// <summary>
        /// Reprend tous les jobs en pause
        /// </summary>
        private void ResumeAllJobs_Click(object sender, RoutedEventArgs e)
        {
            BackupManager manager = BackupManager.GetInstance();
            manager.ResumeBackupJobs();
        }

        /// <summary>
        /// Arrête immédiatement un job de sauvegarde en tuant son thread d'exécution
        /// </summary>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var job = (BackupJob)button.DataContext;
            int index = _viewModel.BackupJobs.IndexOf(job);

            if (index >= 0)
            {
                BackupManager manager = BackupManager.GetInstance();

                // Tuer le thread d'exécution du job
                if (manager.KillBackupJob(index))
                {
                    MessageBox.Show(
                        $"Le job '{job.Name}' a été arrêté.",
                        "Job arrêté",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        /// <summary>
        /// Ouvre la console déportée (client distant) dans la fenêtre principale
        /// </summary>
        private void OpenRemoteConsole_Click(object sender, RoutedEventArgs e)
        {
            if (_remoteConsoleViewControl == null)
            {
                _remoteConsoleViewControl = new EasySave.Views.RemoteConsoleViewControl();
            }
            this.Content = _remoteConsoleViewControl;
        }

        private void UpdateRemoteServerStatus(bool isActive)
        {
            var lang = ViewModels.LanguageViewModel.Instance;
            if (RemoteServerStatus != null)
            {
                if (isActive)
                {
                    RemoteServerStatus.Text = lang["ServerStatusActive"] + $" (Port {RemoteServerPort})";
                    RemoteServerStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    RemoteServerStatus.Text = lang["ServerStatusInactive"];
                    RemoteServerStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
        }

        private void RemoteServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRemoteServerActive)
            {
                _remoteServer = new RemoteConsoleServer(RemoteServerPort);
                _remoteServer.ServerStatusChanged += UpdateRemoteServerStatus;
                _remoteServer.Start();
                _isRemoteServerActive = true;
                RemoteServerButton.Content = "Désactiver Serveur Distant";
                UpdateRemoteServerStatus(true);
            }
            else
            {
                if (_remoteServer != null)
                {
                    _remoteServer.ServerStatusChanged -= UpdateRemoteServerStatus;
                    _remoteServer.Stop();
                }
                _isRemoteServerActive = false;
                RemoteServerButton.Content = "Activer Serveur Distant";
                UpdateRemoteServerStatus(false);
            }
        }
    }
}
