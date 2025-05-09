using EasySave;
using System.Text.Json;

public class ConfigManager
{
    private static readonly Lazy<ConfigManager> lazyInstance = new(() => new ConfigManager());
    private readonly string configFilePath;
    private Dictionary<string, string> settings;
    private ConfigDataWithJobs configData; // Stocker les données de configuration pour un traitement différé

    private ConfigManager()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        configFilePath = Path.Combine(appDataPath, "EasySave", "config.json");
        LoadConfiguration();
    }

    public static ConfigManager GetInstance()
    {
        return lazyInstance.Value;
    }

    public bool LoadConfiguration()
    {
        try
        {
            if (File.Exists(configFilePath))
            {
                string json = File.ReadAllText(configFilePath);

                if (json.Contains("\"Settings\":"))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    // Désérialiser les données de configuration
                    configData = JsonSerializer.Deserialize<ConfigDataWithJobs>(json, options);
                    settings = configData.Settings;
                }
                else
                {
                    settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                }

                return true;
            }
            else
            {
                // Créer une configuration par défaut si le fichier n'existe pas
                settings = new Dictionary<string, string>
                {
                    { "Language", "fr" },
                    { "MaxBackupJobs", "5" }
                };
                SaveConfiguration();
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du chargement de la configuration: {ex.Message}");
            settings = new Dictionary<string, string>();
            return false;
        }
    }

    public void LoadBackupJobs()
    {
        if (configData?.BackupJobs != null)
        {
            foreach (var job in configData.BackupJobs)
            {
                try
                {
                    BackupType backupType;
                    if (Enum.TryParse(job.Type, out backupType))
                    {
                        // Ajouter les jobs au BackupManager
                        BackupManager.GetInstance().AddBackupJob(job.Name, job.SourcePath, job.TargetPath, backupType);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors du chargement du job {job.Name}: {ex.Message}");
                }
            }
        }
    }

    public bool SaveConfiguration()
    {
        try
        {
            string directory = Path.GetDirectoryName(configFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(configFilePath, json);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'enregistrement de la configuration: {ex.Message}");
            return false;
        }
    }

    public string GetSetting(string key)
    {
        if (settings != null && settings.ContainsKey(key))
        {
            return settings[key];
        }
        return null;
    }

    public void SetSetting(string key, string value)
    {
        settings[key] = value;
        SaveConfiguration();
    }

    private class ConfigDataWithJobs
    {
        public Dictionary<string, string> Settings { get; set; }
        public List<BackupJobData> BackupJobs { get; set; }
    }

    private class BackupJobData
    {
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public string Type { get; set; }
        public string State { get; set; }
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public int Progression { get; set; }
        public long LastFileTime { get; set; }
    }
}
