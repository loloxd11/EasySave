using System;
using System.Text.Json;

public class LogManager : ILogService
{
    public void LogFileTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime)
    {
        // Exemple d'utilisation de la sérialisation
        string jsonLog = SerializeLogEntry(jobName, sourcePath, targetPath, fileSize, transferTime, DateTime.Now);

        // Écrire le log dans un fichier ou une destination spécifique
        System.IO.File.AppendAllText("logs.json", jsonLog + Environment.NewLine);
    }

    public string GetDailyLogFilePath(DateTime date)
    {
        // Implémentation existante
        return $"logs_{date:yyyyMMdd}.json";
    }

    public bool IsLogServiceReady()
    {
        // Implémentation existante
        return true;
    }

    public string SerializeLogEntry(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, DateTime timestamp)
    {
        var logEntry = new
        {
            Timestamp = timestamp.ToString("yyyy-MM-ddTHH:mm:ss"),
            JobName = jobName,
            Source = sourcePath,
            Target = targetPath,
            FileSize = fileSize,
            TransferTimeMs = transferTime
        };

        var options = JsonSerializerHelper.CreateDefaultOptions();
        return JsonSerializerHelper.Serialize(logEntry, options);
    }

    internal static ILogService GetInstance(string baseDirectory)
    {
        throw new NotImplementedException();
    }
}
