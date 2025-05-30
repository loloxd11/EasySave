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
    /// <summary>
    /// Main window for the EasySave application.
    /// Handles UI logic for backup job management, state updates, and navigation.
    /// Implements the IStateObserver interface to receive job state notifications.
    /// </summary>
    public partial class MainWindow : Window, IStateObserver
    {
        // Main ViewModel instance for the main menu
        private MainMenuViewModel _viewModel;
        private StateManager _stateManager;

        public static object SharedLanguageViewModel { get; internal set; }

        private RemoteConsoleServer _remoteServer;
        private bool _isRemoteServerActive = false;
        private const int RemoteServerPort = 9000;
        private RemoteConsoleViewControl _remoteConsoleViewControl; // Reference to avoid multiple openings

        /// <summary>
        /// Constructor for MainWindow.
        /// Initializes the DataContext with the MainMenuViewModel.
        /// Subscribes to state updates.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainMenuViewModel();
            DataContext = _viewModel;

            // Subscribe to state updates
            _stateManager = StateManager.GetInstance();
            _stateManager.AttachObserver(this);
        }

        /// <summary>
        /// Updates the state and progress of backup jobs in the UI.
        /// Called by the state manager when a job changes state.
        /// </summary>
        /// <param name="action">Action performed on the job (start, complete, error, pause, resume, etc.)</param>
        /// <param name="name">Name of the backup job</param>
        /// <param name="type">Type of backup</param>
        /// <param name="state">Current state of the job</param>
        /// <param name="sourcePath">Source directory path</param>
        /// <param name="targetPath">Target directory path</param>
        /// <param name="totalFiles">Total number of files</param>
        /// <param name="totalSize">Total size in bytes</param>
        /// <param name="progression">Progress percentage</param>
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
                        UpdateJobState(name, JobState.active, 0);
                    }
                    else if (action == "complete")
                    {
                        UpdateJobState(name, JobState.completed, 100);
                    }
                    else if (action == "error")
                    {
                        UpdateJobState(name, JobState.error);
                    }
                    if (action == "pause" || action == "resume")
                    {
                        RefreshJobsView();
                    }
                    else
                    {
                        UpdateJobState(name, state, progression);
                    }
                }
            });
        }

        /// <summary>
        /// Updates the state of a job in the UI.
        /// Optionally updates the progress value.
        /// </summary>
        /// <param name="name">Job name</param>
        /// <param name="newState">New state of the job</param>
        /// <param name="progressValue">Optional progress value (keeps current if not specified)</param>
        public void UpdateJobState(string name, JobState newState, int? progressValue = null)
        {
            Dispatcher.Invoke(() =>
            {
                var job = _viewModel.BackupJobs.FirstOrDefault(j => j.Name == name);
                if (job != null)
                {
                    job.State = newState;

                    // Only update progress if a value is specified
                    if (progressValue.HasValue)
                    {
                        job.Progress = progressValue.Value;
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

            // Reattach the observer after reloading the view
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
        /// Refreshes the display of jobs in the UI.
        /// Calls the ViewModel to refresh its job list and updates the DataGrid if present.
        /// </summary>
        public void RefreshJobsView()
        {
            Dispatcher.Invoke(() =>
            {
                // Ask the ViewModel to refresh its job list
                _viewModel.RefreshBackupJobs();

                // If you have a specific control to display jobs (like a DataGrid), force its refresh
                if (BackupJobsGrid != null) // Replace with your DataGrid's name
                {
                    BackupJobsGrid.Items.Refresh();
                }
            });
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
                    if (job.IsSelected)
                    {
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
                    if (!job.IsSelected)
                    {
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
                // Execute the selected jobs and get the result
                var result = await _viewModel.ExecuteSelectedJobsAsync();
                // Show the appropriate message based on the result
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
        /// Resumes the execution of a paused job.
        /// Only resumes if the job is currently paused.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var job = (BackupJob)button.DataContext;
            int index = _viewModel.BackupJobs.IndexOf(job);

            if (index >= 0)
            {
                BackupManager manager = BackupManager.GetInstance();

                // Check if the job is paused before resuming
                if (manager.IsJobPaused(index))
                {
                    // Resume only this job
                    manager.ResumeBackupJobs(new[] { index });
                }
                // If the job is not paused, the button has no effect
            }
        }

        /// <summary>
        /// Pauses or resumes the selected job in the row.
        /// Only pauses if the job is not already paused.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var job = (BackupJob)button.DataContext;
            int index = _viewModel.BackupJobs.IndexOf(job);

            if (index >= 0)
            {
                BackupManager manager = BackupManager.GetInstance();

                // Check if the job is already paused
                if (!manager.IsJobPaused(index))
                {
                    // Pause the job
                    manager.PauseBackupJobs(new[] { index }, "Pause manuelle");
                }
            }
        }

        /// <summary>
        /// Pauses all active jobs.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void PauseAllJobs_Click(object sender, RoutedEventArgs e)
        {
            BackupManager manager = BackupManager.GetInstance();
            manager.PauseBackupJobs(reason: "Pause manuelle globale");
        }

        /// <summary>
        /// Resumes all paused jobs.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void ResumeAllJobs_Click(object sender, RoutedEventArgs e)
        {
            BackupManager manager = BackupManager.GetInstance();
            manager.ResumeBackupJobs();
        }

        /// <summary>
        /// Immediately stops a backup job by killing its execution thread.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var job = (BackupJob)button.DataContext;
            int index = _viewModel.BackupJobs.IndexOf(job);

            if (index >= 0)
            {
                BackupManager manager = BackupManager.GetInstance();

                // Kill the job's execution thread
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
        /// Opens the remote console (remote client) in the main window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void OpenRemoteConsole_Click(object sender, RoutedEventArgs e)
        {
            if (_remoteConsoleViewControl == null)
            {
                _remoteConsoleViewControl = new EasySave.Views.RemoteConsoleViewControl();
            }
            this.Content = _remoteConsoleViewControl;
        }

        /// <summary>
        /// Updates the remote server status display in the UI.
        /// Changes the text and color based on the server's active state.
        /// </summary>
        /// <param name="isActive">True if the server is active, false otherwise.</param>
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

        /// <summary>
        /// Handles the click event for the remote server button.
        /// Starts or stops the remote server and updates the UI accordingly.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
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
