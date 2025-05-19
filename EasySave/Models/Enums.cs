using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    /// <summary>
    /// Represents the type of backup operation.
    /// </summary>
    public enum BackupType
    {
        /// <summary>
        /// Complete backup of all files.
        /// </summary>
        Complete,
        /// <summary>
        /// Differential backup, only files changed since last complete backup.
        /// </summary>
        Differential
    }

    /// <summary>
    /// Represents the current state of a backup job.
    /// </summary>
    public enum JobState
    {
        /// <summary>
        /// The job is not active.
        /// </summary>
        inactive,
        /// <summary>
        /// The job is currently running.
        /// </summary>
        active,
        /// <summary>
        /// The job has completed successfully.
        /// </summary>
        completed,
        /// <summary>
        /// The job has encountered an error.
        /// </summary>
        error
    }

    /// <summary>
    /// Represents the format used for logging.
    /// </summary>
    public enum LogFormat
    {
        /// <summary>
        /// Log in JSON format.
        /// </summary>
        JSON,
        /// <summary>
        /// Log in XML format.
        /// </summary>
        XML
    }
}
