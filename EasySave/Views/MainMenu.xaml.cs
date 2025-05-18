using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EasySave.easysave.ViewModels;
using EasySave.Views;

namespace EasySave
{
    public partial class MainWindow : Window
    {
        private MainMenuViewModel _viewModel = new MainMenuViewModel();
        private bool isLanguagePanelVisible = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            ApplyTranslations();
        }

        private void ApplyTranslations()
        {
            // Exemple de traduction dynamique (adapte selon tes clés)
            // btnAdd.Content = languageManager.GetTranslation("MenuAddJob");
            // btnEdit.Content = languageManager.GetTranslation("MenuUpdateJob");
            // btnDelete.Content = languageManager.GetTranslation("MenuRemoveJob");
            // btnExecute.Content = languageManager.GetTranslation("MenuExecuteJob");
            // SettingsButton.Content = languageManager.GetTranslation("Settings");
        }


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

        // Exemple de test pour changer la langue (à appeler depuis un bouton ou autre)
        private void TestChangeLanguageToEnglish(object sender, RoutedEventArgs e)
        {
            _viewModel.ChangeLanguage("english");
        }

        private void TestChangeLanguageToFrench(object sender, RoutedEventArgs e)
        {
            _viewModel.ChangeLanguage("french");
        }

        private void AddBackupJob_Click(object sender, RoutedEventArgs e)
        {
            // Création et affichage de la vue Jobs
            Frame jobsFrame = new Frame();
            Jobs jobsPage = new Jobs();
            jobsFrame.Content = jobsPage;

            // Vous pouvez afficher cette page dans une fenêtre ou remplacer le contenu principal
            // Par exemple:
            Content = jobsFrame;
        }
        private void EditBackupJob_Click(object sender, RoutedEventArgs e)
        {
            // Logique pour éditer un job
        }
        private void DeleteBackupJob_Click(object sender, RoutedEventArgs e)
        {
            // Logique pour supprimer un job
        }
        private void ExecuteBackupJob_Click(object sender, RoutedEventArgs e)
        {
            // Logique pour exécuter un job
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Logique pour ouvrir les paramètres
        }

    }
}
