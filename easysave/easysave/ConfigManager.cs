using EasySave;
using System.Text.Json;

public class ConfigManager
{
    private static readonly Lazy<ConfigManager> lazyInstance = new(() => new ConfigManager());
    private readonly string configFilePath;
    private Dictionary<string, string> settings;
    private ConfigDataWithJobs configData; // Store configuration data for deferred processing

    private ConfigManager()
    {
        // Define the path to the configuration file in the user's application data folder
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        configFilePath = Path.Combine(appDataPath, "EasySave", "config.json");
        LoadConfiguration();
    }

    // Singleton pattern to ensure a single instance of ConfigManager
    public static ConfigManager GetInstance()
    {
        return lazyInstance.Value;
    }

    // Load configuration from the JSON file
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

                    // Deserialize configuration data with backup jobs
                    configData = JsonSerializer.Deserialize<ConfigDataWithJobs>(json, options);
                    settings = configData.Settings;
                }
                else
                {
                    // Deserialize simple settings dictionary
                    settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                }

                return true;
            }
            else
            {
                // Create default configuration if the file does not exist
                settings = new Dictionary<string, string>
                {
                    { "Language", "fr" },
                    { "MaxBackupJobs", "5" },
                    { "LogFormat", "XML" }  // Valeur par défaut: XML
                };
                SaveConfiguration();
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while loading configuration: {ex.Message}");
            settings = new Dictionary<string, string>();
            return false;
        }
    }

    // Load backup jobs from the configuration data
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
                        // Add jobs to the BackupManager
                        BackupManager.GetInstance().AddBackupJob(job.Name, job.SourcePath, job.TargetPath, backupType);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while loading job {job.Name}: {ex.Message}");
                }
            }
        }
    }

    // Save the current configuration to the JSON file
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

            // Serialize settings to JSON and write to file
            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(configFilePath, json);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while saving configuration: {ex.Message}");
            return false;
        }
    }

    // Retrieve a specific setting by key
    public string GetSetting(string key)
    {
        if (settings != null && settings.ContainsKey(key))
        {
            return settings[key];
        }
        return null;
    }

    // Update or add a setting and save the configuration
    public void SetSetting(string key, string value)
    {
        settings[key] = value;
        SaveConfiguration();
    }

    // Class to represent configuration data with backup jobs
    private class ConfigDataWithJobs
    {
        public Dictionary<string, string> Settings { get; set; }
        public List<BackupJobData> BackupJobs { get; set; }
    }

    // Class to represent individual backup job data
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
