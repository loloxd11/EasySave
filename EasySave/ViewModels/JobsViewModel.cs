using EasySave.Commands;
using EasySave.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace EasySave.easysave.ViewModels
{
    public class JobsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public LanguageViewModel LanguageViewModel { get; }
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // instance de BackupManager
        private BackupManager backupManager = BackupManager.GetInstance();

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

        public JobsViewModel()
        {
            LanguageViewModel = LanguageViewModel.Instance;
            ValidateCommand = new RelayCommand(() =>
            {
                // Exécution du job ou autre
                // messagebox qui affiche les valeurs entrer
                System.Windows.MessageBox.Show($"Name: {JobName}\nSource: {SourcePath}\nTarget: {TargetPath}\nBackupType: {SelectedBackupType}", "Job Details",System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                backupManager.AddBackupJob(JobName, SourcePath, TargetPath, SelectedBackupType);
            });
        }
    }
}
