using System.ComponentModel;
using EasySave.Models;

namespace EasySave.ViewModels
{
    // MainMenuViewModel n'est PAS un singleton, il est instanci� par vue
    public class MainMenuViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Utilise l'instance singleton de LanguageViewModel
        public LanguageViewModel LanguageViewModel {get;}

        public MainMenuViewModel()
        {
            LanguageViewModel = LanguageViewModel.Instance;
        }

        // Ajoutez ici d'autres propri�t�s ou m�thodes n�cessaires pour le menu principal
    }
}