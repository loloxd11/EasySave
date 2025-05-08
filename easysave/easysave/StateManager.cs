using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave
{
    public class StateManager : IObserver
    {
        private string stateFilePath;
        private Dictionary<string, JobStateInfo> stateData;

        public StateManager(string path)
        {
            stateFilePath = path;
            LoadStateFile();
        }

        public void Update(BackupJob job, string action)
        {
            Console.WriteLine($"Mise à jour de l'état pour le job {job.Name} : {action}");  
            // Mettre à jour l'état pour cette tâche
            if (action == "create"){
                InitializeJobState(job);
            }
            else if (action == "end")
            {
                FinalizeJobState(job);
            }
            else
            {
                UpdateJobState(job);
            }

        }

        public void UpdateJobState(BackupJob job)
        {
            JobStateInfo jobState = GetOrCreateJobState(job.Name);

            jobState.Name = job.Name;
            jobState.State = job.State.ToString();
            jobState.LastUpdated = DateTime.Now;

            // Mettre à jour ces valeurs quelle que soit l'état du job
            jobState.TotalFiles = job.TotalFiles;
            jobState.TotalSize = job.TotalSize;
            jobState.FilesRemaining = job.RemainingFiles;
            jobState.SizeRemaining = job.RemainingSize;
            jobState.Progression = job.Progression;
            jobState.CurrentSourceFile = job.CurrentSourceFile;
            jobState.CurrentTargetFile = job.CurrentTargetFile;


            // Enregistrer l'état mis à jour dans le fichier
            SaveStateFile();

        }

        public void InitializeJobState(BackupJob job)
        {
            JobStateInfo jobState = GetOrCreateJobState(job.Name);

            jobState.Name = job.Name;
            jobState.State = JobState.Inactive.ToString();
            jobState.LastUpdated = DateTime.Now;
            jobState.TotalFiles = 0;
            jobState.TotalSize = 0;
            jobState.FilesRemaining = 0;
            jobState.SizeRemaining = 0;
            jobState.Progression = 0;
            jobState.CurrentSourceFile = string.Empty;
            jobState.CurrentTargetFile = string.Empty;

            SaveStateFile();
        }

        public void FinalizeJobState(BackupJob job)
        {
            JobStateInfo jobState = GetOrCreateJobState(job.Name);

            jobState.State = job.State.ToString();
            jobState.LastUpdated = DateTime.Now;
            jobState.TotalFiles = job.TotalFiles;
            jobState.TotalSize = job.TotalSize;
            jobState.Progression = (job.State == JobState.Completed) ? 100 : job.Progression;
            jobState.FilesRemaining = 0;
            jobState.SizeRemaining = 0;

            // Mettre à jour les fichiers actuels (même s'ils sont null)
            jobState.CurrentSourceFile = null;
            jobState.CurrentTargetFile = null;

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
                    stateData = JsonSerializer.Deserialize<Dictionary<string, JobStateInfo>>(json);
                }
                else
                {
                    stateData = new Dictionary<string, JobStateInfo>();
                }
            }
            catch (Exception)
            {
                stateData = new Dictionary<string, JobStateInfo>();
            }
        }

        private void SaveStateFile()
        {
            try
            {
                // S'assurer que le répertoire existe
                string directory = Path.GetDirectoryName(stateFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true // Pour l'affichage avec des sauts de ligne
                };

                string json = JsonSerializer.Serialize(stateData, options);
                File.WriteAllText(stateFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'enregistrement du fichier d'état: {ex.Message}");
            }
        }

        // Classe interne pour représenter les informations d'état d'une tâche
        private class JobStateInfo
        {
            public string Name { get; set; }
            public string State { get; set; }
            public DateTime LastUpdated { get; set; }
            public int TotalFiles { get; set; }
            public long TotalSize { get; set; }
            public int FilesRemaining { get; set; }
            public long SizeRemaining { get; set; }
            public int Progression { get; set; }
            public string CurrentSourceFile { get; set; }
            public string CurrentTargetFile { get; set; }
        }
    }
}
