using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EasySave.easysave.ViewModels;

namespace EasySave.Views
{
    /// <summary>
    /// Logique d'interaction pour SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Page
    {
        private readonly MainMenuViewModel _viewModel;

        public SettingsView()
        {
            InitializeComponent();
            // Utiliser le même système de localisation que dans MainMenu
            _viewModel = new MainMenuViewModel();
            DataContext = _viewModel; // Fix: Ensure 'DataContext' is accessible by inheriting from 'FrameworkElement' or 'Page'.
        }
    }
}
