namespace EasySave
{
    /// <summary>
    /// Represents the state of a job in the EasySave application.
    /// </summary>
    public enum JobState
    {
        /// <summary>
        /// The job is not currently active.
        /// </summary>
        Inactive,

        /// <summary>
        /// The job is currently in progress.
        /// </summary>
        Active,

        /// <summary>
        /// The job has been successfully completed.
        /// </summary>
        Completed,

        /// <summary>
        /// The job encountered an error during execution.
        /// </summary>
        Error
    }
}
