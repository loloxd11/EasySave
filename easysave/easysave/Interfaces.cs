using System;
using LogLibrary.Enums;

namespace EasySave
{
    /// <summary>
    /// Observer interface to be implemented by classes that need to be notified of changes in a BackupJob.
    /// </summary>
    public interface IObserver
    {
        /// <summary>
        /// Method called to update the observer with the current state of the BackupJob and the action performed.
        /// </summary>
        /// <param name="job">The backup job whose state has changed.</param>
        /// <param name="action">The action performed on the backup job.</param>
        void Update(BackupJob job, string action);
    }

    /// <summary>
    /// Interface representing the logging service used to track file transfers and backup operations.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs the details of a file transfer operation.
        /// </summary>
        /// <param name="jobName">The name of the backup job.</param>
        /// <param name="sourcePath">The source file path.</param>
        /// <param name="targetPath">The target file path.</param>
        /// <param name="fileSize">The size of the file in bytes.</param>
        /// <param name="transferTime">The time taken to transfer the file in milliseconds.</param>
        /// <param name="encryptionTime">The time taken to encrypt the file in milliseconds.</param>
        void LogTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, long encryptionTime);

        /// <summary>
        /// Logs a specific event related to the backup process.
        /// </summary>
        /// <param name="eventName">The name of the event to log.</param>
        void LogEvent(string eventName);

        /// <summary>
        /// Retrieves the current log format used by the logger.
        /// </summary>
        /// <returns>The current log format.</returns>
        LogFormat GetCurrentFormat();

        /// <summary>
        /// Sets the log format to be used by the logger.
        /// </summary>
        /// <param name="format">The log format to set.</param>
        void SetFormat(LogFormat format);

        /// <summary>
        /// Retrieves the file path of the current log file.
        /// </summary>
        /// <returns>The file path of the current log file.</returns>
        string GetCurrentLogFilePath();

        /// <summary>
        /// Checks if the logger is ready to log events or transfers.
        /// </summary>
        /// <returns>True if the logger is ready, otherwise false.</returns>
        bool IsReady();
    }
}
