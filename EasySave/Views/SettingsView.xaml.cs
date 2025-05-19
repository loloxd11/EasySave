using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EasySave.ViewModels;

namespace EasySave.Views
{
    /// <summary>
    /// Logique d'interaction pour SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Page
    {
        // Instance du ViewModel associé à cette page
        private SettingsViewModel _viewModel;

        /// <summary>
        /// Constructeur de la page des paramètres
        /// Initialise le ViewModel et définit le contexte de données
        /// </summary>
        public SettingsView()
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel();
            DataContext = _viewModel;

            // Abonnement à l'événement de navigation vers le menu principal
            _viewModel.NavigateToMainMenu += OnNavigateToMainMenu;

            // Mettre à jour la PasswordBox avec la passphrase chargée du ViewModel
            Loaded += SettingsView_Loaded;
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialiser la PasswordBox avec la passphrase sauvegardée
            PassphraseBox.Password = _viewModel.EncryptionPassphrase;
        }

        /// <summary>
        /// Gestionnaire d'événement pour définir le processus prioritaire
        /// </summary>
        private void SetPriorityButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SetPriorityProcess();
        }

        /// <summary>
        /// Gestionnaire d'événement pour effacer le processus prioritaire
        /// </summary>
        private void ClearPriorityButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearPriorityProcess();
        }

        /// <summary>
        /// Gestionnaire d'événement pour la navigation vers le menu principal
        /// Si la page est hébergée dans une fenêtre, réinitialise le contenu au menu principal
        /// </summary>
        /// <param name="sender">L'émetteur de l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void OnNavigateToMainMenu(object sender, EventArgs e)
        {
            // Vérifie si la page est hébergée dans une fenêtre et réinitialise son contenu au menu principal
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.ResetToMainMenu();
            }
        }
    }

    /// <summary>
    /// Convertisseur pour vérifier si une valeur n'est pas nulle
    /// </summary>
    public class NotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
