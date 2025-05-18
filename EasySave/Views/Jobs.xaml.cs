using System;
using System.Windows;
using System.Windows.Controls;
using EasySave.ViewModels;
using WinForm = System.Windows.Forms;

namespace EasySave.Views
{
    /// <summary>
    /// Logique d'interaction pour Jobs.xaml
    /// </summary>
    public partial class Jobs : Page
    {
        private JobsViewModel _viewModel;

        public Jobs()
        {
            InitializeComponent();
            _viewModel = new JobsViewModel();
            DataContext = _viewModel; // Utiliser le ViewModel comme DataContext
        }
        private void SelectSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            WinForm.FolderBrowserDialog folderDialog = new WinForm.FolderBrowserDialog
            { };

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
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Logique pour valider le job
            // Vous pouvez appeler la méthode ValidateCommand ici si nécessaire
            if (_viewModel.ValidateCommand.CanExecute(null))
            {
                _viewModel.ValidateCommand.Execute(null);
            }

        }

    }
}