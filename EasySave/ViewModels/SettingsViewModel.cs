using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using EasySave.Commands;
using EasySave.Models;
using System.Diagnostics;
using System.Linq;

namespace EasySave.ViewModels
{
    /// <summary>
    /// ViewModel pour la page des paramètres de l'application
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Événement déclenché lorsqu'une propriété change de valeur
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Instance du ViewModel de langue pour gérer les traductions
        /// </summary>
        public LanguageViewModel LanguageViewModel { get; }

        /// <summary>
        /// Instance du gestionnaire de configuration
        /// </summary>
        private readonly ConfigManager _configManager;

        /// <summary>
        /// Événement pour naviguer vers le menu principal
        /// </summary>
        public event EventHandler NavigateToMainMenu;

        /// <summary>
        /// Commande pour enregistrer les paramètres et retourner au menu principal
        /// </summary>
        public ICommand SaveCommand { get; set; }

        /// <summary>
        /// Commande pour annuler et revenir au menu principal
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Commande pour changer la langue en anglais
        /// </summary>
        public ICommand EnglishCommand { get; }

        /// <summary>
        /// Commande pour changer la langue en français
        /// </summary>
        public ICommand FrenchCommand { get; }

        /// <summary>
        /// Commande pour ajouter une extension de fichier à la liste
        /// </summary>
        public ICommand AddExtensionCommand { get; }

        /// <summary>
        /// Commande pour supprimer une extension de fichier de la liste
        /// </summary>
        public ICommand RemoveExtensionCommand { get; }

        /// <summary>
        /// Commande pour actualiser la liste des processus en cours d'exécution
        /// </summary>
        public ICommand RefreshProcessesCommand { get; }
        
        /// <summary>
        /// Commande pour définir le format de log en XML
        /// </summary>
        public ICommand SetXmlFormatCommand { get; }
        
        /// <summary>
        /// Commande pour définir le format de log en JSON
        /// </summary>
        public ICommand SetJsonFormatCommand { get; }

        /// <summary>
        /// Langue actuelle de l'application
        /// </summary>
        public string CurrentLanguage => LanguageViewModel.CurrentLanguage;

