using System;
using System.IO;
using LogLibrary;
using System.Reflection;
using System.Text.Json;

namespace EasySave
{
    public class LogManager : IObserver
    {
        private readonly ILogService logService;
        private readonly string logDirectory;

        public LogManager(string directory)
        {
            logDirectory = directory;

            // Create the log directory if it does not exist
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Obtain an instance of ILogService from the DLL
            logService = GetLogServiceInstance(logDirectory);
        }

        private ILogService GetLogServiceInstance(string directory)
        {
            try
            {
                // Attempt to load the LogLibrary assembly
                Assembly logLibrary = Assembly.Load("LogLibrary");

                // Look for the LogServiceFactory type
                Type factoryType = logLibrary.GetType("LogLibrary.LogServiceFactory");
                if (factoryType != null)
                {
                    // Look for the CreateLogService method that takes a string parameter
                    MethodInfo createMethod = factoryType.GetMethod("CreateLogService", new[] { typeof(string) });
                    if (createMethod != null)
                    {
                        // Call the static method
                        return (ILogService)createMethod.Invoke(null, new object[] { directory });
                    }
                }

                // Second approach: try to find a direct implementation of ILogService
                Type logManagerType = logLibrary.GetType("LogLibrary.LogManager");
                if (logManagerType != null)
                {
                    // Look for the GetInstance method
                    MethodInfo getInstance = logManagerType.GetMethod("GetInstance", new[] { typeof(string) });
                    if (getInstance != null)
                    {
                        // Call the static method
                        return (ILogService)getInstance.Invoke(null, new object[] { directory });
                    }

                    // Try to create an instance using the constructor
                    var constructor = logManagerType.GetConstructor(new[] { typeof(string) });
                    if (constructor != null)
                    {
                        return (ILogService)constructor.Invoke(new object[] { directory });
                    }
                }

                // If all attempts fail, use a fallback implementation
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
                bool isServiceReady = true;

                // Check if the IsLogServiceReady method exists and call it
                try
                {
                    isServiceReady = logService.IsLogServiceReady();
                }
                catch
                {
                    // Assume the service is ready in case of an error
                    isServiceReady = true;
                }

                if (isServiceReady)
                {
                    // Ensure file paths are valid
                    if (!string.IsNullOrEmpty(job.CurrentSourceFile) && !string.IsNullOrEmpty(job.CurrentTargetFile))
                    {
                        try
                        {
                            // Calculate the file size
                            long fileSize = 0;
                            if (File.Exists(job.CurrentSourceFile))
                            {
                                fileSize = new FileInfo(job.CurrentSourceFile).Length;
                            }

                            // Use the SerializeLogEntry method to get the JSON string
                            DateTime timestamp = DateTime.Now;
                            string jsonEntry = logService.SerializeLogEntry(
                                job.Name,
                                job.CurrentSourceFile,
                                job.CurrentTargetFile,
                                fileSize,
                                job.LastFileTime,
                                timestamp);

                            // Write the JSON entry to the log file
                            string logFilePath = logService.GetDailyLogFilePath(timestamp);
                            File.AppendAllText(logFilePath, jsonEntry + Environment.NewLine);

                            // Also call LogFileTransfer for compatibility
                            logService.LogFileTransfer(
                                job.Name,
                                job.CurrentSourceFile,
                                job.CurrentTargetFile,
                                fileSize,
                                job.LastFileTime);
                        }
                        catch (Exception)
                        {
                            // Do not display the error in the console
                        }
                    }
                }
            }
        }
    }

    // Fallback implementation for ILogService in case of issues with the DLL
    internal class FallbackLogService : ILogService
    {
        private readonly string logDirectory;
        private readonly JsonSerializerOptions jsonOptions;

        public FallbackLogService(string directory)
        {
            logDirectory = directory;

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Configure JSON serialization options
            jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false // No formatting to have one entry per line
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

                // Use SerializeLogEntry to get the JSON entry
                string jsonEntry = SerializeLogEntry(jobName, sourcePath, targetPath, fileSize, transferTime, DateTime.Now);

                // Append a new line to the end of the file
                File.AppendAllText(logFilePath, jsonEntry + Environment.NewLine);
            }
            catch (Exception)
            {
                // Do not display the error in the console
            }
        }

        public bool IsLogServiceReady()
        {
            return true;
        }

        public string SerializeLogEntry(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, DateTime timestamp)
        {
            // Create the log object with the exact required format
            var logEntry = new
            {
                Timestamp = timestamp.ToString("yyyy-MM-ddTHH:mm:ss"),
                JobName = jobName,
                Source = sourcePath,
                Target = targetPath,
                FileSize = fileSize,
                TransferTimeMs = transferTime
            };

            // Serialize to JSON
            if (logFormat == "XML")
            {
                return SerializeToXml(logEntry);
            }
            else
            {
                var options = JsonSerializerHelper.CreateDefaultOptions();
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
