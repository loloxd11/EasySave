// ClassLibraryProjects/LogLibrary/Services/LogManager.cs
using System.Xml.Serialization;
using System.IO;
using System.Text.Json;

public class LogManager : ILogService
{
    private readonly string baseDirectory;
    private readonly string logFormat;

    // Update the constructor
    internal LogManager(string baseDirectory, string logFormat = "JSON")
    {
        this.baseDirectory = baseDirectory;
        this.logFormat = logFormat.ToUpper(); // Ensure consistent casing
    }
internal static ILogService GetInstance(string baseDirectory, string logFormat = "JSON")
{
    return new LogManager(baseDirectory, logFormat);
}
    public void LogFileTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime)
    {
         string logFilePath = GetDailyLogFilePath(DateTime.Now);
         string jsonLog = SerializeLogEntry(jobName, sourcePath, targetPath, fileSize, transferTime, DateTime.Now);

         System.IO.File.AppendAllText(logFilePath, jsonLog + Environment.NewLine);
    }
    public string SerializeLogEntry(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, DateTime timestamp)
    {
        var logEntry = new LogEntry
        {
            Timestamp = timestamp.ToString("yyyy-MM-ddTHH:mm:ss"),
            JobName = jobName,
            Source = sourcePath,
            Target = targetPath,
            FileSize = fileSize,
            TransferTimeMs = (int)transferTime
        };

        if (logFormat == "XML")
        {
            return SerializeToXml(logEntry);
        }
        else
        {
            var options = JsonSerializerHelper.CreateDefaultOptions();
            return JsonSerializerHelper.Serialize(logEntry, options);
        }
    }

    private string SerializeToXml(LogEntry logEntry)
    {
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(LogEntry));
        using (StringWriter textWriter = new StringWriter())
        {
            xmlSerializer.Serialize(textWriter, logEntry);
            return textWriter.ToString();
        }
    }

   public string GetDailyLogFilePath(DateTime date)
    {
        string fileExtension = logFormat == "XML" ? "xml" : "json";
        return Path.Combine(baseDirectory, $"logs_{date:yyyyMMdd}.{fileExtension}");
    }
   
    public bool IsLogServiceReady()
    {
        // Check if the base directory exists
        if (!Directory.Exists(baseDirectory))
        {
            Directory.CreateDirectory(baseDirectory);
        }

        // Check if the log file can be created
        string testFilePath = Path.Combine(baseDirectory, "test.log");
        try
        {
            using (File.Create(testFilePath)) { }
            File.Delete(testFilePath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