        private LogFormat _selectedLogFormat;
        /// <summary>
        /// Format de log sélectionné
        /// </summary>
        public LogFormat SelectedLogFormat
        {
            get => _selectedLogFormat;
            set
            {
                if (_selectedLogFormat != value)
                {
                    _selectedLogFormat = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsXmlSelected));
                    OnPropertyChanged(nameof(IsJsonSelected));
                }
            }
        }
        
        /// <summary>
        /// Indique si le format XML est sélectionné
        /// </summary>
        public bool IsXmlSelected => _selectedLogFormat == LogFormat.XML;
        
        /// <summary>
        /// Indique si le format JSON est sélectionné
        /// </summary>
        public bool IsJsonSelected => _selectedLogFormat == LogFormat.JSON;

        private string _newExtension;
        /// <summary>
        /// Nouvelle extension à ajouter à la liste
        /// </summary>
        public string NewExtension
        {
            get => _newExtension;
            set
            {
                _newExtension = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string> _encryptedExtensions;
        /// <summary>
        /// Collection des extensions de fichiers à chiffrer
        /// </summary>
        public ObservableCollection<string> EncryptedExtensions
        {
            get => _encryptedExtensions;
            set
            {
                _encryptedExtensions = value;
                OnPropertyChanged();
            }
        }

        private string _selectedExtension;
        /// <summary>
        /// Extension sélectionnée dans la liste
        /// </summary>
        public string SelectedExtension
        {
            get => _selectedExtension;
            set
            {
                _selectedExtension = value;
                OnPropertyChanged();
            }
        }

        private string _encryptionPassphrase;
        /// <summary>
        /// Phrase de passe pour le chiffrement des fichiers
        /// </summary>
        public string EncryptionPassphrase
        {
            get => _encryptionPassphrase;
            set
            {
                _encryptionPassphrase = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<ProcessInfo> _runningProcesses;
        /// <summary>
        /// Collection des processus en cours d'exécution
        /// </summary>
        public ObservableCollection<ProcessInfo> RunningProcesses
        {
            get => _runningProcesses;
            set
            {
                _runningProcesses = value;
                OnPropertyChanged();
            }
        }

        private ProcessInfo _selectedProcess;
        /// <summary>
        /// Processus sélectionné dans la liste
        /// </summary>
        public ProcessInfo SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                _selectedProcess = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPriorityProcessSelected));
            }
        }

        private string _priorityProcess;
        /// <summary>
        /// Processus prioritaire sélectionné
        /// </summary>
        public string PriorityProcess
        {
            get => _priorityProcess;
            set
            {
                _priorityProcess = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPriorityProcessSelected));
            }
        }

        /// <summary>
        /// Indique si un processus prioritaire est sélectionné
        /// </summary>
        public bool IsPriorityProcessSelected => !string.IsNullOrEmpty(_priorityProcess);

        /// <summary>
        /// Constructeur du ViewModel des paramètres
        /// </summary>
        public SettingsViewModel()
        {
            LanguageViewModel = LanguageViewModel.Instance;
            _configManager = ConfigManager.GetInstance();
            
            // Charger les extensions chiffrées depuis la configuration
            LoadEncryptedExtensions();
            
            // Charger la passphrase depuis la configuration
            LoadEncryptionPassphrase();

            // Charger le processus prioritaire
            LoadPriorityProcess();

            // Charger le format de log actuel depuis la configuration
            LoadLogFormat();

            // Initialiser la liste des processus en cours d'exécution
            LoadRunningProcesses();

            SaveCommand = new RelayCommand(() => {
                // Sauvegarder les extensions chiffrées
                SaveEncryptedExtensions();
                
                // Sauvegarder la passphrase
                SaveEncryptionPassphrase();
                
                // Sauvegarder le processus prioritaire
                SavePriorityProcess();
                
                // Sauvegarder le format de log
                SaveLogFormat();
                
                NavigateToMainMenu?.Invoke(this, EventArgs.Empty);
            });

            CancelCommand = new RelayCommand(() => {
                NavigateToMainMenu?.Invoke(this, EventArgs.Empty);
            });

            EnglishCommand = new RelayCommand(() => {
                LanguageViewModel.ChangeLanguage("english");
                OnPropertyChanged(nameof(CurrentLanguage));
            });

            FrenchCommand = new RelayCommand(() => {
                LanguageViewModel.ChangeLanguage("french");
                OnPropertyChanged(nameof(CurrentLanguage));
            });

            AddExtensionCommand = new RelayCommand(() => {
                if (!string.IsNullOrWhiteSpace(NewExtension))
                {
                    // S'assurer que l'extension commence par un point
                    string ext = NewExtension.Trim();
                    if (!ext.StartsWith("."))
                    {
                        ext = "." + ext;
                    }

                    // Ajouter l'extension à la liste si elle n'y est pas déjà
                    if (!EncryptedExtensions.Contains(ext))
                    {
                        EncryptedExtensions.Add(ext);
                    }

                    // Réinitialiser le champ
                    NewExtension = string.Empty;
                }
            });

            RemoveExtensionCommand = new RelayCommand(() => {
                if (!string.IsNullOrEmpty(SelectedExtension))
                {
                    EncryptedExtensions.Remove(SelectedExtension);
                    SelectedExtension = null;
                }
            }, () => SelectedExtension != null);

            RefreshProcessesCommand = new RelayCommand(() => {
                LoadRunningProcesses();
            });

            // Commande pour définir le format de log en XML
            SetXmlFormatCommand = new RelayCommand(() => {
                SelectedLogFormat = LogFormat.XML;
            });
            
            // Commande pour définir le format de log en JSON
            SetJsonFormatCommand = new RelayCommand(() => {
                SelectedLogFormat = LogFormat.JSON;
            });
        }

        /// <summary>
        /// Charge les processus en cours d'exécution
        /// </summary>
        public void LoadRunningProcesses()
        {
            try
            {
                var processes = Process.GetProcesses()
                    .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle) || IsCommonProcess(p.ProcessName))
                    .OrderBy(p => p.ProcessName)
                    .Select(p => new ProcessInfo(p.ProcessName, p.MainWindowTitle))
                    .ToList();

                RunningProcesses = new ObservableCollection<ProcessInfo>(processes);
                
                // Présélectionner le processus prioritaire s'il est en cours d'exécution
                if (!string.IsNullOrEmpty(PriorityProcess))
                {
                    var matchingProcess = RunningProcesses.FirstOrDefault(p => 
                        string.Equals(p.Name, PriorityProcess, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingProcess != null)
                    {
                        SelectedProcess = matchingProcess;
                    }
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur, afficher un message ou logger l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des processus : {ex.Message}");
                RunningProcesses = new ObservableCollection<ProcessInfo>();
            }
        }

        /// <summary>
        /// Détermine si un processus est courant/utilisé même s'il n'a pas de fenêtre principale
        /// </summary>
        private bool IsCommonProcess(string processName)
        {
            string[] commonProcesses = { "word", "excel", "powerpoint", "outlook", "chrome", "firefox", "edge", "notepad" };
            return commonProcesses.Any(p => processName.ToLower().Contains(p));
        }

        /// <summary>
        /// Définit le processus sélectionné comme prioritaire
        /// </summary>
        public void SetPriorityProcess()
        {
            if (SelectedProcess != null)
            {
                PriorityProcess = SelectedProcess.Name;
            }
        }

        /// <summary>
        /// Supprime le processus prioritaire
        /// </summary>
        public void ClearPriorityProcess()
        {
            PriorityProcess = string.Empty;
        }

        /// <summary>
        /// Charge les extensions chiffrées depuis la configuration
        /// </summary>
        private void LoadEncryptedExtensions()
        {
            //string extensionsStr = _configManager.GetSetting("EncryptedExtensions") ?? string.Empty;
            string extensionsStr = string.Empty;
            string[] extensions = extensionsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            EncryptedExtensions = new ObservableCollection<string>(extensions);
        }

        /// <summary>
        /// Sauvegarde les extensions chiffrées dans la configuration
        /// </summary>
        public void SaveEncryptedExtensions()
        {
            string extensionsStr = string.Join(",", EncryptedExtensions);
            //_configManager.SetSetting("EncryptedExtensions", extensionsStr);
        }

        /// <summary>
        /// Charge la passphrase de chiffrement depuis la configuration
        /// </summary>
        private void LoadEncryptionPassphrase()
        {
            //EncryptionPassphrase = _configManager.GetSetting("EncryptionPassphrase") ?? string.Empty;
            EncryptionPassphrase = string.Empty;
        }

        /// <summary>
        /// Sauvegarde la passphrase de chiffrement dans la configuration
        /// </summary>
        public void SaveEncryptionPassphrase()
        {
            //_configManager.SetSetting("EncryptionPassphrase", EncryptionPassphrase ?? string.Empty);

        }

        /// <summary>
        /// Charge le processus prioritaire depuis la configuration
        /// </summary>
        private void LoadPriorityProcess()
        {
            //PriorityProcess = _configManager.GetSetting("PriorityProcess") ?? string.Empty;
            PriorityProcess = string.Empty;
        }

        /// <summary>
        /// Sauvegarde le processus prioritaire dans la configuration
        /// </summary>
        public void SavePriorityProcess()
        {
            //_configManager.SetSetting("PriorityProcess", PriorityProcess ?? string.Empty);
        }

        /// <summary>
        /// Charge le format de log depuis la configuration
        /// </summary>
        private void LoadLogFormat()
        {
            string formatString = _configManager.GetSetting("LogFormat");
            if (!string.IsNullOrEmpty(formatString) && Enum.TryParse<LogFormat>(formatString, true, out LogFormat format))
            {
                SelectedLogFormat = format;
            }
            else
            {
                // Par défaut, utiliser XML
                SelectedLogFormat = LogFormat.XML;
            }
        }
        
        /// <summary>
        /// Sauvegarde le format de log dans la configuration
        /// </summary>
        public void SaveLogFormat()
        {
            _configManager.SetSetting("LogFormat", SelectedLogFormat.ToString());
            
            // Réinitialiser le singleton LogManager pour qu'il prenne en compte le nouveau format
            // Cette ligne peut être facultative selon l'architecture
            // LogManager.ResetInstance();
        }

        /// <summary>
        /// Notifie les abonnés du changement d'une propriété
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Classe représentant les informations d'un processus
    /// </summary>
    public class ProcessInfo
    {
        /// <summary>
        /// Nom du processus
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Titre de la fenêtre principale du processus
        /// </summary>
        public string WindowTitle { get; }

        /// <summary>
        /// Texte affiché dans l'interface utilisateur
        /// </summary>
        public string DisplayText => string.IsNullOrEmpty(WindowTitle) 
            ? Name 
            : $"{Name} - {WindowTitle}";

        public ProcessInfo(string name, string windowTitle)
        {
            Name = name;
            WindowTitle = windowTitle;
        }
    }
}
