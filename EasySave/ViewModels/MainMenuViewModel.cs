using EasySave.Commands;
using EasySave.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace EasySave.ViewModels
{
    /// <summary>
    /// ViewModel for the main menu in the application.
    /// This class is not a singleton; it is instantiated per view.
    /// Implements INotifyPropertyChanged to support data binding and notify the UI of property changes.
    /// </summary>
    public class MainMenuViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Event triggered when a property value changes.
        /// Used to notify the UI of updates to bound properties.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="name">The name of the property that changed.</param>
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Singleton instance of the LanguageViewModel for language management.
        /// </summary>
        public LanguageViewModel LanguageViewModel { get; }

        private readonly BackupManager _backupManager;

        /// <summary>
        /// Command to delete selected backup jobs.
        /// </summary>
        public ICommand DeleteJobCommand { get; private set; }

        /// <summary>
        /// Command to edit the selected backup job.
        /// </summary>
        public ICommand EditJobCommand { get; private set; }

        // Observable collection for backup jobs
        private ObservableCollection<BackupJob> _backupJobs;

        /// <summary>
        /// List of backup jobs displayed in the main menu.
        /// </summary>
        public ObservableCollection<BackupJob> BackupJobs
        {
            get => _backupJobs;
            set
            {
                _backupJobs = value;
                OnPropertyChanged(nameof(BackupJobs));
            }
        }

        private BackupJob _selectedJob;

        /// <summary>
        /// The currently selected backup job.
        /// </summary>
        public BackupJob SelectedJob
        {
            get => _selectedJob;
            set
            {
                _selectedJob = value;
                OnPropertyChanged(nameof(SelectedJob));
            }
        }

        /// <summary>
        /// Constructor for MainMenuViewModel.
        /// Initializes the LanguageViewModel property with the singleton instance.
        /// Loads backup jobs and initializes commands.
        /// </summary>
        public MainMenuViewModel()
        {
            LanguageViewModel = LanguageViewModel.Instance;
            _backupManager = BackupManager.GetInstance();

            // Initialize the backup jobs collection
            LoadBackupJobs();
            DeleteJobCommand = new RelayCommand(DeleteSelectedJobs, CanDeleteJobs);
            EditJobCommand = new RelayCommand(EditSelectedJob, CanEditJob);
        }

        /// <summary>
        /// Loads the list of backup jobs from the BackupManager.
        /// </summary>
        public void LoadBackupJobs()
        {
            var jobs = _backupManager.ListBackups();
            BackupJobs = new ObservableCollection<BackupJob>(jobs);
        }

        /// <summary>
        /// Refreshes the list of backup jobs (should be called after add/remove/edit).
        /// </summary>
        public void RefreshJobsList()
        {
            LoadBackupJobs();
        }

        // Collection to store selected job indices
        private ObservableCollection<int> _selectedJobIndices = new ObservableCollection<int>();

        /// <summary>
        /// Indices of the selected backup jobs.
        /// </summary>
        public ObservableCollection<int> SelectedJobIndices
        {
            get => _selectedJobIndices;
            set
            {
                _selectedJobIndices = value;
                OnPropertyChanged(nameof(SelectedJobIndices));
            }
        }

        // Property for the "Select All" checkbox
        private bool _areAllJobsSelected;

        /// <summary>
        /// Indicates whether all jobs are selected.
        /// </summary>
        public bool AreAllJobsSelected
        {
            get => _areAllJobsSelected;
            set
            {
                _areAllJobsSelected = value;
                // Select or deselect all jobs
                SelectAllJobs(value);
                OnPropertyChanged(nameof(AreAllJobsSelected));
            }
        }

        /// <summary>
        /// Selects or deselects all jobs in the list.
        /// </summary>
        /// <param name="select">True to select all, false to deselect all.</param>
        public void SelectAllJobs(bool select)
        {
            _selectedJobIndices.Clear();

            if (select)
            {
                for (int i = 0; i < BackupJobs.Count; i++)
                {
                    _selectedJobIndices.Add(i);
                }
            }

            OnPropertyChanged(nameof(SelectedJobIndices));
            // Hack: reassigns the collection to force the DataGrid to rebind
            var temp = BackupJobs;
            BackupJobs = null;
            BackupJobs = temp;
        }

        /// <summary>
        /// Updates the "Select All" state based on individual selections.
        /// </summary>
        private void UpdateAllJobsSelectedState()
        {
            bool allSelected = BackupJobs.Count > 0 && _selectedJobIndices.Count == BackupJobs.Count;
            if (_areAllJobsSelected != allSelected)
            {
                _areAllJobsSelected = allSelected;
                OnPropertyChanged(nameof(AreAllJobsSelected));
            }
        }

        /// <summary>
        /// Toggles the selection state of a job and updates the "Select All" state.
        /// </summary>
        /// <param name="index">Index of the job to toggle.</param>
        public void ToggleJobSelection(int index)
        {
            if (_selectedJobIndices.Contains(index))
                _selectedJobIndices.Remove(index);
            else
                _selectedJobIndices.Add(index);

            OnPropertyChanged(nameof(SelectedJobIndices));
            OnPropertyChanged(nameof(BackupJobs));

            // Forces refresh of each DataGrid row
            foreach (var job in BackupJobs)
            {
                // Notifies that the fictitious "IsSelected" property has changed for each job
                OnPropertyChanged("Item[]");
            }

            UpdateAllJobsSelectedState();
        }

        /// <summary>
        /// Checks if a job is selected.
        /// </summary>
        /// <param name="index">Index of the job.</param>
        /// <returns>True if selected, false otherwise.</returns>
        public bool IsJobSelected(int index)
        {
            return _selectedJobIndices.Contains(index);
        }

        /// <summary>
        /// Executes the selected backup jobs.
        /// </summary>
        public void ExecuteSelectedJobs()
        {
            try
            {
                if (_selectedJobIndices.Count <= 0 || ConfigManager.PriorityProcessIsRunning() == true)
                {
                    throw new InvalidOperationException("Jobs Canceled, Priority Process is running");
                }
                else
                {
                    _backupManager.ExecuteBackupJob(_selectedJobIndices.ToList(), "sequential");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(!string.IsNullOrEmpty(ex.Message) ? ex.Message : "No jobs selected or jobs are already running.");
            }
        }

        /// <summary>
        /// Determines if jobs can be deleted (at least one selected).
        /// </summary>
        /// <returns>True if jobs can be deleted, false otherwise.</returns>
        private bool CanDeleteJobs()
        {
            return SelectedJobIndices.Count > 0;
        }

        /// <summary>
        /// Deletes the selected backup jobs after confirmation.
        /// </summary>
        public void DeleteSelectedJobs()
        {
            if (SelectedJobIndices.Count == 0)
                return;

            // Ask for confirmation before deletion
            MessageBoxResult result = System.Windows.MessageBox.Show(
                LanguageViewModel["DeleteJobConfirmation"],
                LanguageViewModel["DeleteConfirmationTitle"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var indicesToRemove = SelectedJobIndices.OrderByDescending(i => i).ToList();

                foreach (var index in indicesToRemove)
                {
                    _backupManager.RemoveBackup(index);
                }

                RefreshJobsList();

                SelectedJobIndices.Clear();
                UpdateAllJobsSelectedState();
            }
        }

        /// <summary>
        /// Determines if a job can be edited (a job is selected).
        /// </summary>
        /// <returns>True if a job can be edited, false otherwise.</returns>
        private bool CanEditJob()
        {
            return SelectedJob != null;
        }

        /// <summary>
        /// Opens the edit view for the selected backup job.
        /// </summary>
        public void EditSelectedJob()
        {
            if (SelectedJob == null)
                return;

            // Find the index of the selected job
            int selectedIndex = _backupJobs.IndexOf(SelectedJob);

            if (selectedIndex >= 0)
            {
                // Create a new instance of Jobs view with the job to edit
                var jobsView = new EasySave.Views.Jobs(SelectedJob, selectedIndex);

                // Access the main window
                if (App.Current.MainWindow is MainWindow mainWindow)
                {
                    // Replace the current content with the job edit view
                    mainWindow.Content = jobsView;
                    mainWindow.Title = LanguageViewModel["EditJobTitle"];
                }
            }
        }
    }
}