using EasySave.Commands;
using EasySave.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

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
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Utilise l'instance singleton de LanguageViewModel
        public LanguageViewModel LanguageViewModel { get; }

        private readonly BackupManager _backupManager;
        public ICommand DeleteJobCommand { get; private set; }
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

        /// <summary>
        /// Constructor for MainMenuViewModel.
        /// Initializes the LanguageViewModel property with the singleton instance.
        /// </summary>
        public MainMenuViewModel()
        {
            LanguageViewModel = LanguageViewModel.Instance;
            _backupManager = BackupManager.GetInstance();

            // Initialiser la collection des jobs
            LoadBackupJobs();
            DeleteJobCommand = new RelayCommand(DeleteSelectedJobs, CanDeleteJobs);
        }
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

        // Collection pour stocker les jobs sélectionnés
        private ObservableCollection<int> _selectedJobIndices = new ObservableCollection<int>();
        public ObservableCollection<int> SelectedJobIndices
        {
            get => _selectedJobIndices;
            set
            {
                _selectedJobIndices = value;
                OnPropertyChanged(nameof(SelectedJobIndices));
            }
        }

        // Propriété pour la case "Tout sélectionner"
        private bool _areAllJobsSelected;
        public bool AreAllJobsSelected
        {
            get => _areAllJobsSelected;
            set
            {
                _areAllJobsSelected = value;

                // Sélectionner ou désélectionner tous les jobs
                SelectAllJobs(value);

                OnPropertyChanged(nameof(AreAllJobsSelected));
            }
        }

        // Méthode pour sélectionner ou désélectionner tous les jobs
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
            // Hack : réaffecte la collection pour forcer le DataGrid à se re-binder
            var temp = BackupJobs;
            BackupJobs = null;
            BackupJobs = temp;
        }



        // Méthode pour mettre à jour l'état de "Tout sélectionner" en fonction des sélections individuelles
        private void UpdateAllJobsSelectedState()
        {
            bool allSelected = BackupJobs.Count > 0 && _selectedJobIndices.Count == BackupJobs.Count;
            if (_areAllJobsSelected != allSelected)
            {
                _areAllJobsSelected = allSelected;
                OnPropertyChanged(nameof(AreAllJobsSelected));
            }
        }

        // Mise à jour de la méthode ToggleJobSelection pour mettre à jour l'état de "Tout sélectionner"
        public void ToggleJobSelection(int index)
        {
            if (_selectedJobIndices.Contains(index))
                _selectedJobIndices.Remove(index);
            else
                _selectedJobIndices.Add(index);

            OnPropertyChanged(nameof(SelectedJobIndices));
            OnPropertyChanged(nameof(BackupJobs)); // Ajoutez cette ligne
            
            // Force le rafraîchissement de chaque ligne du DataGrid
            foreach (var job in BackupJobs)
            {
                // Notifie que la propriété fictive "IsSelected" a changé pour chaque job
                // (même si elle n'existe pas, cela force le DataGrid à re-binder la ligne)
                OnPropertyChanged("Item[]");
            }


            UpdateAllJobsSelectedState();
        }


        // Méthode pour vérifier si un job est sélectionné
        public bool IsJobSelected(int index)
        {
            return _selectedJobIndices.Contains(index);
        }

        // Méthode pour exécuter les jobs sélectionnés
        public void ExecuteSelectedJobs()
        {
            if (_selectedJobIndices.Count > 0)
            {
                _backupManager.ExecuteBackupJob(_selectedJobIndices.ToList(), "sequential");
            }
        }
        private bool CanDeleteJobs()
        {
            return SelectedJobIndices.Count > 0;
        }

        public void DeleteSelectedJobs()
        {
            if (SelectedJobIndices.Count == 0)
                return;

            // Demander confirmation avant suppression
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

    }
}