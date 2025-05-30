using EasySave.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace EasySave.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml.
    /// This page allows the user to configure application settings such as encryption, logging, and process priority.
    /// </summary>
    public partial class SettingsView : Page
    {
        // Instance of the ViewModel associated with this page
        private SettingsViewModel _viewModel;

        /// <summary>
        /// Constructor for the settings page.
        /// Initializes the ViewModel and sets the data context.
        /// </summary>
        public SettingsView()
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel();
            DataContext = _viewModel;

            // Subscribe to the event for navigation to the main menu
            _viewModel.NavigateToMainMenu += OnNavigateToMainMenu;

            // Update the PasswordBox with the passphrase loaded from the ViewModel
            Loaded += SettingsView_Loaded;
        }

        /// <summary>
        /// Event handler for the Loaded event.
        /// Initializes the PasswordBox with the saved passphrase from the ViewModel.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Set the PasswordBox value to the passphrase from the ViewModel
            PassphraseBox.Password = _viewModel.EncryptionPassphrase;
        }

        /// <summary>
        /// Event handler for setting the priority process.
        /// Calls the ViewModel method to set the selected process as priority.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void SetPriorityButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SetPriorityProcess();
        }

        /// <summary>
        /// Event handler for clearing the priority process.
        /// Calls the ViewModel method to clear the priority process.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void ClearPriorityButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearPriorityProcess();
        }

        /// <summary>
        /// Event handler for navigation to the main menu.
        /// If the page is hosted in a window, resets the content to the main menu.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnNavigateToMainMenu(object sender, EventArgs e)
        {
            // Check if the page is hosted in a window and reset its content to the main menu
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.ResetToMainMenu();
            }
        }

        /// <summary>
        /// Event handler for saving settings.
        /// Retrieves the passphrase from the PasswordBox and triggers the ViewModel's SaveCommand.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (SettingsViewModel)DataContext;

            // Get the value from the PasswordBox
            string passphrase = PassphraseBox.Password;

            // Only update the password if the user entered a non-empty value
            if (!string.IsNullOrEmpty(passphrase))
            {
                viewModel.EncryptionPassphrase = passphrase;
                // Save the password directly using the EncryptionService
                EasySave.Models.EncryptionService.GetInstance().SetEncryptionPassword(passphrase);
            }

            // Execute the SaveCommand for all other settings
            viewModel.SaveCommand.Execute(null);
        }

    }
}
