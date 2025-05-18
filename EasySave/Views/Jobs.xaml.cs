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
        private JobsViewModel _viewModel = new JobsViewModel();

        public Jobs()
        {
            InitializeComponent();
            // Utilise JobsViewModel comme DataContext
            DataContext = _viewModel.LanguageViewModel;
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            // Utilisez _viewModel pour la logique liée aux jobs
        }
    }
}
