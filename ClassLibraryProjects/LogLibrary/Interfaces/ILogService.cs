public interface ILogService
{
    void LogFileTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime);
    string GetDailyLogFilePath(DateTime date);
    bool IsLogServiceReady();
}