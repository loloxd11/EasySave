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
    /// ViewModel for the application's settings page.
    /// Handles language, encryption, logging format, and process priority settings.
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Event triggered when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Singleton instance of the language ViewModel for managing translations.
        /// </summary>
        public LanguageViewModel LanguageViewModel { get; }

        /// <summary>
        /// Instance of the configuration manager.
        /// </summary>
        private readonly ConfigManager _configManager;

        /// <summary>
        /// Instance of the LogManager.
        /// </summary>
        private readonly LogManager _logManager;

        /// <summary>
        /// Event to navigate back to the main menu.
        /// </summary>
        public event EventHandler NavigateToMainMenu;

        /// <summary>
        /// Command to save settings and return to the main menu.
        /// </summary>
        public ICommand SaveCommand { get; set; }

        /// <summary>
        /// Command to cancel and return to the main menu.
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Command to change the language to English.
        /// </summary>
        public ICommand EnglishCommand { get; }

        /// <summary>
        /// Command to change the language to French.
        /// </summary>
        public ICommand FrenchCommand { get; }

        /// <summary>
        /// Command to add a file extension to the encrypted list.
        /// </summary>
        public ICommand AddExtensionCommand { get; }

        /// <summary>
        /// Command to remove a file extension from the encrypted list.
        /// </summary>
        public ICommand RemoveExtensionCommand { get; }

        /// <summary>
        /// Command to refresh the list of running processes.
        /// </summary>
        public ICommand RefreshProcessesCommand { get; }

        /// <summary>
        /// Command to set the log format to XML.
        /// </summary>
        public ICommand SetXmlFormatCommand { get; }

        /// <summary>
        /// Command to set the log format to JSON.
        /// </summary>
        public ICommand SetJsonFormatCommand { get; }

        /// <summary>
        /// Command to add a priority file extension to the list.
        /// </summary>
        public ICommand AddPriorityExtensionCommand { get; }

        /// <summary>
        /// Command to remove a priority file extension from the list.
        /// </summary>
        public ICommand RemovePriorityExtensionCommand { get; }

        /// <summary>
        /// Current application language.
        /// </summary>
        public string CurrentLanguage => LanguageViewModel.CurrentLanguage;

        private LogFormat _selectedLogFormat;
        /// <summary>
        /// Selected log format.
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
        /// Indicates if XML format is selected.
        /// </summary>
        public bool IsXmlSelected => _selectedLogFormat == LogFormat.XML;

        /// <summary>
        /// Indicates if JSON format is selected.
        /// </summary>
        public bool IsJsonSelected => _selectedLogFormat == LogFormat.JSON;

        private string _newExtension;
        /// <summary>
        /// New extension to add to the encrypted list.
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
        /// Collection of file extensions to encrypt.
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
        /// Selected extension in the list.
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
        /// Passphrase for file encryption.
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
        /// Collection of currently running processes.
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
        /// Selected process in the list.
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
        /// Selected priority process.
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
        /// Indicates if a priority process is selected.
        /// </summary>
        public bool IsPriorityProcessSelected => !string.IsNullOrEmpty(_priorityProcess);

        private ObservableCollection<string> _priorityExtensions;
        /// <summary>
        /// Collection of priority file extensions.
        /// </summary>
        public ObservableCollection<string> PriorityExtensions
        {
            get => _priorityExtensions;
            set
            {
                _priorityExtensions = value;
                OnPropertyChanged();
            }
        }

        private string _newPriorityExtension;
        /// <summary>
        /// New priority extension to add to the list.
        /// </summary>
        public string NewPriorityExtension
        {
            get => _newPriorityExtension;
            set
            {
                _newPriorityExtension = value;
                OnPropertyChanged();
            }
        }

        private string _selectedPriorityExtension;
        /// <summary>
        /// Selected priority extension in the list.
        /// </summary>
        public string SelectedPriorityExtension
        {
            get => _selectedPriorityExtension;
            set
            {
                _selectedPriorityExtension = value;
                OnPropertyChanged();
            }
        }

        private int _maxParallelSizeKB;
        /// <summary>
        /// Maximum parallel size in KB.
        /// </summary>
        public int MaxParallelSizeKB
        {
            get => _maxParallelSizeKB;
            set
            {
                _maxParallelSizeKB = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Constructor for the settings ViewModel.
        /// Initializes commands and loads configuration values.
        /// </summary>
        public SettingsViewModel()
        {
            LanguageViewModel = LanguageViewModel.Instance;
            _configManager = ConfigManager.GetInstance();
            _logManager = LogManager.GetInstance();

            // Load encrypted extensions from configuration
            LoadEncryptedExtensions();

            // Load encryption passphrase from configuration
            LoadEncryptionPassphrase();

            // Load priority process from configuration
            LoadPriorityProcess();

            // Load current log format from configuration
            LoadLogFormat();

            // Load priority extensions from configuration
            LoadPriorityExtensions();

            // Load max parallel size from configuration
            LoadMaxParallelSizeKB();

            // Initialize the list of running processes
            LoadRunningProcesses();

            SaveCommand = new RelayCommand(() =>
            {
                // Save encrypted extensions
                SaveEncryptedExtensions();

                // Save passphrase
                SaveEncryptionPassphrase();

                // Save priority process
                SavePriorityProcess();

                // Save log format
                SaveLogFormat();

                // Save priority extensions
                SavePriorityExtensions();

                // Save max parallel size
                SaveMaxParallelSizeKB();

                NavigateToMainMenu?.Invoke(this, EventArgs.Empty);
            });

            CancelCommand = new RelayCommand(() =>
            {
                NavigateToMainMenu?.Invoke(this, EventArgs.Empty);
            });

            EnglishCommand = new RelayCommand(() =>
            {
                LanguageViewModel.ChangeLanguage("english");
                OnPropertyChanged(nameof(CurrentLanguage));
            });

            FrenchCommand = new RelayCommand(() =>
            {
                LanguageViewModel.ChangeLanguage("french");
                OnPropertyChanged(nameof(CurrentLanguage));
            });

            AddExtensionCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrWhiteSpace(NewExtension))
                {
                    // Ensure the extension starts with a dot
                    string ext = NewExtension.Trim();
                    if (!ext.StartsWith("."))
                    {
                        ext = "." + ext;
                    }

                    // Add the extension if not already present
                    if (!EncryptedExtensions.Contains(ext))
                    {
                        EncryptedExtensions.Add(ext);
                    }

                    // Reset the input field
                    NewExtension = string.Empty;
                }
            });

            RemoveExtensionCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrEmpty(SelectedExtension))
                {
                    EncryptedExtensions.Remove(SelectedExtension);
                    SelectedExtension = null;
                }
            }, () => SelectedExtension != null);

            RefreshProcessesCommand = new RelayCommand(() =>
            {
                LoadRunningProcesses();
            });

            // Command to set log format to XML
            SetXmlFormatCommand = new RelayCommand(() =>
            {
                SelectedLogFormat = LogFormat.XML;
            });

            // Command to set log format to JSON
            SetJsonFormatCommand = new RelayCommand(() =>
            {
                SelectedLogFormat = LogFormat.JSON;
            });

            AddPriorityExtensionCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrWhiteSpace(NewPriorityExtension))
                {
                    string ext = NewPriorityExtension.Trim();
                    if (!ext.StartsWith(".")) ext = "." + ext;
                    if (!PriorityExtensions.Contains(ext))
                        PriorityExtensions.Add(ext);
                    NewPriorityExtension = string.Empty;
                }
            });

            RemovePriorityExtensionCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrEmpty(SelectedPriorityExtension))
                {
                    PriorityExtensions.Remove(SelectedPriorityExtension);
                    SelectedPriorityExtension = null;
                }
            }, () => SelectedPriorityExtension != null);
        }

        /// <summary>
        /// Loads the list of currently running processes.
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

                // Preselect the priority process if it is running
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
                // Log or display error if process loading fails
                System.Diagnostics.Debug.WriteLine($"Error loading processes: {ex.Message}");
                RunningProcesses = new ObservableCollection<ProcessInfo>();
            }
        }

        /// <summary>
        /// Determines if a process is common/used even if it has no main window.
        /// </summary>
        /// <param name="processName">The process name.</param>
        /// <returns>True if the process is common, otherwise false.</returns>
        private bool IsCommonProcess(string processName)
        {
            string[] commonProcesses = { "word", "excel", "powerpoint", "outlook", "chrome", "firefox", "edge", "notepad" };
            return commonProcesses.Any(p => processName.ToLower().Contains(p));
        }

        /// <summary>
        /// Sets the selected process as the priority process.
        /// </summary>
        public void SetPriorityProcess()
        {
            if (SelectedProcess != null)
            {
                PriorityProcess = SelectedProcess.Name;
            }
        }

        /// <summary>
        /// Clears the priority process.
        /// </summary>
        public void ClearPriorityProcess()
        {
            PriorityProcess = string.Empty;
        }

        /// <summary>
        /// Loads encrypted file extensions from configuration.
        /// </summary>
        private void LoadEncryptedExtensions()
        {
            EncryptionService encryptionService = EncryptionService.GetInstance();
            var extensions = encryptionService.GetEncryptedExtensions();
            EncryptedExtensions = new ObservableCollection<string>(extensions);
        }

        /// <summary>
        /// Saves encrypted file extensions to configuration.
        /// </summary>
        public void SaveEncryptedExtensions()
        {
            EncryptionService encryptionService = EncryptionService.GetInstance();
            encryptionService.SetEncryptedExtensions(EncryptedExtensions);
        }

        /// <summary>
        /// Saves the encryption passphrase to configuration.
        /// </summary>
        public void SaveEncryptionPassphrase()
        {
            if (!string.IsNullOrWhiteSpace(EncryptionPassphrase))
            {
                EncryptionService.GetInstance().SetEncryptionPassword(EncryptionPassphrase);
            }
        }

        /// <summary>
        /// Loads the encryption passphrase from configuration.
        /// For security, the passphrase is not displayed in the UI.
        /// </summary>
        private void LoadEncryptionPassphrase()
        {
            // The passphrase is already hashed and stored, do not display the hash in the UI!
            // Leave the field empty for security reasons.
            bool hasPassword = !string.IsNullOrEmpty(_configManager.GetSetting("EncryptionPassword"));

            if (hasPassword)
            {
                // Inform the user that a password is already set, but do not display it.
                EncryptionPassphrase = string.Empty;
            }
            else
            {
                EncryptionPassphrase = string.Empty;
            }
        }

        /// <summary>
        /// Loads the priority process from configuration.
        /// </summary>
        private void LoadPriorityProcess()
        {
            PriorityProcess = _configManager.GetSetting("PriorityProcess") ?? string.Empty;
        }

        /// <summary>
        /// Saves the priority process to configuration.
        /// </summary>
        public void SavePriorityProcess()
        {
            _configManager.SetSetting("PriorityProcess", PriorityProcess ?? string.Empty);
        }

        /// <summary>
        /// Loads the log format from configuration.
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
                // Default to XML
                SelectedLogFormat = LogFormat.XML;
            }
        }

        /// <summary>
        /// Saves the log format to configuration and updates the LogManager.
        /// </summary>
        public void SaveLogFormat()
        {
            _configManager.SetSetting("LogFormat", SelectedLogFormat.ToString());
            // Optionally reset the LogManager singleton if needed by architecture
            // LogManager.ResetInstance();
            _logManager.SetFormat(SelectedLogFormat);
        }

        /// <summary>
        /// Loads priority file extensions from configuration.
        /// </summary>
        private void LoadPriorityExtensions()
        {
            string extStr = _configManager.GetSetting("PriorityExtensions") ?? string.Empty;
            var list = extStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant()).ToList();
            PriorityExtensions = new ObservableCollection<string>(list);
        }

        /// <summary>
        /// Saves priority file extensions to configuration.
        /// </summary>
        public void SavePriorityExtensions()
        {
            string extStr = string.Join(",", PriorityExtensions);
            _configManager.SetSetting("PriorityExtensions", extStr);
        }

        /// <summary>
        /// Loads the maximum parallel size in KB from configuration.
        /// </summary>
        private void LoadMaxParallelSizeKB()
        {
            if (int.TryParse(_configManager.GetSetting("MaxParallelSizeKB"), out int n))
                MaxParallelSizeKB = n;
            else
                MaxParallelSizeKB = 1024;
        }

        /// <summary>
        /// Saves the maximum parallel size in KB to configuration.
        /// </summary>
        public void SaveMaxParallelSizeKB()
        {
            _configManager.SetSetting("MaxParallelSizeKB", MaxParallelSizeKB.ToString());
        }

        /// <summary>
        /// Notifies subscribers of a property value change.
        /// </summary>
        /// <param name="name">The property name.</param>
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Represents information about a process.
    /// </summary>
    public class ProcessInfo
    {
        /// <summary>
        /// Process name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Main window title of the process.
        /// </summary>
        public string WindowTitle { get; }

        /// <summary>
        /// Text displayed in the UI.
        /// </summary>
        public string DisplayText => string.IsNullOrEmpty(WindowTitle)
            ? Name
            : $"{Name} - {WindowTitle}";

        /// <summary>
        /// Constructor for ProcessInfo.
        /// </summary>
        /// <param name="name">Process name.</param>
        /// <param name="windowTitle">Main window title.</param>
        public ProcessInfo(string name, string windowTitle)
        {
            Name = name;
            WindowTitle = windowTitle;
        }
    }
}
