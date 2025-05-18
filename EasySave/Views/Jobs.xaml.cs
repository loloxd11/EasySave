using System;
using System.Windows;
using System.Windows.Controls;
using EasySave.ViewModels;

namespace EasySave.Views
{
    /// <summary>
    /// Logique d'interaction pour Jobs.xaml
    /// </summary>
    public partial class Jobs : Page
    {
        private readonly MainMenuViewModel _viewModel;

        public Jobs()
        {
            InitializeComponent();

            // Utiliser le même système de localisation que dans MainMenu
            _viewModel = new MainMenuViewModel();
            DataContext = _viewModel; // Fix: Ensure 'DataContext' is accessible by inheriting from 'FrameworkElement' or 'Page'.
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            // Logiq
        }
    }
}
