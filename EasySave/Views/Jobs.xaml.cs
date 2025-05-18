using System;
using System.Windows;
using System.Windows.Controls;
using EasySave.easysave.ViewModels;

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