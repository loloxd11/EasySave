using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave
{
    public class ConfigManager
    {
        private static ConfigManager instance;
        private readonly string configFilePath;
        private Dictionary<string, string> settings;

        private ConfigManager()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            configFilePath = Path.Combine(appDataPath, "EasySave", "config.json");
            LoadConfiguration();
        }

        public static ConfigManager GetInstance()
        {
            if (instance == null)
            {
                instance = new ConfigManager();
            }
            return instance;
        }

        public bool LoadConfiguration()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string json = File.ReadAllText(configFilePath);
                    settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    return true;
                }
                else
                {
                    // Initialiser avec des paramètres par défaut si le fichier n'existe pas
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

        public bool SaveConfiguration()
        {
            try
            {
                // S'assurer que le répertoire existe
                string directory = Path.GetDirectoryName(configFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true // Pour l'affichage avec des sauts de ligne
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
    }
}
