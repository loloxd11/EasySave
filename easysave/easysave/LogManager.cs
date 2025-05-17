using System;
using System.Collections.Generic;
using System.IO;
using LogLibrary.Enums;
using LogLibrary.Factories;
using LogLibrary.Interfaces;

namespace EasySave
{
    /// <summary>
    /// Manages logging operations for the EasySave application.
    /// Implements the Singleton pattern to ensure a single instance.
    /// </summary>
    public class LogManager : ILogger, IObserver
    {
        // Singleton instance of LogManager
        private static LogManager instance;
        private static readonly object lockObject = new object();

        // External logger instance from the LogLibrary
        private readonly LogLibrary.Interfaces.ILogger _logger;

        // Default log directory path
        private static string defaultLogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");

        /// <summary>
        /// Private constructor to prevent direct instantiation.
        /// Initializes the logger with the specified directory and format.
        /// </summary>
        /// <param name="logDirectory">The directory where logs will be stored.</param>
        /// <param name="format">The format of the logs (XML or JSON).</param>
        private LogManager(string logDirectory, LogFormat format = LogFormat.XML)
        {
            _logger = LoggerFactory.Create(logDirectory, format);
        }

        /// <summary>
        /// Retrieves the singleton instance of LogManager.
        /// Ensures thread safety and initializes the logger if not already done.
        /// </summary>
        /// <param name="logDirectory">Optional custom log directory.</param>
        /// <param name="format">Optional log format (default is XML).</param>
        /// <returns>The singleton instance of LogManager.</returns>
        public static LogManager GetInstance(string logDirectory = null, LogFormat format = LogFormat.XML)
        {
            if (instance == null)
            {
                lock (lockObject) // Ensures thread safety
                {
                    if (instance == null)
                    {
                        // Use default directory if none is specified
                        string directory = logDirectory ?? defaultLogDirectory;

                        // Ensure the directory exists
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // Check for log format in configuration
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

        /// <summary>
        /// Logs the details of a file transfer operation.
        /// </summary>
        /// <param name="jobName">The name of the backup job.</param>
        /// <param name="sourcePath">The source file path.</param>
        /// <param name="targetPath">The target file path.</param>
        /// <param name="fileSize">The size of the file in bytes.</param>
        /// <param name="transferTime">The time taken to transfer the file in milliseconds.</param>
        /// <param name="encryptionTime">The time taken to encrypt the file in milliseconds (default is 0).</param>
        public void LogTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, long encryptionTime = 0)
        {
            _logger.LogTransfer(jobName, sourcePath, targetPath, fileSize, transferTime, encryptionTime);
        }

        /// <summary>
        /// Logs a specific event related to the backup process.
        /// </summary>
        /// <param name="eventName">The name of the event to log.</param>
        public void LogEvent(string eventName)
        {
            _logger.LogEvent(eventName);
        }

        /// <summary>
        /// Retrieves the current log format used by the logger.
        /// </summary>
        /// <returns>The current log format.</returns>
        public LogFormat GetCurrentFormat()
        {
            return _logger.GetCurrentFormat();
        }

        /// <summary>
        /// Sets the log format to be used by the logger.
        /// </summary>
        /// <param name="format">The log format to set.</param>
        public void SetFormat(LogFormat format)
        {
            _logger.SetFormat(format);
        }

        /// <summary>
        /// Retrieves the file path of the current log file.
        /// </summary>
        /// <returns>The file path of the current log file.</returns>
        public string GetCurrentLogFilePath()
        {
            return _logger.GetCurrentLogFilePath();
        }

        /// <summary>
        /// Checks if the logger is ready to log events or transfers.
        /// </summary>
        /// <returns>True if the logger is ready, otherwise false.</returns>
        public bool IsReady()
        {
            return _logger.IsReady();
        }

        /// <summary>
        /// Updates the observer with the current state of the BackupJob and the action performed.
        /// </summary>
        /// <param name="job">The backup job whose state has changed.</param>
        /// <param name="action">The action performed on the backup job.</param>
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

                case "delete":
                    // Journalisation de la suppression d'un fichier
                    LogTransfer(
                        job.Name,
                        "", // Pas de fichier source pour une suppression
                        job.CurrentTargetFile,
                        0, // Pas de taille pour une suppression
                        0, // Pas de temps de transfert pour une suppression
                        0  // Pas de cryptage pour une suppression
                    );
                    break;

                case "delete_dir":
                    // Journalisation de la suppression d'un répertoire
                    LogEvent("DirectoryDeleted");
                    break;

                case "clean_complete":
                    // Journalisation de la fin du nettoyage d'un répertoire
                    LogEvent("TargetDirectoryCleaned");
                    break;

                case "progress":
                    // Optionnellement, logger les mises à jour de progression si besoin
                    // LogEvent("JobProgress", properties);
                    break;
            }
        }


        /// <summary>
        /// Utility method to get the size of a file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The size of the file in bytes, or 0 if an error occurs.</returns>
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
