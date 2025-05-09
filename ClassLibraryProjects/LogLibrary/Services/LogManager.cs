using System;
using System.Text.Json;

public class LogManager : ILogService
{
    /// <summary>
    /// Logs the details of a file transfer operation by serializing the information
    /// and appending it to a log file in JSON format.
    /// </summary>
    /// <param name="jobName">The name of the job associated with the file transfer.</param>
    /// <param name="sourcePath">The source file path of the transfer.</param>
    /// <param name="targetPath">The target file path of the transfer.</param>
    /// <param name="fileSize">The size of the file being transferred, in bytes.</param>
    /// <param name="transferTime">The time taken to transfer the file, in milliseconds.</param>
    public void LogFileTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime)
    {
        // Serialize the log entry into a JSON string.
        string jsonLog = SerializeLogEntry(jobName, sourcePath, targetPath, fileSize, transferTime, DateTime.Now);

        // Append the serialized log entry to the "logs.json" file.
        System.IO.File.AppendAllText("logs.json", jsonLog + Environment.NewLine);
    }

    /// <summary>
    /// Retrieves the file path of the daily log file for a specific date.
    /// </summary>
    /// <param name="date">The date for which the log file path is requested.</param>
    /// <returns>The file path of the daily log file in the format "logs_yyyyMMdd.json".</returns>
    public string GetDailyLogFilePath(DateTime date)
    {
        return $"logs_{date:yyyyMMdd}.json";
    }

    /// <summary>
    /// Checks if the logging service is ready to perform operations.
    /// </summary>
    /// <returns>True if the logging service is ready; otherwise, false.</returns>
    public bool IsLogServiceReady()
    {
        return true;
    }

    /// <summary>
    /// Serializes a log entry containing details of a file transfer operation into a JSON string.
    /// </summary>
    /// <param name="jobName">The name of the job associated with the file transfer.</param>
    /// <param name="sourcePath">The source file path of the transfer.</param>
    /// <param name="targetPath">The target file path of the transfer.</param>
    /// <param name="fileSize">The size of the file being transferred, in bytes.</param>
    /// <param name="transferTime">The time taken to transfer the file, in milliseconds.</param>
    /// <param name="timestamp">The timestamp of the log entry.</param>
    /// <returns>A serialized string representation of the log entry in JSON format.</returns>
    public string SerializeLogEntry(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, DateTime timestamp)
    {
        // Create an anonymous object representing the log entry.
        var logEntry = new
        {
            Timestamp = timestamp.ToString("yyyy-MM-ddTHH:mm:ss"),
            JobName = jobName,
            Source = sourcePath,
            Target = targetPath,
            FileSize = fileSize,
            TransferTimeMs = transferTime
        };

        // Use helper methods to create JSON serialization options and serialize the log entry.
        var options = JsonSerializerHelper.CreateDefaultOptions();
        return JsonSerializerHelper.Serialize(logEntry, options);
    }

    /// <summary>
    /// Retrieves an instance of the logging service based on the specified base directory.
    /// </summary>
    /// <param name="baseDirectory">The base directory for the logging service.</param>
    /// <returns>An instance of the logging service.</returns>
    internal static ILogService GetInstance(string baseDirectory)
    {
        throw new NotImplementedException();
    }
}
