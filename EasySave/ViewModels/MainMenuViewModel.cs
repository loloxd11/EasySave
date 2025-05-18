using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using EasySave.Models;

namespace EasySave.ViewModels
{
    // MainMenuViewModel n'est PAS un singleton, il est instancié par vue
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

        // Job sélectionné pour l'édition ou la suppression
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

        // Méthode pour récupérer la liste des jobs de sauvegarde et les afficher
        public void LoadBackupJobs()
        {
            // Récupérer la liste des jobs depuis le BackupManager
            var jobs = _backupManager.ListBackups();

            // Créer une ObservableCollection à partir de la liste
            BackupJobs = new ObservableCollection<BackupJob>(jobs);
        }

        // Méthode pour rafraîchir la liste des jobs (à appeler après ajout/suppression/modification)
        public void RefreshJobsList()
        {
            LoadBackupJobs();
        }
    }
}