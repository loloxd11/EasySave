using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave
{
    public class LogManager : IObserver
    {
        private string logDirectory;

        public LogManager(string directory)
        {
            logDirectory = directory;

            // Créer le répertoire de logs s'il n'existe pas
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public void Update(BackupJob job, string action)
        {
            if (action == "file")
            {
                // Ne consigner que les transferts de fichiers
                LogTransfer(job.Name, job.CurrentSourceFile, job.CurrentTargetFile, new FileInfo(job.CurrentSourceFile).Length, job.LastFileTime);
            }
        }

        private void LogTransfer(string jobName, string source, string target, long size, long timeMs)
        {
            try
            {
                // Créer une entrée de log
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = jobName,
                    SourceFile = source,
                    TargetFile = target,
                    FileSize = size,
                    TransferTime = timeMs
                };

                // Obtenir le chemin du fichier de log pour aujourd'hui
                string logFilePath = GetDailyLogFilePath(DateTime.Now);

                // Créer le répertoire s'il n'existe pas
                string directory = Path.GetDirectoryName(logFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Charger les entrées de log existantes ou créer une nouvelle liste
                List<LogEntry> logEntries;
                if (File.Exists(logFilePath))
                {
                    string json = File.ReadAllText(logFilePath);
                    logEntries = JsonSerializer.Deserialize<List<LogEntry>>(json) ?? new List<LogEntry>();
                }
                else
                {
                    logEntries = new List<LogEntry>();
                }

                // Ajouter la nouvelle entrée
                logEntries.Add(logEntry);

                // Écrire le log mis à jour dans le fichier
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true // Pour l'affichage avec des sauts de ligne
                };

                string updatedJson = JsonSerializer.Serialize(logEntries, options);
                File.WriteAllText(logFilePath, updatedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la journalisation du transfert: {ex.Message}");
            }
        }

        private string GetDailyLogFilePath(DateTime date)
        {
            string fileName = $"{date.ToString("yyyy-MM-dd")}.json";
            return Path.Combine(logDirectory, fileName);
        }

        // Classe interne pour représenter une entrée de log
        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string JobName { get; set; }
            public string SourceFile { get; set; }
            public string TargetFile { get; set; }
            public long FileSize { get; set; }
            public long TransferTime { get; set; }
        }
    }
}
