using System;

namespace EasySave
{
    public interface IObserver
    {
        void Update(BackupJob job, string action);
    }

    public interface ILogService
    {
        void LogFileTransfer(string jobName, string source, string target, long size, long time);
        string GetDailyLogFilePath(DateTime date);
    }
}
