namespace EasySave
{
    /// <summary>
    /// Represents the type of backup that can be performed.
    /// </summary>
    public enum BackupType
    {
        /// <summary>
        /// A complete backup, which copies all files regardless of changes.
        /// </summary>
        Complete,

        /// <summary>
        /// A differential backup, which copies only files that have changed since the last complete backup.
        /// </summary>
        Differential
    }
}
