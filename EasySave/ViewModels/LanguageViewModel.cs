using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace EasySave.ViewModels
{
    // Singleton pour LanguageViewModel (partagé globalement)
    public class LanguageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private static readonly LanguageViewModel _instance = new LanguageViewModel();
        public static LanguageViewModel Instance => _instance;

        private ResourceManager _resourceManager;
        private string _currentLanguage;
        public string CurrentLanguage => _currentLanguage;

        // Constructeur privé pour le singleton
        private LanguageViewModel()
        {
            _currentLanguage = "english";
            _resourceManager = new ResourceManager("EasySave.LangResources.english", typeof(LanguageViewModel).Assembly);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
        }

        public string this[string key]
        {
            get
            {
                string value = _resourceManager.GetString(key, Thread.CurrentThread.CurrentUICulture);
                return value ?? $"!{key}!";
            }
        }

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
