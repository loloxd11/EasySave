using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasySave.Models
{
    /// <summary>
    /// Singleton class responsible for managing application configuration,
    /// including settings and backup jobs. Handles loading and saving configuration
    /// from/to a JSON file in the user's AppData directory.
    /// </summary>
    public class ConfigManager
    {
        private static ConfigManager _instance;
        private string configFilePath;
        private Dictionary<string, string> settings;
        private ConfigDataWithJobs configData;

        /// <summary>
        /// Internal class representing the structure of the configuration file,
        /// including application settings and backup jobs.
        /// </summary>
        private class ConfigDataWithJobs
        {
            /// <summary>
            /// Dictionary of application settings (key-value pairs).
            /// </summary>
            public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
            /// <summary>
            /// List of backup job configurations.
            /// </summary>
            public List<BackupJobConfig> BackupJobs { get; set; } = new List<BackupJobConfig>();

            /// <summary>
            /// Represents the configuration for a single backup job.
            /// </summary>
            public class BackupJobConfig
            {
                /// <summary>
                /// Name of the backup job.
                /// </summary>
                public string Name { get; set; }
                /// <summary>
                /// Source directory path.
                /// </summary>
                public string Source { get; set; }
                /// <summary>
                /// Destination directory path.
                /// </summary>
                public string Destination { get; set; }
                /// <summary>
                /// Type of backup (Complete or Differential).
                /// </summary>
                public BackupType Type { get; set; }
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// Initializes configuration file path and data structures.
        /// </summary>
        private ConfigManager()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            configFilePath = Path.Combine(appDataPath, "EasySave", "config.json");
            settings = new Dictionary<string, string>();
            configData = new ConfigDataWithJobs();
        }

        /// <summary>
        /// Gets the singleton instance of ConfigManager.
        /// </summary>
        /// <returns>The singleton ConfigManager instance.</returns>
        public static ConfigManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ConfigManager();
            }
            return _instance;
        }

        /// <summary>
        /// Loads the configuration from the JSON file.
        /// Populates settings and backup jobs from the file.
        /// </summary>
        /// <returns>True if configuration was loaded successfully, false otherwise.</returns>
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

        /// <summary>
        /// Loads backup jobs from the configuration and adds them to the BackupManager.
        /// </summary>
        public void LoadBackupJobs()
        {
            // Get the instance of BackupManager
            BackupManager manager = BackupManager.GetInstance();

            // Check if backup jobs exist in the configuration
            if (configData.BackupJobs != null && configData.BackupJobs.Count > 0)
            {
                // Iterate through each backup job in the configuration
                foreach (var jobConfig in configData.BackupJobs)
                {
                    // Ensure required data is present
                    if (!string.IsNullOrEmpty(jobConfig.Name) &&
                        !string.IsNullOrEmpty(jobConfig.Source) &&
                        !string.IsNullOrEmpty(jobConfig.Destination))
                    {
                        // Add the job to the BackupManager
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

        /// <summary>
        /// Loads the log format setting from the configuration and applies it to the LogManager.
        /// </summary>
        public void LoadLogFormat()
        {
            // Logic to load log format from configuration
            if (settings.TryGetValue("LogFormat", out string formatStr))
            {
                if (Enum.TryParse<LogFormat>(formatStr, out LogFormat format))
                {
                    // Set the log format in the LogManager
                    // object value = LogManager.GetInstance("logs").SetFormat(format);
                }
            }
        }

        /// <summary>
        /// Saves the current configuration (settings and backup jobs) to the JSON file.
        /// </summary>
        /// <returns>True if configuration was saved successfully, false otherwise.</returns>
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

                // Ensure the directory exists
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

        /// <summary>
        /// Gets the value of a setting by key.
        /// </summary>
        /// <param name="key">The setting key.</param>
        /// <returns>The setting value, or an empty string if not found.</returns>
        public string GetSetting(string key)
        {
            if (settings.TryGetValue(key, out string value))
            {
                return value;
            }
            return string.Empty;
        }

        /// <summary>
        /// Sets the value of a setting and saves the configuration.
        /// </summary>
        /// <param name="key">The setting key.</param>
        /// <param name="value">The setting value.</param>
        public void SetSetting(string key, string value)
        {
            settings[key] = value;
            SaveConfiguration();
        }
    }
}
