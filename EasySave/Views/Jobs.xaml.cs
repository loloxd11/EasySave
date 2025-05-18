using System;
using System.Windows;
using System.Windows.Controls;
using EasySave.ViewModels;
using WinForm = System.Windows.Forms;

namespace EasySave.Views
{
    /// <summary>
    /// Interaction logic for Jobs.xaml
    /// This class represents the code-behind for the Jobs.xaml page in the EasySave application.
    /// It is responsible for handling user interactions and binding the ViewModel to the UI.
    /// </summary>
    public partial class Jobs : Page
    {
        // Instance of the ViewModel associated with this page.
        private JobsViewModel _viewModel;

        /// <summary>
        /// Constructor for the Jobs page.
        /// Initializes the ViewModel and sets it as the DataContext for data binding.
        /// Subscribes to the navigation event to handle transitions to the main menu.
        /// </summary>
        public Jobs()
        {
            InitializeComponent();
            _viewModel = new JobsViewModel();
            DataContext = _viewModel; // Use the ViewModel as the DataContext for data binding.

            // Subscribe to the navigation event to handle transitions to the main menu.
            _viewModel.NavigateToMainMenu += OnNavigateToMainMenu;
        }

        /// <summary>
        /// Event handler for navigating back to the main menu.
        /// If the page is hosted in a Window, it resets the content to the main menu.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnNavigateToMainMenu(object sender, EventArgs e)
        {
            // Check if the page is hosted in a Window and reset its content to the main menu.
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.ResetToMainMenu();
            }
        }

        /// <summary>
        /// Event handler for selecting the source folder.
        /// Opens a folder browser dialog and updates the SourcePath property in the ViewModel.
        /// </summary>
        /// <param name="sender">The event sender (button click).</param>
        /// <param name="e">Event arguments.</param>
        private void SelectSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            // Open a folder browser dialog to select the source folder.
            WinForm.FolderBrowserDialog folderDialog = new WinForm.FolderBrowserDialog { };

            // If a folder is selected, update the SourcePath in the ViewModel.
            if (folderDialog.ShowDialog() == WinForm.DialogResult.OK)
            {
                _viewModel.SourcePath = folderDialog.SelectedPath;
            }
        }

        /// <summary>
        /// Event handler for selecting the target folder.
        /// Opens a folder browser dialog and updates the TargetPath property in the ViewModel.
        /// </summary>
        /// <param name="sender">The event sender (button click).</param>
        /// <param name="e">Event arguments.</param>
        private void SelectTargetFolder_Click(object sender, RoutedEventArgs e)
        {
            // Open a folder browser dialog to select the target folder.
            WinForm.FolderBrowserDialog folderDialog = new WinForm.FolderBrowserDialog { };

            // If a folder is selected, update the TargetPath in the ViewModel.
            if (folderDialog.ShowDialog() == WinForm.DialogResult.OK)
            {
                _viewModel.TargetPath = folderDialog.SelectedPath;
            }
        }
    }
}
