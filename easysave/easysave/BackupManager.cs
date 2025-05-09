using System;
using System.Collections.Generic;
using System.IO;

namespace EasySave
{
    public class BackupManager
    {
        private static BackupManager instance;
        private List<BackupJob> backupJobs;
        private LanguageManager languageManager;
        private readonly Lazy<ConfigManager> lazyConfigManager = new(() => ConfigManager.GetInstance());
        private ConfigManager ConfigManager => lazyConfigManager.Value;


        private BackupManager()
        {
            backupJobs = new List<BackupJob>();
            languageManager = LanguageManager.GetInstance();
            // Charger la configuration
            ConfigManager.LoadConfiguration();


            // Créer le répertoire pour les fichiers de log et d'état s'ils n'existent pas
            string logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public static BackupManager GetInstance()
        {
            if (instance == null)
            {
                instance = new BackupManager();
            }
            return instance;
        }

        public void AddBackupJob(string name, string source, string target, string backupTypeStr)
        {
            BackupType type;
            if (Enum.TryParse(backupTypeStr, out type))
            {
                AddBackupJob(name, source, target, type);
            }
            else
            {
                Console.WriteLine($"Type de sauvegarde invalide: {backupTypeStr}");
            }
        }

        public bool AddBackupJob(string name, string source, string target, BackupType type)
        {
            // Vérifier si nous avons déjà 5 tâches de sauvegarde
            if (backupJobs.Count >= 5)
            {
                Console.WriteLine(languageManager.GetTranslation("MaxBackupJobsReached"));
                return false;
            }

            // Valider les répertoires source et cible
            if (!Directory.Exists(source))
            {
                Console.WriteLine(languageManager.GetTranslation("SourceDirNotFound"));
                return false;
            }

            // Créer le répertoire cible s'il n'existe pas
            if (!Directory.Exists(target))
            {
                try
                {
                    Directory.CreateDirectory(target);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{languageManager.GetTranslation("TargetDirCreateFailed")}: {ex.Message}");
                    return false;
                }
            }

            // Vérifier si une tâche avec le même nom existe déjà
            if (backupJobs.Exists(job => job.Name == name))
            {
                Console.WriteLine(languageManager.GetTranslation("JobNameExists"));
                return false;
            }

            // Créer la stratégie de sauvegarde appropriée
            AbstractBackupStrategy strategy = CreateBackupStrategy(type);

            // Créer et ajouter la tâche de sauvegarde
            BackupJob job = new BackupJob(name, source, target, type, strategy);

            // Configurer les observateurs pour la tâche
            string stateFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "state.json");

            string logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");

            StateManager stateManager = new StateManager(stateFilePath);
            LogManager logManager = new LogManager(logDirectory);

            job.AttachObserver(stateManager);
            job.AttachObserver(logManager);

            // Initialiser l'état de la tâche
            job.NotifyObservers("create");

            backupJobs.Add(job);

            // Enregistrer la liste de tâches de sauvegarde mise à jour dans la configuration
            SaveBackupJobsToConfig();

            return true;
        }

        public bool RemoveBackupJob(int index)
        {
            if (index >= 0 && index < backupJobs.Count)
            {
                backupJobs.RemoveAt(index);
                SaveBackupJobsToConfig();
                return true;
            }

            return false;
        }

        public bool UpdateBackupJob(int index, string name, string source, string target, BackupType type)
        {
            if (index < 0 || index >= backupJobs.Count)
            {
                return false;
            }

            // Valider les répertoires source et cible
            if (!Directory.Exists(source))
            {
                Console.WriteLine(languageManager.GetTranslation("SourceDirNotFound"));
                return false;
            }

            // Créer le répertoire cible s'il n'existe pas
            if (!Directory.Exists(target))
            {
                try
                {
                    Directory.CreateDirectory(target);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{languageManager.GetTranslation("TargetDirCreateFailed")}: {ex.Message}");
                    return false;
                }
            }

            // Vérifier si une tâche avec le même nom existe déjà (sauf pour la tâche actuelle)
            if (backupJobs.Exists(job => job.Name == name && backupJobs.IndexOf(job) != index))
            {
                Console.WriteLine(languageManager.GetTranslation("JobNameExists"));
                return false;
            }

            // Créer la stratégie de sauvegarde appropriée
            AbstractBackupStrategy strategy = CreateBackupStrategy(type);

            // Mettre à jour la tâche de sauvegarde
            BackupJob job = backupJobs[index];

            // Créer une nouvelle tâche avec les paramètres mis à jour
            BackupJob updatedJob = new BackupJob(name, source, target, type, strategy);

            // Copier les observateurs de l'ancienne tâche vers la nouvelle tâche
            foreach (var observer in job.Observers)
            {
                updatedJob.AttachObserver(observer);
            }

            // Remplacer l'ancienne tâche par la nouvelle
            backupJobs[index] = updatedJob;

            // Enregistrer la liste de tâches de sauvegarde mise à jour dans la configuration
            SaveBackupJobsToConfig();

            return true;
        }

        public List<BackupJob> ListBackups()
        {
            return backupJobs;
        }

        public void ExecuteBackupJob(List<int> backupIndices)
        {
            foreach (int index in backupIndices)
            {
                if (index >= 0 && index < backupJobs.Count)
                {
                    try
                    {
                        BackupJob job = backupJobs[index];
                        Console.WriteLine($"{languageManager.GetTranslation("ExecutingJob")}: {job.Name}");
                        job.Execute();
                        job.NotifyObservers("end");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{languageManager.GetTranslation("ErrorExecutingJob")}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"{languageManager.GetTranslation("InvalidJobIndex")}: {index + 1}");
                }
            }
        }

        private AbstractBackupStrategy CreateBackupStrategy(BackupType type)
        {
            string stateFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "state.json");

            string logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");

            StateManager stateManager = new StateManager(stateFilePath);
            LogManager logManager = new LogManager(logDirectory);

            switch (type)
            {
                case BackupType.Complete:
                    return new CompleteBackupStrategy(stateManager, logManager);
                case BackupType.Differential:
                    return new DifferentialBackupStrategy(stateManager, logManager);
                default:
                    return new CompleteBackupStrategy(stateManager, logManager);
            }
        }

        private void SaveBackupJobsToConfig()
        {
            // Obtenir l'instance de ConfigManager
            ConfigManager configManager = ConfigManager.GetInstance();

            // Préparer les données de configuration
            var configData = new
            {
                Settings = new
                {
                    Language = configManager.GetSetting("Language"),
                    MaxBackupJobs = configManager.GetSetting("MaxBackupJobs")
                },
                BackupJobs = backupJobs.ConvertAll(job => new
                {
                    Name = job.Name,
                    SourcePath = job.SourcePath,
                    TargetPath = job.TargetPath,
                    Type = job.Type.ToString(),
                    State = job.State.ToString(),
                    TotalFiles = job.TotalFiles,
                    TotalSize = job.TotalSize,
                    Progression = job.Progression,
                    LastFileTime = job.LastFileTime
                })
            };

            // Sérialiser les données en JSON
            string json = System.Text.Json.JsonSerializer.Serialize(configData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Définir le chemin du fichier de configuration
            string configFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "config.json");

            // Écrire les données dans le fichier
            File.WriteAllText(configFilePath, json);
        }

    }
}
