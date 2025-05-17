using EasySave;
using System.Text.Json;

/// <summary>
/// Manages the configuration settings and backup jobs for the EasySave application.
/// Implements a singleton pattern to ensure a single instance of the manager.
/// </summary>
public class ConfigManager
{
    private static readonly Lazy<ConfigManager> lazyInstance = new(() => new ConfigManager());
    private readonly string configFilePath;
    private Dictionary<string, string> settings;
    private ConfigDataWithJobs configData; // Stores configuration data for deferred processing

    /// <summary>
    /// Private constructor to initialize the configuration manager.
    /// Defines the path to the configuration file and loads the configuration.
    /// </summary>
    private ConfigManager()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        configFilePath = Path.Combine(appDataPath, "EasySave", "config.json");
        LoadConfiguration();
    }

    /// <summary>
    /// Retrieves the singleton instance of the ConfigManager.
    /// </summary>
    /// <returns>The single instance of ConfigManager.</returns>
    public static ConfigManager GetInstance()
    {
        return lazyInstance.Value;
    }

    /// <summary>
    /// Loads the configuration from the JSON file.
    /// If the file does not exist, creates a default configuration.
    /// </summary>
    /// <returns>True if the configuration is successfully loaded or created, false otherwise.</returns>
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
                    { "LogFormat", "XML" }  // Default value: XML
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

    /// <summary>
    /// Loads backup jobs from the configuration data and adds them to the BackupManager.
    /// </summary>
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

    /// <summary>
    /// Loads the log format setting and applies it to the LogManager.
    /// </summary>
    public void LoadLogFormat()
    {
        if (configData?.Settings != null)
        {
            string logFormat = configData.Settings["LogFormat"];
            if (logFormat.Equals("XML", StringComparison.OrdinalIgnoreCase))
            {
                LogManager.GetInstance().SetFormat(LogLibrary.Enums.LogFormat.XML);
            }
            else if (logFormat.Equals("JSON", StringComparison.OrdinalIgnoreCase))
            {
                LogManager.GetInstance().SetFormat(LogLibrary.Enums.LogFormat.JSON);
            }
        }
    }

    /// <summary>
    /// Saves the current configuration and backup jobs to the JSON file.
    /// </summary>
    /// <returns>True if the configuration is successfully saved, false otherwise.</returns>
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

            // Reload backup jobs from the BackupManager
            var backupManager = BackupManager.GetInstance();
            var currentJobs = backupManager.GetBackupJobs(); // Method to implement in BackupManager

            // Update configData with the current jobs
            configData.BackupJobs = currentJobs.Select(job => new ConfigDataWithJobs.BackupJobData
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
            }).ToList();

            // Serialize the combined object to JSON and write to the file
            string json = JsonSerializer.Serialize(configData, options);
            File.WriteAllText(configFilePath, json);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while saving configuration: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Retrieves a specific setting by its key.
    /// </summary>
    /// <param name="key">The key of the setting to retrieve.</param>
    /// <returns>The value of the setting, or null if the key does not exist.</returns>
    public string GetSetting(string key)
    {
        if (settings != null && settings.ContainsKey(key))
        {
            return settings[key];
        }
        return null;
    }

    /// <summary>
    /// Updates or adds a setting and saves the configuration.
    /// </summary>
    /// <param name="key">The key of the setting to update or add.</param>
    /// <param name="value">The value of the setting to update or add.</param>
    public void SetSetting(string key, string value)
    {
        settings[key] = value;
        SaveConfiguration();
    }

    /// <summary>
    /// Represents configuration data with backup jobs.
    /// </summary>
    private class ConfigDataWithJobs
    {
        public Dictionary<string, string> Settings { get; set; }
        public List<BackupJobData> BackupJobs { get; set; }

        /// <summary>
        /// Represents the data structure for a backup job.
        /// </summary>
        internal class BackupJobData
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
}
