using System.Text.Json;

public class LogManager : ILogService
{
    private string logBaseDirectory;
    private JsonSerializerOptions jsonOptions;
    private static LogManager? instance;

    private LogManager(string baseDirectory)
    {
        logBaseDirectory = baseDirectory;
        jsonOptions = JsonSerializerHelper.CreateDefaultOptions();
        FileHelper.EnsureDirectoryExists(logBaseDirectory);
    }

    public static LogManager GetInstance(string baseDirectory)
    {
        return instance ??= new LogManager(baseDirectory);
    }

    public void LogFileTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime)
    {
        try
        {
            var entry = new LogEntry(jobName, sourcePath, targetPath, fileSize, transferTime, transferTime >= 0);
            var logPath = FormatLogFilePath(entry.Timestamp);
            FileHelper.AppendToFile(logPath, entry.ToJson());
        }
        catch (Exception ex)
        {
            throw new LogServiceException("Failed to log file transfer", ex);
        }
    }

    public string GetDailyLogFilePath(DateTime date) => FormatLogFilePath(date);

    public bool IsLogServiceReady() => Directory.Exists(logBaseDirectory);

    private string FormatLogFilePath(DateTime date) => Path.Combine(logBaseDirectory, date.ToString("yyyy-MM-dd") + ".json");
}