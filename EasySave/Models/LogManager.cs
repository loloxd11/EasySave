using LogLibrary.Factories;
using System.IO;

namespace EasySave.Models
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

        // Lock object dedicated to log writing
        private static readonly object _logWriteLock = new object();

        // External logger instance from LogLibrary
        private readonly LogLibrary.Interfaces.ILogger _logger;

        // Default log directory path
        private static string defaultLogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");

        /// <summary>
        /// Private constructor to initialize the logger with the specified directory and format.
        /// </summary>
        /// <param name="logDirectory">Directory where logs will be stored.</param>
        /// <param name="format">Format used for logging (XML or JSON).</param>
        private LogManager(string logDirectory, LogFormat format = LogFormat.XML)
        {
            _logger = LoggerFactory.Create(logDirectory, (LogLibrary.Enums.LogFormat)format);
        }

        /// <summary>
        /// Gets the singleton instance of LogManager, creating it if necessary.
        /// </summary>
        /// <param name="logDirectory">Optional log directory path.</param>
        /// <param name="format">Optional log format.</param>
        /// <returns>The singleton LogManager instance.</returns>
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
        /// Logs a file transfer operation.
        /// </summary>
        /// <param name="jobName">Name of the backup job.</param>
        /// <param name="sourcePath">Source file path.</param>
        /// <param name="targetPath">Target file path.</param>
        /// <param name="fileSize">Size of the file in bytes.</param>
        /// <param name="transferTime">Time taken to transfer the file in milliseconds.</param>
        /// <param name="encryptionTime">Time taken to encrypt the file in milliseconds.</param>
        public void LogFileTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, long encryptionTime)
        {
            lock (_logWriteLock)
            {
                _logger.LogTransfer(jobName, sourcePath, targetPath, fileSize, transferTime, encryptionTime);
            }
        }

        /// <summary>
        /// Logs a specific event related to the backup process.
        /// </summary>
        /// <param name="jobName">Name of the backup job.</param>
        /// <param name="eventName">Name of the event to log.</param>
        public void LogEvent(string jobName, string eventName)
        {
            lock (_logWriteLock)
            {
                string formattedEvent = $"{jobName} - {eventName}";
                _logger.LogEvent(formattedEvent);
            }
        }

        /// <summary>
        /// Gets the current log format.
        /// </summary>
        /// <returns>The current LogFormat used for logging.</returns>
        public LogFormat GetCurrentFormat()
        {
            return (LogFormat)_logger.GetCurrentFormat();
        }

        /// <summary>
        /// Sets the log format.
        /// </summary>
        /// <param name="format">The log format to set.</param>
        public void SetFormat(LogFormat format)
        {
            _logger.SetFormat((LogLibrary.Enums.LogFormat)format);
        }

        /// <summary>
        /// Gets the file path of the current log file.
        /// </summary>
        /// <returns>The path to the current log file as a string.</returns>
        public string GetCurrentLogFilePath()
        {
            return _logger.GetCurrentLogFilePath();
        }

        /// <summary>
        /// Checks if the logger is ready to log events or file transfers.
        /// </summary>
        /// <returns>True if the logger is ready; otherwise, false.</returns>
        public bool IsReady()
        {
            return _logger.IsReady();
        }

        /// <summary>
        /// Observer update method called to notify of a change in a backup job.
        /// </summary>
        /// <param name="action">The action performed (e.g., start, stop, update).</param>
        /// <param name="name">The name of the backup job.</param>
        /// <param name="type">The type of backup (Complete or Differential).</param>
        /// <param name="state">The current state of the job.</param>
        /// <param name="sourcePath">The source path of the backup.</param>
        /// <param name="targetPath">The target path of the backup.</param>
        /// <param name="totalFiles">The total number of files involved in the backup.</param>
        /// <param name="totalSize">The total size of the files in bytes.</param>
        /// <param name="transferTime">The time taken for file transfer, if applicable.</param>
        /// <param name="encryptionTime">The time taken for encryption, if applicable.</param>
        /// <param name="progression">The progression percentage of the backup job.</param>
        public void Update(string action, string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, long transferTime, long encryptionTime, int progression)
        {
            switch (action)
            {
                case "start":
                    LogEvent(name, "JobStarted");
                    break;
                case "finish":
                    LogEvent(name, "JobCompleted");
                    break;
                case "error":
                    LogEvent(name, "JobError");
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
                            transferTime,
                            encryptionTime
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
                    LogEvent(name, "DirectoryDeleted");
                    break;
                case "clean_complete":
                    LogEvent(name, "TargetDirectoryCleaned");
                    break;
                case "pause":
                    LogEvent(name, "JobPaused");
                    break;
                case "cancelled":
                    LogEvent(name, "JobCancelled");
                    break;
            }
        }

        /// <summary>
        /// Gets the size of a file in bytes.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>The size of the file in bytes, or 0 if the file does not exist or an error occurs.</returns>
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
