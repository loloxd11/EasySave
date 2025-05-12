using System;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;

namespace EasySave
{
    public class LogManager : IObserver
    {
        private readonly ILogService logService;
        private readonly string logDirectory;

        public LogManager(string directory)
        {
            logDirectory = directory;

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            logService = GetLogServiceInstance(logDirectory);
        }

        private ILogService GetLogServiceInstance(string directory)
        {
            try
            {
                // Simplified fallback implementation
                return new FallbackLogService(directory);
            }
            catch (Exception)
            {
                return new FallbackLogService(directory);
            }
        }

        public void Update(BackupJob job, string action)
        {
            if (action == "file")
            {
                if (!string.IsNullOrEmpty(job.CurrentSourceFile) && !string.IsNullOrEmpty(job.CurrentTargetFile))
                {
                    try
                    {
                        long fileSize = File.Exists(job.CurrentSourceFile) ? new FileInfo(job.CurrentSourceFile).Length : 0;
                        DateTime timestamp = DateTime.Now;

                        string jsonEntry = logService.SerializeLogEntry(
                            job.Name,
                            job.CurrentSourceFile,
                            job.CurrentTargetFile,
                            fileSize,
                            job.LastFileTime,
                            timestamp);

                        string logFilePath = logService.GetDailyLogFilePath(timestamp);
                        File.AppendAllText(logFilePath, jsonEntry + Environment.NewLine);

                        logService.LogFileTransfer(
                            job.Name,
                            job.CurrentSourceFile,
                            job.CurrentTargetFile,
                            fileSize,
                            job.LastFileTime);
                    }
                    catch (Exception)
                    {
                        // Handle exceptions silently
                    }
                }
            }
        }
    }

    internal class LogEntry
    {
        public string Timestamp { get; set; }
        public string JobName { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public long FileSize { get; set; }
        public long TransferTimeMs { get; set; }
    }

    internal class FallbackLogService : ILogService
    {
        private readonly string logDirectory;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly string logFormat;

        public FallbackLogService(string directory)
        {
            logDirectory = directory;
            logFormat = "JSON";

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false
            };
        }

        public string GetDailyLogFilePath(DateTime date)
        {
            string fileExtension = logFormat == "XML" ? "xml" : "json";
            return Path.Combine(logDirectory, $"{date:yyyy-MM-dd}.{fileExtension}");
        }

        public void LogFileTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime)
        {
            try
            {
                string logFilePath = GetDailyLogFilePath(DateTime.Now);
                string jsonEntry = SerializeLogEntry(jobName, sourcePath, targetPath, fileSize, transferTime, DateTime.Now);
                File.AppendAllText(logFilePath, jsonEntry + Environment.NewLine);
            }
            catch (Exception)
            {
                // Handle exceptions silently
            }
        }

        public bool IsLogServiceReady()
        {
            return true;
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
                TransferTimeMs = transferTime
            };

            if (logFormat == "XML")
            {
                return SerializeToXml(logEntry);
            }
            else
            {
                return JsonSerializer.Serialize(logEntry, jsonOptions);
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
    }
}