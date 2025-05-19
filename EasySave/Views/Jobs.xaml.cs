using System;
using System.Windows;
using System.Windows.Controls;
using EasySave.ViewModels;
using EasySave.Models;
using WinForm = System.Windows.Forms;

namespace EasySave.Views
{
    public partial class Jobs : Page
    {
        private JobsViewModel _viewModel;

        // Constructeur standard pour l'ajout d'un nouveau job
        public Jobs()
        {
            InitializeComponent();
            _viewModel = new JobsViewModel();
            DataContext = _viewModel;
            _viewModel.NavigateToMainMenu += OnNavigateToMainMenu;
        }

        // Constructeur pour l'édition d'un job existant
        public Jobs(BackupJob jobToEdit, int jobIndex)
        {
            InitializeComponent();
            _viewModel = new JobsViewModel(jobToEdit, jobIndex);
            DataContext = _viewModel;
            _viewModel.NavigateToMainMenu += OnNavigateToMainMenu;
        }

        private void OnNavigateToMainMenu(object sender, EventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.ResetToMainMenu();
            }
        }

        private void SelectSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            WinForm.FolderBrowserDialog folderDialog = new WinForm.FolderBrowserDialog { };
            if (folderDialog.ShowDialog() == WinForm.DialogResult.OK)
            {
                _viewModel.SourcePath = folderDialog.SelectedPath;
            }
        }

        private void SelectTargetFolder_Click(object sender, RoutedEventArgs e)
        {
            WinForm.FolderBrowserDialog folderDialog = new WinForm.FolderBrowserDialog { };
            if (folderDialog.ShowDialog() == WinForm.DialogResult.OK)
            {
                _viewModel.TargetPath = folderDialog.SelectedPath;
            }
        }
    }
}
