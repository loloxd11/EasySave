using EasySave.Commands;
using EasySave.Models;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;

namespace EasySave.ViewModels
{
    public class JobsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public LanguageViewModel LanguageViewModel { get; }
        private BackupManager backupManager = BackupManager.GetInstance();
        public event EventHandler NavigateToMainMenu;

        private string jobName;
        public string JobName
        {
            get => jobName;
            set { jobName = value; OnPropertyChanged(); }
        }

        private string sourcePath;
        public string SourcePath
        {
            get => sourcePath;
            set { sourcePath = value; OnPropertyChanged(); }
        }

        private string targetPath;
        public string TargetPath
        {
            get => targetPath;
            set { targetPath = value; OnPropertyChanged(); }
        }

        public IEnumerable<BackupType> BackupTypes { get; } = Enum.GetValues(typeof(BackupType)).Cast<BackupType>();

        private BackupType selectedBackupType;
        public BackupType SelectedBackupType
        {
            get => selectedBackupType;
            set { selectedBackupType = value; OnPropertyChanged(); }
        }

        public ICommand ValidateCommand { get; }
        public ICommand CancelCommand { get; }

        // Pour l'édition
        private bool isEditMode;
        public bool IsEditMode => isEditMode;

        private string originalJobName;
        private int editJobIndex;

        /// <summary>
        /// Constructeur standard pour l'ajout d'un nouveau job
        /// </summary>
        public JobsViewModel()
        {
            LanguageViewModel = LanguageViewModel.Instance;
            isEditMode = false;

            ValidateCommand = new RelayCommand(ValidateJob);
            CancelCommand = new RelayCommand(() =>
            {
                NavigateToMainMenu?.Invoke(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// Constructeur pour l'édition d'un job existant
        /// </summary>
        public JobsViewModel(BackupJob jobToEdit, int jobIndex)
        {
            LanguageViewModel = LanguageViewModel.Instance;
            isEditMode = true;
            editJobIndex = jobIndex;

            // Charger les valeurs du job à éditer
            originalJobName = jobToEdit.Name;
            JobName = jobToEdit.Name;
            SourcePath = jobToEdit.Source;
            TargetPath = jobToEdit.Destination;
            SelectedBackupType = jobToEdit.Type;

            ValidateCommand = new RelayCommand(ValidateJob);
            CancelCommand = new RelayCommand(() =>
            {
                NavigateToMainMenu?.Invoke(this, EventArgs.Empty);
            });
        }

        private void ValidateJob()
        {
            // Validation des champs
            if (string.IsNullOrWhiteSpace(JobName))
            {
                System.Windows.MessageBox.Show(
                    LanguageViewModel["JobNameEmptyError"] ?? "The job name cannot be empty.",
                    LanguageViewModel["MissingField"] ?? "Missing Field",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(SourcePath))
            {
                System.Windows.MessageBox.Show(
                    LanguageViewModel["SourcePathEmptyError"] ?? "The source path cannot be empty.",
                    LanguageViewModel["MissingField"] ?? "Missing Field",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TargetPath))
            {
                System.Windows.MessageBox.Show(
                    LanguageViewModel["TargetPathEmptyError"] ?? "The target path cannot be empty.",
                    LanguageViewModel["MissingField"] ?? "Missing Field",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(SourcePath))
            {
                System.Windows.MessageBox.Show(
                    LanguageViewModel["SourceDirNotExistError"] ?? "The source directory does not exist.",
                    LanguageViewModel["InvalidDirectory"] ?? "Invalid Directory",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

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
                // En mode édition
                if (originalJobName != JobName)
                {
                    // Le nom a changé, vérifier si le nouveau nom existe déjà
                    if (backupManager.ListBackups().Any(j => j.Name == JobName && j.Name != originalJobName))
                    {
                        System.Windows.MessageBox.Show(
                            LanguageViewModel["JobNameExistsError"] ?? "A job with this name already exists.",
                            LanguageViewModel["Error"] ?? "Error",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                        return;
                    }

                    // Supprimer l'ancien job et en créer un nouveau avec le même index
                    backupManager.RemoveBackup(editJobIndex);
                    success = backupManager.AddBackupJob(JobName, SourcePath, TargetPath, SelectedBackupType);
                }
                else
                {
                    // Mettre à jour le job existant avec les mêmes valeurs
                    success = backupManager.UpdateBackupJob(JobName, SourcePath, TargetPath, SelectedBackupType);
                }
            }
            else
            {
                // En mode ajout
                success = backupManager.AddBackupJob(JobName, SourcePath, TargetPath, SelectedBackupType);
            }

            if (success)
            {
                NavigateToMainMenu?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                System.Windows.MessageBox.Show(
                    LanguageViewModel["JobSaveError"] ?? "Error saving the job.",
                    LanguageViewModel["Error"] ?? "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
