namespace EasySave.Models
{
    /// <summary>
    /// Interface for observer classes that need to be notified of backup job updates.
    /// </summary>
    public interface IObserver
    {
        /// <summary>
        /// Called to notify the observer of a change in a backup job.
        /// </summary>
        /// <param name="action">The action performed (e.g., start, stop, update).</param>
        /// <param name="name">The name of the backup job.</param>
        /// <param name="type">The type of backup (Complete or Differential).</param>
        /// <param name="state">The current state of the job (inactive, active, completed, error).</param>
        /// <param name="sourcePath">The source path of the backup.</param>
        /// <param name="targetPath">The target path of the backup.</param>
        /// <param name="totalFiles">The total number of files involved in the backup.</param>
        /// <param name="totalSize">The total size of the files in bytes.</param>
        /// <param name="transferTime">The time taken for file transfer, if applicable.</param>
        /// <param name="encryptionTime">The time taken for encryption, if applicable.</param>
        /// <param name="progression">The progression percentage of the backup job.</param>
        void Update(string action, string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, long transferTime, long encryptionTime, int progression);

    }
}
