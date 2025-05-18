using System.ComponentModel;
using EasySave.Models;

namespace EasySave.ViewModels
{
    // MainMenuViewModel n'est PAS un singleton, il est instancié par vue
    public class MainMenuViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Utilise l'instance singleton de LanguageViewModel
        public LanguageViewModel LanguageViewModel {get;}

        public MainMenuViewModel()
        {
            LanguageViewModel = LanguageViewModel.Instance;
        }

        // Ajoutez ici d'autres propriétés ou méthodes nécessaires pour le menu principal
    }
}