// LogLibrary/Interfaces/ILogger.cs
using LogLibrary.Enums;
using System;
using System.Collections.Generic;

namespace LogLibrary.Interfaces
{
    /// <summary>
    /// Interface defining logging operations.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a file transfer operation.
        /// </summary>
        /// <param name="jobName">The name of the job associated with the transfer.</param>
        /// <param name="sourcePath">The source file path.</param>
        /// <param name="targetPath">The destination file path.</param>
        /// <param name="fileSize">The size of the file in bytes.</param>
        /// <param name="transferTime">The time taken for the transfer in milliseconds.</param>
        /// <param name="encryptionTime">The time taken for encryption in milliseconds.</param>
        void LogTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, long encryptionTime);

        /// <summary>
        /// Logs a custom event with specific properties.
        /// </summary>
        /// <param name="eventName">The name of the event to log.</param>
        void LogEvent(string eventName);

        /// <summary>
        /// Retrieves the current logging format.
        /// </summary>
        /// <returns>The current logging format.</returns>
        LogFormat GetCurrentFormat();

        /// <summary>
        /// Sets the logging format to be used.
        /// </summary>
        /// <param name="format">The logging format to set.</param>
        void SetFormat(LogFormat format);

        /// <summary>
        /// Retrieves the current log file path.
        /// </summary>
        /// <returns>The path of the current log file.</returns>
        string GetCurrentLogFilePath();

        /// <summary>
        /// Checks if the logging service is ready to use.
        /// </summary>
        /// <returns>True if the service is ready, otherwise false.</returns>
        bool IsReady();
    }
}
