using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public class ConfigManager
    {
        private static ConfigManager _instance;
        private string configFilePath;
        private Dictionary<string, string> settings;
        private ConfigDataWithJobs configData;

        private class ConfigDataWithJobs
        {
            public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
            public List<BackupJobConfig> BackupJobs { get; set; } = new List<BackupJobConfig>();

            public class BackupJobConfig
            {
                public string Name { get; set; }
                public string Source { get; set; }
                public string Destination { get; set; }
                public BackupType Type { get; set; }
            }
        }

        private ConfigManager()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            configFilePath = Path.Combine(appDataPath, "EasySave", "config.json");
            settings = new Dictionary<string, string>();
            configData = new ConfigDataWithJobs();
        }

        public static ConfigManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ConfigManager();
            }
            return _instance;
        }

        public bool LoadConfiguration()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string json = File.ReadAllText(configFilePath);
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
                    };
                    configData = JsonSerializer.Deserialize<ConfigDataWithJobs>(json, options) ?? new ConfigDataWithJobs();
                    settings = configData.Settings;

                    Console.WriteLine(configData.BackupJobs.Count + " backup jobs loaded.");



                    LoadBackupJobs();
                    LoadLogFormat();

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                return false;
            }
        }

        public void LoadBackupJobs()
        {
            // Récupérer l'instance du BackupManager
            BackupManager manager = BackupManager.GetInstance();

            // Vérifier si des jobs de sauvegarde existent dans la configuration
            if (configData.BackupJobs != null && configData.BackupJobs.Count > 0)
            {
                // Parcourir chaque tâche de sauvegarde dans la configuration
                foreach (var jobConfig in configData.BackupJobs)
                {
                    // Vérifier que les données nécessaires sont présentes
                    if (!string.IsNullOrEmpty(jobConfig.Name) &&
                        !string.IsNullOrEmpty(jobConfig.Source) &&
                        !string.IsNullOrEmpty(jobConfig.Destination))
                    {
                        // Ajouter la tâche au BackupManager
                        manager.AddBackupJob(
                            jobConfig.Name,
                            jobConfig.Source,
                            jobConfig.Destination,
                            jobConfig.Type
                        );
                    }
                }
            }
        }

        public void LoadLogFormat()
        {
            // Logic to load log format from configuration
            if (settings.TryGetValue("LogFormat", out string formatStr))
            {
                if (Enum.TryParse<LogFormat>(formatStr, out LogFormat format))
                {
                    // Set the log format in the LogManager
                    //object value = LogManager.GetInstance("logs").SetFormat(format);
                }
            }
        }

        public bool SaveConfiguration()
        {
            try
            {
                configData.Settings = settings;
                configData.BackupJobs = BackupManager.GetInstance().ListBackups()
                    .Select(job => new ConfigDataWithJobs.BackupJobConfig
                    {
                        Name = job.Name,
                        Source = job.Source,
                        Destination = job.Destination,
                        Type = job.Type
                    }).ToList();

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
                };

                string json = JsonSerializer.Serialize(configData, options);

                // Assurez-vous que le répertoire existe
                Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
                File.WriteAllText(configFilePath, json);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
                return false;
            }
        }

        public string GetSetting(string key)
        {
            if (settings.TryGetValue(key, out string value))
            {
                return value;
            }
            return string.Empty;
        }

        public void SetSetting(string key, string value)
        {
            settings[key] = value;
            SaveConfiguration();
        }

        /// <summary>
        /// Vérifie si le processus prioritaire configuré est en cours d'exécution
        /// </summary>
        /// <returns>True si le processus prioritaire est en cours d'exécution, sinon False</returns>
        public bool PriorityProcessIsRunning()
        {
            string priorityProcess = GetSetting("PriorityProcess");
            if (string.IsNullOrWhiteSpace(priorityProcess))
                return false;

            try
            {
                // Si l'extension .exe est incluse, la supprimer
                if (priorityProcess.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    priorityProcess = priorityProcess.Substring(0, priorityProcess.Length - 4);

                // Récupérer tous les processus en cours d'exécution
                var processes = Process.GetProcesses();

                // Vérifier si le processus prioritaire est en cours d'exécution
                return processes.Any(p => string.Equals(p.ProcessName, priorityProcess, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification du processus prioritaire : {ex.Message}");
                return false;
            }
        }
    }
}
