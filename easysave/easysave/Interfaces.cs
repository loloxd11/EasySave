using System;
using LogLibrary.Enums;

namespace EasySave
{
    // Observer interface to be implemented by classes that need to be notified of changes in a BackupJob
    public interface IObserver
    {
        // Method called to update the observer with the current state of the BackupJob and the action performed
        void Update(BackupJob job, string action);
    }

    // Interface representing the logging service used to track file transfers and backup operations
    public interface ILogger
    {
        void LogTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, long encryptionTime);
        void LogEvent(string eventName);
        LogFormat GetCurrentFormat();
        void SetFormat(LogFormat format);
        string GetCurrentLogFilePath();
        bool IsReady();
    }
}
