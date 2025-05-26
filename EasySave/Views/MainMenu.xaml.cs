using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EasySave.Models;
using EasySave.ViewModels;
using EasySave.Views;
using System;
using System.Linq;
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
        /// Placeholder for deleting a backup job.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void DeleteBackupJob_Click(object sender, RoutedEventArgs e)
        {
            // Logic for deleting a backup job (to be implemented)
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
                    if (!_viewModel.IsJobSelected(index))
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
                    if (_viewModel.IsJobSelected(index))
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
                await _viewModel.ExecuteSelectedJobsAsync();
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
    }
}
