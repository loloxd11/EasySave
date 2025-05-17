using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EasySave.ViewModels;

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
    }
}
