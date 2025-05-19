using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EasySave.ViewModels;
using EasySave.Views;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using MessageBox = System.Windows.MessageBox;

namespace EasySave
{
    public partial class MainWindow : Window
    {
        // Un seul ViewModel principal
        private MainMenuViewModel _viewModel;


        public static object SharedLanguageViewModel { get; internal set; }

        /// <summary>
        /// Constructor for MainWindow.
        /// Initializes the DataContext with the LanguageViewModel from MainMenuViewModel.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainMenuViewModel();
            DataContext = _viewModel;
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
            DataContext = _viewModel;

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

        private void JobCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var row = DataGridRow.GetRowContainingElement(checkBox);
                if (row != null)
                {
                    int index = row.GetIndex();
                    if (!_viewModel.IsJobSelected(index))
                    {
                        _viewModel.ToggleJobSelection(index);
                    }
                }
            }
        }

        private void JobCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var row = DataGridRow.GetRowContainingElement(checkBox);
                if (row != null)
                {
                    int index = row.GetIndex();
                    if (_viewModel.IsJobSelected(index))
                    {
                        _viewModel.ToggleJobSelection(index);
                    }
                }
            }
        }


        private void ExecuteBackupJob_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedJobIndices.Count == 0)
            {
                MessageBox.Show("Please select at least one backup job.",
                                "No job selected",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            // Disable the button during execution
            Button executeButton = (Button)sender;
            executeButton.IsEnabled = false;

            try
            {
                _viewModel.ExecuteSelectedJobs();
                MessageBox.Show("The selected jobs have been executed successfully.",
                                "Execution completed",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while executing the jobs: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable the button
                executeButton.IsEnabled = true;
            }
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
