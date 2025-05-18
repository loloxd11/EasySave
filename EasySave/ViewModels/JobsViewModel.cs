using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.easysave.ViewModels
{
    internal class JobsViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Utilise l'instance singleton de LanguageViewModel
        public LanguageViewModel LanguageViewModel { get; }
        public JobsViewModel() {
            LanguageViewModel = LanguageViewModel.Instance;
        }
    }
}
