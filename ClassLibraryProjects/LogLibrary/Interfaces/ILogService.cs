/// <summary>
/// Interface defining the contract for a logging service.
/// </summary>
public interface ILogService
{
    /// <summary>
    /// Logs the details of a file transfer operation.
    /// </summary>
    /// <param name="jobName">The name of the job associated with the file transfer.</param>
    /// <param name="sourcePath">The source file path of the transfer.</param>
    /// <param name="targetPath">The target file path of the transfer.</param>
    /// <param name="fileSize">The size of the file being transferred, in bytes.</param>
    /// <param name="transferTime">The time taken to transfer the file, in milliseconds.</param>
    void LogFileTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime);

    /// <summary>
    /// Retrieves the file path of the daily log file for a specific date.
    /// </summary>
    /// <param name="date">The date for which the log file path is requested.</param>
    /// <returns>The file path of the daily log file.</returns>
    string GetDailyLogFilePath(DateTime date);

    /// <summary>
    /// Checks if the logging service is ready to perform operations.
    /// </summary>
    /// <returns>True if the logging service is ready; otherwise, false.</returns>
    bool IsLogServiceReady();

    /// <summary>
    /// Serializes a log entry containing details of a file transfer operation.
    /// </summary>
    /// <param name="jobName">The name of the job associated with the file transfer.</param>
    /// <param name="sourcePath">The source file path of the transfer.</param>
    /// <param name="targetPath">The target file path of the transfer.</param>
    /// <param name="fileSize">The size of the file being transferred, in bytes.</param>
    /// <param name="transferTime">The time taken to transfer the file, in milliseconds.</param>
    /// <param name="timestamp">The timestamp of the log entry.</param>
    /// <returns>A serialized string representation of the log entry.</returns>
    string SerializeLogEntry(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, DateTime timestamp);
}
