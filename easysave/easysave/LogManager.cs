using System;
using System.Collections.Generic;
using System.IO;
using LogLibrary.Enums;
using LogLibrary.Factories;
using LogLibrary.Interfaces;

namespace EasySave
{
    public class LogManager : ILogger, IObserver
    {
        // Instance unique (singleton)
        private static LogManager instance;
        private static readonly object lockObject = new object();

        // Logger de la bibliothèque externe
        private readonly LogLibrary.Interfaces.ILogger _logger;

        // Chemin du répertoire de log par défaut
        private static string defaultLogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");

        // Constructeur privé pour empêcher l'instanciation directe
        private LogManager(string logDirectory, LogFormat format = LogFormat.XML)
        {
            _logger = LoggerFactory.Create(logDirectory, format);
        }

        // Méthode pour obtenir l'instance unique
        public static LogManager GetInstance(string logDirectory = null, LogFormat format = LogFormat.XML)
        {
            if (instance == null)
            {
                lock (lockObject) // Thread-safe
                {
                    if (instance == null)
                    {
                        // Utiliser le répertoire par défaut si aucun n'est spécifié
                        string directory = logDirectory ?? defaultLogDirectory;

                        // S'assurer que le répertoire existe
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // Vérifier si un format est défini dans la configuration
                        var configManager = ConfigManager.GetInstance();
                        string formatSetting = configManager.GetSetting("LogFormat");

                        if (!string.IsNullOrEmpty(formatSetting))
                        {
                            if (formatSetting.Equals("XML", StringComparison.OrdinalIgnoreCase))
                            {
                                format = LogFormat.XML;
                            }
                            else if (formatSetting.Equals("JSON", StringComparison.OrdinalIgnoreCase))
                            {
                                format = LogFormat.JSON;
                            }
                        }

                        instance = new LogManager(directory, format);
                    }
                }
            }
            return instance;
        }

        public void LogTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, long encryptionTime = 0)
        {
            _logger.LogTransfer(jobName, sourcePath, targetPath, fileSize, transferTime, encryptionTime);
        }

        public void LogEvent(string eventName)
        {
            _logger.LogEvent(eventName);
        }

        public LogFormat GetCurrentFormat()
        {
            return _logger.GetCurrentFormat();
        }

        public void SetFormat(LogFormat format)
        {
            _logger.SetFormat(format);
        }

        public string GetCurrentLogFilePath()
        {
            return _logger.GetCurrentLogFilePath();
        }

        public bool IsReady()
        {
            return _logger.IsReady();
        }

        // Implémentation de la méthode Update de l'interface IObserver
        public void Update(BackupJob job, string action)
        {
            // Créer un dictionnaire pour stocker les propriétés de l'événement
            Dictionary<string, object> properties = new Dictionary<string, object>
            {
                { "JobName", job.Name },
                { "JobType", job.Type.ToString() },
                { "JobState", job.State.ToString() },
                { "SourcePath", job.SourcePath },
                { "TargetPath", job.TargetPath },
                { "TotalFiles", job.TotalFiles },
                { "TotalSize", job.TotalSize },
                { "Progression", job.Progression }
            };

            switch (action)
            {
                case "start":
                    // Journalisation du démarrage d'un travail de sauvegarde
                    LogEvent("JobStarted");
                    break;

                case "finish":
                    // Journalisation de la fin d'un travail de sauvegarde
                    properties["Duration"] = job.LastFileTime; // Ajouter le temps total de la sauvegarde
                    LogEvent("JobCompleted");
                    break;

                case "error":
                    // Journalisation d'une erreur dans un travail de sauvegarde
                    LogEvent("JobError");
                    break;

                case "file":
                    // Journalisation du traitement d'un fichier si nécessaire
                    if (job.LastFileTime > 0)
                    {
                        LogTransfer(
                            job.Name,
                            job.CurrentSourceFile,
                            job.CurrentTargetFile,
                            GetFileSize(job.CurrentSourceFile),
                            job.LastFileTime,
                            0 // Pas de cryptage pour l'instant
                        );
                    }
                    break;

                case "progress":
                    // Optionnellement, logger les mises à jour de progression si besoin
                    // LogEvent("JobProgress", properties);
                    break;
            }
        }

        // Méthode utilitaire pour obtenir la taille d'un fichier
        private long GetFileSize(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                return fileInfo.Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
