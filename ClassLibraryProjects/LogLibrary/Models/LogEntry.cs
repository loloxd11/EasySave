using System.Text.Json;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string JobName { get; set; }
    public string SourcePath { get; set; }
    public string TargetPath { get; set; }
    public long FileSize { get; set; }
    public long TransferTimeMs { get; set; }
    public bool Success { get; set; }

    public LogEntry(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, bool success)
    {
        Timestamp = DateTime.Now;
        JobName = jobName;
        SourcePath = sourcePath;
        TargetPath = targetPath;
        FileSize = fileSize;
        TransferTimeMs = transferTime;
        Success = success;
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonSerializerHelper.CreateDefaultOptions());
}
