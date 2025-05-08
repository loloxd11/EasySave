using System;

namespace EasySave
{
    public interface IObserver
    {
        void Update(BackupJob job, string action);
    }

    // Cette interface reflète celle fournie par la DLL
    public interface ILogService
    {
        void LogFileTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime);
        string GetDailyLogFilePath(DateTime date);
        bool IsLogServiceReady();
        string SerializeLogEntry(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, DateTime timestamp);
    }
}
