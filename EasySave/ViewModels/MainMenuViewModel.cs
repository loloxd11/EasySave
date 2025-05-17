using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace EasySave.ViewModels
{
    public class MainMenuViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ResourceManager _resourceManager;

        public MainMenuViewModel()
        {
            // Utilise la ressource par défaut (english)
            _resourceManager = new ResourceManager("EasySave.LangResources.english", typeof(MainMenuViewModel).Assembly);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
        }

        // Indexeur pour accéder dynamiquement aux ressources
        public string this[string key]
        {
            get
            {
                string value = _resourceManager.GetString(key, Thread.CurrentThread.CurrentUICulture);
                return value ?? $"!{key}!";
            }
        }

        // Méthode pour changer la langue
        public void ChangeLanguage(string culture)
        {
            switch (culture)
            {
                case "french":
                    _resourceManager = new ResourceManager("EasySave.LangResources.french", typeof(MainMenuViewModel).Assembly);
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr");
                    break;

                case "english":
                default:
                    _resourceManager = new ResourceManager("EasySave.LangResources.english", typeof(MainMenuViewModel).Assembly);
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
                    break;
            }

            // Notifie tous les bindings de se mettre à jour
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
