using System.ComponentModel;
using EasySave.Models;

namespace EasySave.ViewModels
{
    /// <summary>
    /// ViewModel for the main menu in the application.
    /// This class is not a singleton; it is instantiated per view.
    /// Implements INotifyPropertyChanged to support data binding and notify the UI of property changes.
    /// </summary>
    public class MainMenuViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Event triggered when a property value changes.
        /// Used to notify the UI of updates to bound properties.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Provides access to the singleton instance of LanguageViewModel.
        /// Used to manage and switch application languages.
        /// </summary>
        public LanguageViewModel LanguageViewModel { get; }

        /// <summary>
        /// Constructor for MainMenuViewModel.
        /// Initializes the LanguageViewModel property with the singleton instance.
        /// </summary>
        public MainMenuViewModel()
        {
            LanguageViewModel = LanguageViewModel.Instance;
        }

        // Add other properties or methods necessary for the main menu here.
    }
}