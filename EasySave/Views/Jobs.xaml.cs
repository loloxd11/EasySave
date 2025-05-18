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
            _viewModel = new JobsViewModel();
            DataContext = _viewModel; // Utilisez directement le ViewModel comme DataContext
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
