// LogLibrary/Managers/LoggerManager.cs
using LogLibrary.Enums;
using LogLibrary.Interfaces;
using LogLibrary.Models;
using LogLibrary.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogLibrary.Managers
{
    /// <summary>
    /// Concrete implementation of the logging service.
    /// </summary>
    public class LoggerManager : ILogger
    {
        // Maximum log file size (1 MB)
        private const long MAX_FILE_SIZE_BYTES = 1_048_576;

        private readonly string _logDirectory;
        private LogFormat _format;
        private Dictionary<string, int> _sequenceNumbers = new();
        private string? _currentLogFilePath;

        /// <summary>
        /// Initializes a new instance with the specified directory.
        /// </summary>
        /// <param name="directory">The directory for log files.</param>
        public LoggerManager(string directory)
            : this(directory, LogFormat.JSON)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified directory and format.
        /// </summary>
        /// <param name="directory">The directory for log files.</param>
        /// <param name="format">The log format.</param>
        public LoggerManager(string directory, LogFormat format)
        {
            _logDirectory = directory;
            _format = format;

            // Ensure the directory exists
            FileUtil.EnsureDirectoryExists(_logDirectory);
        }

        /// <summary>
        /// Gets the current log format.
        /// </summary>
        /// <returns>The current format.</returns>
        public LogFormat GetCurrentFormat()
        {
            return _format;
        }

        /// <summary>
        /// Sets the log format.
        /// </summary>
        /// <param name="format">The new format to use.</param>
        public void SetFormat(LogFormat format)
        {
            if (_format == format)
                return;

            _format = format;
            _currentLogFilePath = null; // Force creation of a new file
        }

        /// <summary>
        /// Gets the path of the current log file.
        /// </summary>
        /// <returns>The path of the current log file.</returns>
        public string GetCurrentLogFilePath()
        {
            if (_currentLogFilePath == null)
            {
                _currentLogFilePath = GetFilePath(DateTime.Now);
            }

            return _currentLogFilePath;
        }

        /// <summary>
        /// Checks if the logging service is ready.
        /// </summary>
        /// <returns>True if the service is ready to use.</returns>
        public bool IsReady()
        {
            try
            {
                string testFile = Path.Combine(_logDirectory, "test.tmp");
                File.WriteAllText(testFile, "Test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Logs a file transfer operation.
        /// </summary>
        /// <param name="jobName">The job name.</param>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="targetPath">The destination path.</param>
        /// <param name="fileSize">The file size.</param>
        /// <param name="transferTime">The transfer time.</param>
        /// <param name="encryptionTime">The encryption time.</param>
        public void LogTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, long encryptionTime)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                JobName = jobName,
                SourcePath = sourcePath,
                TargetPath = targetPath,
                FileSize = fileSize,
                TransferTimeMs = transferTime,
                EncryptionTimeMs = encryptionTime
            };

            // Always use the path returned by RotateFileIfNeeded
            string filePath = GetCurrentLogFilePath();
            string rotatedFilePath = RotateFileIfNeeded(filePath);

            // Update the current path if rotation occurred
            if (rotatedFilePath != filePath)
                _currentLogFilePath = rotatedFilePath;

            bool isNewFile = rotatedFilePath != filePath;
            string content = CreateLogEntry(entry, rotatedFilePath, isNewFile);

            if (_format == LogFormat.JSON)
            {
                File.AppendAllText(rotatedFilePath, content + Environment.NewLine);
            }
            else
            {
                File.WriteAllText(rotatedFilePath, content);
            }
        }

        /// <summary>
        /// Logs an event with custom properties.
        /// </summary>
        /// <param name="eventName">The event name.</param>
        public void LogEvent(string eventName)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                JobName = eventName
            };

            string filePath = GetCurrentLogFilePath();
            string rotatedFilePath = RotateFileIfNeeded(filePath);

            if (rotatedFilePath != filePath)
                _currentLogFilePath = rotatedFilePath;

            bool isNewFile = rotatedFilePath != filePath;
            string content = CreateLogEntry(entry, rotatedFilePath, isNewFile);

            if (_format == LogFormat.JSON)
            {
                // Add each JSON log on a new line
                File.AppendAllText(rotatedFilePath, content + Environment.NewLine);
            }
            else
            {
                File.WriteAllText(rotatedFilePath, content);
            }

        }

        /// <summary>
        /// Creates a formatted string for the log entry.
        /// </summary>
        /// <param name="entry">The log entry.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="isNewFile">Indicates if this is a new file.</param>
        /// <returns>A string formatted according to the current format.</returns>
        private string CreateLogEntry(LogEntry entry, string filePath, bool isNewFile)
        {
            LogEntries logEntries;

            if (_format == LogFormat.XML)
            {
                if (!isNewFile && File.Exists(filePath))
                {
                    using var stream = File.OpenRead(filePath);
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(LogEntries));
                    logEntries = (LogEntries?)serializer.Deserialize(stream) ?? new LogEntries();
                }
                else
                {
                    logEntries = new LogEntries();
                }

                logEntries.Entries.Add(entry);

                using var ms = new MemoryStream();
                var serializer2 = new System.Xml.Serialization.XmlSerializer(typeof(LogEntries));
                serializer2.Serialize(ms, logEntries);
                ms.Position = 0;
                using var reader = new StreamReader(ms);
                return reader.ReadToEnd();
            }
            else
            {
                return FormatUtil.ToJson(entry);
            }
        }

        /// <summary>
        /// Generates the log file path for a given date.
        /// </summary>
        /// <param name="date">The date for which to generate the path.</param>
        /// <returns>The full path of the log file.</returns>
        private string GetFilePath(DateTime date)
        {
            string dateKey = date.ToString("yyyy-MM-dd");
            string extension = FormatUtil.GetExtension(_format);
            bool formatChanged = _currentLogFilePath != null;

            int sequenceNumber = GetNextSequenceNumber(dateKey, formatChanged);

            return Path.Combine(_logDirectory, $"{dateKey}.{sequenceNumber}.{extension}");
        }

        /// <summary>
        /// Determines the next sequence number for the date and format.
        /// </summary>
        /// <param name="dateKey">The date key (format yyyy-MM-dd).</param>
        /// <param name="formatChanged">Indicates if the format has changed.</param>
        /// <returns>The next sequence number to use.</returns>
        private int GetNextSequenceNumber(string dateKey, bool formatChanged)
        {
            string extension = FormatUtil.GetExtension(_format);

            if (formatChanged)
            {
                int maxSequence = 0;
                string[] existingFiles = FileUtil.GetDailyLogFiles(_logDirectory, dateKey);

                foreach (string filePath in existingFiles)
                {
                    string fileName = Path.GetFileName(filePath);
                    string[] parts = fileName.Split('.');
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int seqNum) && parts[2].Equals(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        maxSequence = Math.Max(maxSequence, seqNum);
                    }
                }

                _sequenceNumbers[dateKey] = maxSequence + 1;
                return maxSequence + 1;
            }

            if (!_sequenceNumbers.TryGetValue(dateKey, out int sequence))
            {
                sequence = 1;
                _sequenceNumbers[dateKey] = sequence;
            }

            return sequence;
        }

        /// <summary>
        /// Checks if the file exceeds the maximum size and rotates if necessary.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <returns>The new file path to use.</returns>
        private string RotateFileIfNeeded(string filePath)
        {
            if (File.Exists(filePath) && FileUtil.GetFileSize(filePath) >= MAX_FILE_SIZE_BYTES)
            {
                // The file exists and exceeds the max size, create a new one
                string dateKey = DateTime.Now.ToString("yyyy-MM-dd");
                string extension = FormatUtil.GetExtension(_format);

                // Increment the sequence
                if (_sequenceNumbers.TryGetValue(dateKey, out int currentSeq))
                {
                    _sequenceNumbers[dateKey] = currentSeq + 1;
                }

                // Generate a new file name
                string newPath = Path.Combine(
                    _logDirectory,
                    $"{dateKey}.{_sequenceNumbers[dateKey]}.{extension}"
                );

                _currentLogFilePath = newPath;
                return newPath;
            }

            return filePath;
        }
    }
}
