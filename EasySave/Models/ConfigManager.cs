using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public class ConfigManager
    {
        private static Lazy<ConfigManager> lazyInstance = new Lazy<ConfigManager>(() => new ConfigManager());
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
                public string Target { get; set; }
                public BackupType Type { get; set; }
            }
        }

        private ConfigManager()
        {
            configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            settings = new Dictionary<string, string>();
            configData = new ConfigDataWithJobs();
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
                    configData = JsonSerializer.Deserialize<ConfigDataWithJobs>(json)
                        ?? new ConfigDataWithJobs();
                    settings = configData.Settings;

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
            // Logic to load backup jobs from configuration
            // This would typically interact with the BackupManager
            BackupManager manager = BackupManager.GetInstance();

            // Implementation would create backup jobs based on configData.BackupJobs
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

                // Update backup jobs in config data
                // This would typically be called from BackupManager

                string json = JsonSerializer.Serialize(configData);
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

            return null;
        }

        public void SetSetting(string key, string value)
        {
            settings[key] = value;
            SaveConfiguration();
        }
    }
}
