using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EasySave
{
    public partial class MainWindow : Window
    {
        private readonly LanguageManager languageManager;
        private ObservableCollection<BackupJob> backupJobs = new ObservableCollection<BackupJob>();
        private bool isLanguagePanelVisible = false;

        public MainWindow()
        {
            InitializeComponent();
            languageManager = LanguageManager.GetInstance();
            LanguagePanel.ItemsSource = backupJobs; // Liaison DataGrid
            ApplyTranslations();
        }

        private void ApplyTranslations()
        {
            // Exemple de traduction dynamique (adapte selon tes clés)
            // btnAdd.Content = languageManager.GetTranslation("MenuAddJob");
            // btnEdit.Content = languageManager.GetTranslation("MenuUpdateJob");
            // btnDelete.Content = languageManager.GetTranslation("MenuRemoveJob");
            // btnExecute.Content = languageManager.GetTranslation("MenuExecuteJob");
            SettingsButton.Content = languageManager.GetTranslation("Settings");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            LanguageMenu.Visibility = LanguageMenu.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LanguageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string lang)
            {
                languageManager.SetLanguage(lang);
                ApplyTranslations();
                LanguageMenu.Visibility = Visibility.Collapsed;
            }
        }

        private void AddBackupJob_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddBackupJobDialog();
            if (dialog.ShowDialog() == true)
            {
                // Utilise le singleton BackupManager
                var manager = BackupManager.GetInstance();
                bool added = manager.AddBackupJob(
                    dialog.JobName,
                    dialog.SourcePath,
                    dialog.TargetPath,
                    dialog.BackupType
                );

                if (added)
                {
                    // Mets à jour la liste affichée dans le DataGrid
                    backupJobs.Clear();
                    foreach (var job in manager.ListBackups())
                        backupJobs.Add(job);
                }
                else
                {
                    MessageBox.Show("Impossible d'ajouter le travail de sauvegarde (voir la console pour le détail).");
                }
            }
        }


        private void EditBackupJob_Click(object sender, RoutedEventArgs e)
        {
            if (LanguagePanel.SelectedItem is BackupJob job)
            {
                MessageBox.Show($"Modifier : {job.Name}");
                // Tu peux ouvrir une fenêtre de modification ici
            }
        }

        private void DeleteBackupJob_Click(object sender, RoutedEventArgs e)
        {
            if (LanguagePanel.SelectedItem is BackupJob job)
            {
                if (MessageBox.Show($"Supprimer {job.Name} ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    backupJobs.Remove(job);
                }
            }
        }

        private void ExecuteBackupJob_Click(object sender, RoutedEventArgs e)
        {
            if (LanguagePanel.SelectedItem is BackupJob job)
            {
                job.Execute();
                MessageBox.Show($"Job {job.Name} exécuté !");
            }
        }

        // Les méthodes suivantes sont inutiles si tu relies bien les bons événements dans le XAML
        // private void btnAdd_Click(object sender, RoutedEventArgs e) { ... }
        // private void btnEdit_Click(object sender, RoutedEventArgs e) { ... }
        // private void btnDelete_Click(object sender, RoutedEventArgs e) { ... }
        // private void btnExecute_Click(object sender, RoutedEventArgs e) { ... }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            isLanguagePanelVisible = !isLanguagePanelVisible;
            LanguagePanel.Visibility = isLanguagePanelVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HideLanguagePanel()
        {
            isLanguagePanelVisible = false;
            LanguagePanel.Visibility = Visibility.Collapsed;
        }
    }
}
