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
            // Utilise la ressource par d�faut (english)
            _resourceManager = new ResourceManager("EasySave.LangResources.english", typeof(MainMenuViewModel).Assembly);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
        }

        // Indexeur pour acc�der dynamiquement aux ressources
        public string this[string key]
        {
            get
            {
                string value = _resourceManager.GetString(key, Thread.CurrentThread.CurrentUICulture);
                return value ?? $"!{key}!";
            }
        }

        // M�thode pour changer la langue
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

            // Notifie tous les bindings de se mettre � jour
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
