using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EasySave.Views;

namespace EasySave.Models
{
    public class StateManager : IObserver
    {
        private List<IStateObserver> stateObservers;
        private static StateManager instance;
        private string stateFilePath;
        private Dictionary<string, JobStateInfo> stateData;
        private readonly object lockObject = new object();

        // Dans Models/StateManager.cs
        private class JobStateInfo
        {
            public string Name { get; set; }
            public BackupType Type { get; set; }

            [JsonConverter(typeof(JsonStringEnumConverter))]
            public JobState State { get; set; }

            public string SourcePath { get; set; }
            public string TargetPath { get; set; }
            public int TotalFiles { get; set; }      // Nombre total de fichiers à copier (statique)
            public long TotalSize { get; set; }
            public int Progression { get; set; }     // En pourcentage (0-100)
        }


        private StateManager(string path)
        {
            stateObservers = new List<IStateObserver>();
            stateFilePath = path;
            stateData = new Dictionary<string, JobStateInfo>();
            LoadStateFile();
        }

        public static StateManager GetInstance()
        {
            if (instance == null)
            {
                string defaultPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EasySave", "state.json");
                instance = new StateManager(defaultPath);
            }
            return instance;
        }

        public void Update(string action, string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, int progression)
        {
            lock (lockObject)
            {
                Console.WriteLine($"Received update: action={action}, name={name}, progression={progression}");

                if (action == "start")
                {
                    InitializeJobState(name, type, state, sourcePath, targetPath, totalFiles, totalSize, progression);
                }
                else if (action == "complete" || action == "error")
                {
                    FinalizeJobState(name, type, state, sourcePath, targetPath, totalFiles, totalSize, progression);
                }
                else if (action == "transfer" || action == "processing")  // Assurez-vous que "transfer" est bien traité
                {
                    UpdateJobState(name, type, state, sourcePath, targetPath, totalFiles, totalSize, progression);
                }
                else
                {
                    UpdateJobState(name, type, state, sourcePath, targetPath, totalFiles, totalSize, progression);
                }

                 NotifyObservers();
            }
        }


        public void UpdateJobState(string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, int progression)
        {
            JobStateInfo jobState = GetOrCreateJobState(name);

            jobState.Type = type;
            jobState.State = state;
            jobState.SourcePath = sourcePath;
            jobState.TargetPath = targetPath;

            // Assurez-vous que TotalFiles soit toujours initialisé avec la valeur correcte
            // et ne change pas pendant l'exécution
            if (jobState.TotalFiles == 0 && totalFiles > 0)
            {
                jobState.TotalFiles = totalFiles;
            }

            jobState.TotalSize = totalSize;

            // Calculer la progression en pourcentage
            jobState.Progression = (int)Math.Min(100, Math.Round((double)progression / jobState.TotalFiles * 100));

            SaveStateFile();
        }


        public void InitializeJobState(string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, int progression)
        {
            JobStateInfo jobState = GetOrCreateJobState(name);

            jobState.Type = type;
            jobState.State = JobState.active;
            jobState.SourcePath = sourcePath;
            jobState.TargetPath = targetPath;
            jobState.TotalFiles = totalFiles;
            jobState.TotalSize = totalSize;
            jobState.Progression = 0; // Progression initiale à 0%

            SaveStateFile();
        }

        public void FinalizeJobState(string name, BackupType type, JobState state,
    string sourcePath, string targetPath, int totalFiles, long totalSize, int progression)
        {
            JobStateInfo jobState = GetOrCreateJobState(name);

            jobState.Type = type;
            jobState.State = state;
            jobState.SourcePath = sourcePath;
            jobState.TargetPath = targetPath;

            // Ne pas modifier TotalFiles lors de la finalisation
            if (jobState.TotalFiles == 0 && totalFiles > 0)
            {
                jobState.TotalFiles = totalFiles;
            }

            jobState.TotalSize = totalSize;

            // Pour un job complété, aucun fichier ne reste à traiter
            if (state == JobState.completed)
            {
                jobState.Progression = 100;
            }
            else
            {

                // Calculer la progression en pourcentage
                jobState.Progression = jobState.TotalFiles > 0
                    ? (int)Math.Min(100, Math.Round((double)progression / jobState.TotalFiles * 100))
                    : 0;
            }

            SaveStateFile();
        }

        private JobStateInfo GetOrCreateJobState(string jobName)
        {
            if (!stateData.ContainsKey(jobName))
            {
                stateData[jobName] = new JobStateInfo { Name = jobName };
            }

            return stateData[jobName];
        }

        private void LoadStateFile()
        {
            try
            {
                if (File.Exists(stateFilePath))
                {
                    string json = File.ReadAllText(stateFilePath);
                    var options = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() }
                    };

                    stateData = JsonSerializer.Deserialize<Dictionary<string, JobStateInfo>>(json, options)
                        ?? new Dictionary<string, JobStateInfo>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading state file: {ex.Message}");
                stateData = new Dictionary<string, JobStateInfo>();
            }
        }

        private void SaveStateFile()
        {
            try
            {
                string directory = Path.GetDirectoryName(stateFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                string json = JsonSerializer.Serialize(stateData, options);
                File.WriteAllText(stateFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving state file: {ex.Message}");
            }
        }

        public void NotifyObservers()
        {
            foreach (var jobState in stateData.Values)
            {
                foreach (var observer in stateObservers)
                {
                    observer.Update("update", jobState.Name, jobState.Type, jobState.State,
                        jobState.SourcePath, jobState.TargetPath, jobState.TotalFiles,
                        jobState.TotalSize, jobState.Progression);
                }
            }
        }

        public void AttachObserver(IStateObserver observer)
        {
            if (!stateObservers.Contains(observer))
            {
                stateObservers.Add(observer);
            }
        }
    }
}
