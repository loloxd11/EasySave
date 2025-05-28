namespace EasySave.Models
{
    /// <summary>
    /// Interface for logging file transfers and events in the application.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs the transfer of a file, including job name, source and target paths, file size, transfer time, and encryption time.
        /// </summary>
        /// <param name="jobName">The name of the backup job.</param>
        /// <param name="sourcePath">The source file path.</param>
        /// <param name="targetPath">The target file path.</param>
        /// <param name="fileSize">The size of the file in bytes.</param>
        /// <param name="transferTime">The time taken to transfer the file in milliseconds.</param>
        /// <param name="encryptionTime">The time taken to encrypt the file in milliseconds.</param>
        void LogFileTransfer(string jobName, string sourcePath, string targetPath,
            long fileSize, long transferTime, long encryptionTime);

        /// <summary>
        /// Logs a specific event by its name.
        /// </summary>
        /// <param name="eventName">The name of the event to log.</param>
        void LogEvent(string name, string eventName);

        /// <summary>
        /// Gets the current log format (e.g., JSON or XML).
        /// </summary>
        /// <returns>The current <see cref="LogFormat"/> used for logging.</returns>
        LogFormat GetCurrentFormat();

        /// <summary>
        /// Gets the file path of the current log file.
        /// </summary>
        /// <returns>The path to the current log file as a string.</returns>
        string GetCurrentLogFilePath();

        /// <summary>
        /// Checks if the logger is ready to log events or file transfers.
        /// </summary>
        /// <returns>True if the logger is ready; otherwise, false.</returns>
        bool IsReady();
    }
}
