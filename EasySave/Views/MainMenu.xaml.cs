using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EasySave.ViewModels;
using EasySave.Views;

namespace EasySave
{
    public partial class MainWindow : Window
    {
        // Un seul ViewModel principal
        private MainMenuViewModel _viewModel = new MainMenuViewModel();
        private bool isLanguagePanelVisible = false;

        public static object SharedLanguageViewModel { get; internal set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel.LanguageViewModel;
        }

        private void TestChangeLanguageToEnglish(object sender, RoutedEventArgs e)
        {
            _viewModel.LanguageViewModel.ChangeLanguage("english");
        }

        private void TestChangeLanguageToFrench(object sender, RoutedEventArgs e)
        {
            _viewModel.LanguageViewModel.ChangeLanguage("french");
        }

        private void AddBackupJob_Click(object sender, RoutedEventArgs e)
        {
            Frame jobsFrame = new Frame();
            Jobs jobsPage = new Jobs();
            jobsFrame.Content = jobsPage;
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
