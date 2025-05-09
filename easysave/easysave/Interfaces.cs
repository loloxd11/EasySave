using System;

namespace EasySave
{
    // Observer interface to be implemented by classes that need to be notified of changes in a BackupJob
    public interface IObserver
    {
        // Method called to update the observer with the current state of the BackupJob and the action performed
        void Update(BackupJob job, string action);
    }

    // Interface representing the logging service used to track file transfers and backup operations
    public interface ILogService
    {
        // Logs the details of a file transfer, including job name, source and target paths, file size, and transfer time
        void LogFileTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime);

        // Retrieves the file path of the daily log file for a specific date
        string GetDailyLogFilePath(DateTime date);

        // Checks if the logging service is ready to log operations
        bool IsLogServiceReady();

        // Serializes a log entry into a string format, including job name, source and target paths, file size, transfer time, and timestamp
        string SerializeLogEntry(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, DateTime timestamp);
    }
}
