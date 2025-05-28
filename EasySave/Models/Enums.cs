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
        error,
        /// <summary>
        /// The job is currently paused.
        /// 
        paused,
        /// <summary>
        /// The job is stopped
        /// 
        stopped
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
