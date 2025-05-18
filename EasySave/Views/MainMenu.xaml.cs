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
        // Main ViewModel for the application
        private MainMenuViewModel _viewModel = new MainMenuViewModel();
        private bool isLanguagePanelVisible = false;

        public static object SharedLanguageViewModel { get; internal set; }

        /// <summary>
        /// Constructor for MainWindow.
        /// Initializes the DataContext with the LanguageViewModel from MainMenuViewModel.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel.LanguageViewModel;
        }

        /// <summary>
        /// Resets the current view to the MainMenu.
        /// Clears the current content, reinitializes the ViewModel, and reloads the MainMenu XAML.
        /// </summary>
        public void ResetToMainMenu()
        {
            // Clear the current content
            Content = null;

            // Reinitialize the ViewModel
            _viewModel = new MainMenuViewModel();
            DataContext = _viewModel.LanguageViewModel;

            // Reset the name scope for the window
            NameScope.SetNameScope(this, new NameScope());

            // Load the MainMenu XAML directly
            System.Windows.Application.LoadComponent(
                this,
                new Uri("/EasySave;component/Views/MainMenu.xaml", UriKind.Relative)
            );
        }

        /// <summary>
        /// Changes the application language to English.
        /// </summary>
        private void TestChangeLanguageToEnglish(object sender, RoutedEventArgs e)
        {
            _viewModel.LanguageViewModel.ChangeLanguage("english");
        }

        /// <summary>
        /// Changes the application language to French.
        /// </summary>
        private void TestChangeLanguageToFrench(object sender, RoutedEventArgs e)
        {
            _viewModel.LanguageViewModel.ChangeLanguage("french");
        }

        /// <summary>
        /// Navigates to the Add Backup Job page.
        /// Creates a new Frame and sets its content to the Jobs page.
        /// </summary>
        private void AddBackupJob_Click(object sender, RoutedEventArgs e)
        {
            Frame jobsFrame = new Frame();
            Jobs jobsPage = new Jobs();
            jobsFrame.Content = jobsPage;
            Content = jobsFrame;
        }

        /// <summary>
        /// Placeholder for editing a backup job.
        /// </summary>
        private void EditBackupJob_Click(object sender, RoutedEventArgs e)
        {
            // Logic for editing a backup job
        }

        /// <summary>
        /// Placeholder for deleting a backup job.
        /// </summary>
        private void DeleteBackupJob_Click(object sender, RoutedEventArgs e)
        {
            // Logic for deleting a backup job
        }

        /// <summary>
        /// Placeholder for executing a backup job.
        /// </summary>
        private void ExecuteBackupJob_Click(object sender, RoutedEventArgs e)
        {
            // Logic for executing a backup job
        }

        /// <summary>
        /// Placeholder for opening the settings page.
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Logic for opening settings
        }
    }
}
