// LogLibrary/Models/LogEntry.cs
using System;
using System.Collections.Generic;

namespace LogLibrary.Models
{
    /// <summary>
    /// Represents a log entry containing details of an operation.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// The timestamp indicating when the log entry was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The name of the job associated with this log entry.
        /// </summary>
        public string JobName { get; set; } = string.Empty;

        /// <summary>
        /// The source path of the operation.
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// The target path of the operation.
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>
        /// The size of the file in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// The time taken for the transfer operation, in milliseconds.
        /// </summary>
        public long TransferTimeMs { get; set; }

        /// <summary>
        /// The time taken for the encryption process, in milliseconds.
        /// </summary>
        public long EncryptionTimeMs { get; set; }
    }
}
