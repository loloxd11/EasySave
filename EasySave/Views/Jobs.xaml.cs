using EasySave.Models;
using EasySave.ViewModels;
using System.Windows;
using System.Windows.Controls;
using WinForm = System.Windows.Forms;

namespace EasySave.Views
{
    /// <summary>
    /// Interaction logic for Jobs page.
    /// Handles creation and editing of backup jobs.
    /// </summary>
    public partial class Jobs : Page
    {
        /// <summary>
        /// ViewModel instance for managing backup jobs.
        /// </summary>
        private JobsViewModel _viewModel;

        /// <summary>
        /// Default constructor for adding a new backup job.
        /// Initializes the view model and sets up data binding.
        /// </summary>
        public Jobs()
        {
            InitializeComponent();
            _viewModel = new JobsViewModel();
            DataContext = _viewModel;
            _viewModel.NavigateToMainMenu += OnNavigateToMainMenu;
        }

        /// <summary>
        /// Constructor for editing an existing backup job.
        /// Initializes the view model with the job to edit and its index.
        /// </summary>
        /// <param name="jobToEdit">The backup job to edit.</param>
        /// <param name="jobIndex">The index of the job in the list.</param>
        public Jobs(BackupJob jobToEdit, int jobIndex)
        {
            InitializeComponent();
            _viewModel = new JobsViewModel(jobToEdit, jobIndex);
            DataContext = _viewModel;
            _viewModel.NavigateToMainMenu += OnNavigateToMainMenu;
        }

        /// <summary>
        /// Handles navigation back to the main menu when requested by the view model.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void OnNavigateToMainMenu(object sender, EventArgs e)
        {
            // Get the parent window and reset to the main menu
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.ResetToMainMenu();
            }
        }

        /// <summary>
        /// Opens a folder browser dialog for selecting the source folder.
        /// Updates the view model with the selected path.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void SelectSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            // Show folder browser dialog for source path selection
            WinForm.FolderBrowserDialog folderDialog = new WinForm.FolderBrowserDialog { };
            if (folderDialog.ShowDialog() == WinForm.DialogResult.OK)
            {
                _viewModel.SourcePath = folderDialog.SelectedPath;
            }
        }

        /// <summary>
        /// Opens a folder browser dialog for selecting the target folder.
        /// Updates the view model with the selected path.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void SelectTargetFolder_Click(object sender, RoutedEventArgs e)
        {
            // Show folder browser dialog for target path selection
            WinForm.FolderBrowserDialog folderDialog = new WinForm.FolderBrowserDialog { };
            if (folderDialog.ShowDialog() == WinForm.DialogResult.OK)
            {
                _viewModel.TargetPath = folderDialog.SelectedPath;
            }
        }
    }
}
