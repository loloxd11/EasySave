using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace EasySave.ViewModels
{
    /// <summary>
    /// Singleton class for managing application language settings globally.
    /// Implements INotifyPropertyChanged to notify UI of language changes.
    /// </summary>
    public class LanguageViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Event triggered when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        // Singleton instance of the LanguageViewModel.
        private static readonly LanguageViewModel _instance = new LanguageViewModel();

        /// <summary>
        /// Public accessor for the singleton instance.
        /// </summary>
        public static LanguageViewModel Instance => _instance;

        // ResourceManager to manage language resource files.
        private ResourceManager _resourceManager;

        // Current language of the application.
        private string _currentLanguage;

        /// <summary>
        /// Gets the current language of the application.
        /// </summary>
        public string CurrentLanguage => _currentLanguage;

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// Initializes the default language to English.
        /// </summary>
        private LanguageViewModel()
        {
            _currentLanguage = "english";
            _resourceManager = new ResourceManager("EasySave.LangResources.english", typeof(LanguageViewModel).Assembly);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
        }

        /// <summary>
        /// Indexer to retrieve localized strings by key.
        /// If the key is not found, returns the key wrapped in exclamation marks.
        /// </summary>
        /// <param name="key">The key for the localized string.</param>
        /// <returns>The localized string or a placeholder if not found.</returns>
        public string this[string key]
        {
            get
            {
                string value = _resourceManager.GetString(key, Thread.CurrentThread.CurrentUICulture);
                return value ?? $"!{key}!";
            }
        }

        /// <summary>
        /// Changes the application's language and updates the ResourceManager and culture.
        /// Notifies the UI of the language change.
        /// </summary>
        /// <param name="culture">The culture identifier (e.g., "french", "english").</param>
        public void ChangeLanguage(string culture)
        {
            switch (culture)
            {
                case "french":
                    _resourceManager = new ResourceManager("EasySave.LangResources.french", typeof(LanguageViewModel).Assembly);
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr");
                    _currentLanguage = "french";
                    break;
                case "english":
                default:
                    _resourceManager = new ResourceManager("EasySave.LangResources.english", typeof(LanguageViewModel).Assembly);
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
                    _currentLanguage = "english";
                    break;
            }

            // Notify UI of the CurrentLanguage property change and refresh all bindings.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
