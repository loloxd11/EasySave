using EasySave.Commands;
using EasySave.Models;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace EasySave.ViewModels
{
    /// <summary>
    /// ViewModel for managing backup jobs (add/edit) in the EasySave application.
    /// Handles job creation, validation, and editing logic.
    /// </summary>
    public class JobsViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Event triggered when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="name">The name of the property that changed.</param>
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Singleton instance for language management.
        /// </summary>
        public LanguageViewModel LanguageViewModel { get; }

        /// <summary>
        /// Singleton instance for backup job management.
        /// </summary>
        private BackupManager backupManager = BackupManager.GetInstance();

        /// <summary>
        /// Event to navigate back to the main menu.
        /// </summary>
        public event EventHandler NavigateToMainMenu;

        private string jobName;
        /// <summary>
        /// Name of the backup job.
        /// </summary>
        public string JobName
        {
            get => jobName;
            set { jobName = value; OnPropertyChanged(); }
        }

        private string sourcePath;
        /// <summary>
        /// Source directory path for the backup job.
        /// </summary>
        public string SourcePath
        {
            get => sourcePath;
            set { sourcePath = value; OnPropertyChanged(); }
        }

        private string targetPath;
        /// <summary>
        /// Target directory path for the backup job.
        /// </summary>
        public string TargetPath
        {
            get => targetPath;
            set { targetPath = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// List of available backup types (Complete, Differential).
        /// </summary>
        public IEnumerable<BackupType> BackupTypes { get; } = Enum.GetValues(typeof(BackupType)).Cast<BackupType>();

        private BackupType selectedBackupType;
        /// <summary>
        /// Selected backup type for the job.
        /// </summary>
        public BackupType SelectedBackupType
        {
            get => selectedBackupType;
            set { selectedBackupType = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Command to validate and save the job.
        /// </summary>
        public ICommand ValidateCommand { get; }

        /// <summary>
        /// Command to cancel the operation and return to the main menu.
        /// </summary>
        public ICommand CancelCommand { get; }

        // For edit mode
        private bool isEditMode;
        /// <summary>
        /// Indicates if the ViewModel is in edit mode.
        /// </summary>
        public bool IsEditMode => isEditMode;

        private string originalJobName;
        private int editJobIndex;

        /// <summary>
        /// Default constructor for adding a new job.
        /// </summary>
        public JobsViewModel()
        {
            LanguageViewModel = LanguageViewModel.Instance;
            isEditMode = false;

            // Initialize commands for validation and cancellation
            ValidateCommand = new RelayCommand(ValidateJob);
            CancelCommand = new RelayCommand(() =>
            {
                NavigateToMainMenu?.Invoke(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// Constructor for editing an existing job.
        /// </summary>
        /// <param name="jobToEdit">The job to edit.</param>
        /// <param name="jobIndex">The index of the job in the list.</param>
        public JobsViewModel(BackupJob jobToEdit, int jobIndex)
        {
            LanguageViewModel = LanguageViewModel.Instance;
            isEditMode = true;
            editJobIndex = jobIndex;

            // Load values from the job to edit
            originalJobName = jobToEdit.Name;
            JobName = jobToEdit.Name;
            SourcePath = jobToEdit.Source;
            TargetPath = jobToEdit.Destination;
            SelectedBackupType = jobToEdit.Type;

            // Initialize commands for validation and cancellation
            ValidateCommand = new RelayCommand(ValidateJob);
            CancelCommand = new RelayCommand(() =>
            {
                NavigateToMainMenu?.Invoke(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// Validates the job fields and saves or updates the job accordingly.
        /// Shows error messages if validation fails.
        /// </summary>
        private void ValidateJob()
        {
            // Validate job name
            if (string.IsNullOrWhiteSpace(JobName))
            {
                System.Windows.MessageBox.Show(
                    LanguageViewModel["JobNameEmptyError"] ?? "The job name cannot be empty.",
                    LanguageViewModel["MissingField"] ?? "Missing Field",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Validate source path
            if (string.IsNullOrWhiteSpace(SourcePath))
            {
                System.Windows.MessageBox.Show(
                    LanguageViewModel["SourcePathEmptyError"] ?? "The source path cannot be empty.",
                    LanguageViewModel["MissingField"] ?? "Missing Field",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Validate target path
            if (string.IsNullOrWhiteSpace(TargetPath))
            {
                System.Windows.MessageBox.Show(
                    LanguageViewModel["TargetPathEmptyError"] ?? "The target path cannot be empty.",
                    LanguageViewModel["MissingField"] ?? "Missing Field",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Check if source directory exists
            if (!Directory.Exists(SourcePath))
            {
                System.Windows.MessageBox.Show(
                    LanguageViewModel["SourceDirNotExistError"] ?? "The source directory does not exist.",
                    LanguageViewModel["InvalidDirectory"] ?? "Invalid Directory",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            // Validate target path (try to get full path or create directory)
            try
            {
                if (!Directory.Exists(TargetPath))
                {
                    Path.GetFullPath(TargetPath);
                }
                else
                {
                    Directory.CreateDirectory(TargetPath);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"{LanguageViewModel["TargetPathInvalidError"] ?? "The target path is invalid:"} {ex.Message}",
                    LanguageViewModel["InvalidPath"] ?? "Invalid Path",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            bool success;

            if (isEditMode)
            {
                // Edit mode: check if job name changed and if new name already exists
                if (originalJobName != JobName)
                {
                    if (backupManager.ListBackups().Any(j => j.Name == JobName && j.Name != originalJobName))
                    {
                        System.Windows.MessageBox.Show(
                            LanguageViewModel["JobNameExistsError"] ?? "A job with this name already exists.",
                            LanguageViewModel["Error"] ?? "Error",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                        return;
                    }

                    // Remove old job and add new one at the same index
                    backupManager.RemoveBackup(editJobIndex);
                    success = backupManager.AddBackupJob(JobName, SourcePath, TargetPath, SelectedBackupType);
                }
                else
                {
                    // Update existing job with new values
                    success = backupManager.UpdateBackupJob(JobName, SourcePath, TargetPath, SelectedBackupType);
                }
            }
            else
            {
                // Add mode: add new job
                success = backupManager.AddBackupJob(JobName, SourcePath, TargetPath, SelectedBackupType);
            }

            if (success)
            {
                // If the operation is successful, navigate back to the main menu
                NavigateToMainMenu?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // Show error message if saving the job failed
                System.Windows.MessageBox.Show(
                    LanguageViewModel["JobSaveError"] ?? "Error saving the job.",
                    LanguageViewModel["Error"] ?? "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
