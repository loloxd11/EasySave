using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasySave.Models;
using LogLibrary.Factories;
using LogLibrary.Enums;
using LogLibrary.Interfaces;

namespace EasySave.Models
{
    /// <summary>
    /// Gère les opérations de journalisation pour l'application EasySave.
    /// Implémente le pattern Singleton pour garantir une instance unique.
    /// </summary>
    public class LogManager : ILogger, IObserver
    {
        // Instance Singleton de LogManager
        private static LogManager instance;
        private static readonly object lockObject = new object();

        // Verrou dédié à l'écriture dans les logs
        private static readonly object _logWriteLock = new object();

        // Instance de logger externe de la LogLibrary
        private readonly LogLibrary.Interfaces.ILogger _logger;

        // Chemin du répertoire de logs par défaut
        private static string defaultLogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");

        private LogManager(string logDirectory, LogFormat format = LogFormat.XML)
        {
            _logger = LoggerFactory.Create(logDirectory, (LogLibrary.Enums.LogFormat)format);
        }

        public static LogManager GetInstance(string logDirectory = null, LogFormat format = LogFormat.XML)
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        string directory = logDirectory ?? defaultLogDirectory;
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
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
        /// Journalise un transfert de fichier.
        /// </summary>
        public void LogFileTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, long encryptionTime)
        {
            lock (_logWriteLock)
            {
                _logger.LogTransfer(jobName, sourcePath, targetPath, fileSize, transferTime, encryptionTime);
            }
        }

        /// <summary>
        /// Journalise un événement spécifique lié au processus de sauvegarde.
        /// </summary>
        public void LogEvent(string eventName)
        {
            lock (_logWriteLock)
            {
                _logger.LogEvent(eventName);
            }
        }

        public LogFormat GetCurrentFormat()
        {
            return (LogFormat)_logger.GetCurrentFormat();
        }

        public void SetFormat(LogFormat format)
        {
            _logger.SetFormat((LogLibrary.Enums.LogFormat)format);
        }

        public string GetCurrentLogFilePath()
        {
            return _logger.GetCurrentLogFilePath();
        }

        public bool IsReady()
        {
            return _logger.IsReady();
        }

        public void Update(string action, string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, int progression)
        {
            switch (action)
            {
                case "start":
                    LogEvent("JobStarted");
                    break;
                case "finish":
                    LogEvent("JobCompleted");
                    break;
                case "error":
                    LogEvent("JobError");
                    break;
                case "processing":
                    if (sourcePath != null && targetPath != null)
                    {
                        long fileSize = GetFileSize(sourcePath);
                        LogFileTransfer(
                            name,
                            sourcePath,
                            targetPath,
                            fileSize,
                            0,
                            0
                        );
                    }
                    break;
                case "delete":
                    LogFileTransfer(
                        name,
                        "",
                        targetPath,
                        0,
                        0,
                        0
                    );
                    break;
                case "delete_dir":
                    LogEvent("DirectoryDeleted");
                    break;
                case "clean_complete":
                    LogEvent("TargetDirectoryCleaned");
                    break;
            }
        }

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
