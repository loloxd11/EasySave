using EasySave.Commands;
using EasySave.Models;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Navigation;

namespace EasySave.ViewModels
{
    /// <summary>
    /// ViewModel for managing backup jobs in the EasySave application.
    /// Handles user input, validation, and interaction with the BackupManager.
    /// </summary>
    public class JobsViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Event triggered when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Instance of the LanguageViewModel for managing application language.
        /// </summary>
        public LanguageViewModel LanguageViewModel { get; }

        /// <summary>
        /// Notifies listeners of property changes.
        /// </summary>
        /// <param name="name">The name of the property that changed.</param>
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Singleton instance of the BackupManager for managing backup jobs.
        /// </summary>
        private BackupManager backupManager = BackupManager.GetInstance();

        /// <summary>
        /// Event triggered to navigate back to the main menu.
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
        /// Source path for the backup job.
        /// </summary>
        public string SourcePath
        {
            get => sourcePath;
            set { sourcePath = value; OnPropertyChanged(); }
        }

        private string targetPath;
        /// <summary>
        /// Target path for the backup job.
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
        /// Command to validate and add a new backup job.
        /// </summary>
        public ICommand ValidateCommand { get; }

        /// <summary>
        /// Command to cancel the operation and navigate back to the main menu.
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Constructor for the JobsViewModel.
        /// Initializes commands and sets up the LanguageViewModel instance.
        /// </summary>
        public JobsViewModel()
        {
            LanguageViewModel = LanguageViewModel.Instance;

            ValidateCommand = new RelayCommand(() =>
            {
                // Validate the job name
                if (string.IsNullOrWhiteSpace(JobName))
                {
                    System.Windows.MessageBox.Show(
                        "The job name cannot be empty.",
                        "Missing Field",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Validate the source path
                if (string.IsNullOrWhiteSpace(SourcePath))
                {
                    System.Windows.MessageBox.Show(
                        "The source path cannot be empty.",
                        "Missing Field",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Validate the target path
                if (string.IsNullOrWhiteSpace(TargetPath))
                {
                    System.Windows.MessageBox.Show(
                        "The target path cannot be empty.",
                        "Missing Field",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Check if the source directory exists
                if (!Directory.Exists(SourcePath))
                {
                    System.Windows.MessageBox.Show(
                        "The source directory does not exist.",
                        "Invalid Directory",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return;
                }

                // Validate the target path
                try
                {
                    // Check if the target directory exists, otherwise create it
                    if (!Directory.Exists(TargetPath))
                    {
                        // Validate the path before attempting to create the directory
                        Path.GetFullPath(TargetPath);

                        // Optionally, create the directory
                        // Directory.CreateDirectory(TargetPath);
                    }
                    else
                    {
                        Directory.CreateDirectory(TargetPath);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"The target path is invalid: {ex.Message}",
                        "Invalid Path",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return;
                }

                // Add the backup job if all validations pass
                bool success = backupManager.AddBackupJob(JobName, SourcePath, TargetPath, SelectedBackupType);

                if (success)
                {
                    // Trigger the event to navigate back to the main menu
                    NavigateToMainMenu?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Error adding the job. A job with this name may already exist.",
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            });

            CancelCommand = new RelayCommand(() =>
            {
                // Navigate back to the main menu without saving
                NavigateToMainMenu?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}
