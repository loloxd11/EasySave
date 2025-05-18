using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using EasySave.Models;

namespace EasySave.ViewModels
{
    // MainMenuViewModel n'est PAS un singleton, il est instanci� par vue
    public class MainMenuViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Utilise l'instance singleton de LanguageViewModel
        public LanguageViewModel LanguageViewModel { get; }

        private readonly BackupManager _backupManager;

        // Collection observable pour les jobs de sauvegarde
        private ObservableCollection<BackupJob> _backupJobs;
        public ObservableCollection<BackupJob> BackupJobs
        {
            get => _backupJobs;
            set
            {
                _backupJobs = value;
                OnPropertyChanged(nameof(BackupJobs));
            }
        }

        // Job s�lectionn� pour l'�dition ou la suppression
        private BackupJob _selectedJob;
        public BackupJob SelectedJob
        {
            get => _selectedJob;
            set
            {
                _selectedJob = value;
                OnPropertyChanged(nameof(SelectedJob));
            }
        }

        public MainMenuViewModel()
        {
            LanguageViewModel = LanguageViewModel.Instance;
            _backupManager = BackupManager.GetInstance();

            // Initialiser la collection des jobs
            LoadBackupJobs();
        }

        // M�thode pour r�cup�rer la liste des jobs de sauvegarde et les afficher
        public void LoadBackupJobs()
        {
            // R�cup�rer la liste des jobs depuis le BackupManager
            var jobs = _backupManager.ListBackups();

            // Cr�er une ObservableCollection � partir de la liste
            BackupJobs = new ObservableCollection<BackupJob>(jobs);
        }

        // M�thode pour rafra�chir la liste des jobs (� appeler apr�s ajout/suppression/modification)
        public void RefreshJobsList()
        {
            LoadBackupJobs();
        }
    }
}