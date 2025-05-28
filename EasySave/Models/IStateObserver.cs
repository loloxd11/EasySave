namespace EasySave.Models
{
    /// <summary>
    /// Interface for observing state changes in backup jobs.
    /// Implementers will be notified when a backup job's state changes.
    /// </summary>
    public interface IStateObserver
    {
        /// <summary>
        /// Called when the state of a backup job changes.
        /// </summary>
        /// <param name="action">The action performed (e.g., start, stop, complete).</param>
        /// <param name="name">The name of the backup job.</param>
        /// <param name="type">The type of backup (Complete or Differential).</param>
        /// <param name="state">The current state of the job (inactive, active, completed, error).</param>
        /// <param name="sourcePath">The source directory path for the backup.</param>
        /// <param name="targetPath">The target directory path for the backup.</param>
        /// <param name="totalFiles">The total number of files involved in the backup.</param>
        /// <param name="totalSize">The total size of the files in bytes.</param>
        /// <param name="progression">The progression percentage of the backup job.</param>
        void Update(string action, string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, int progression);
    }
}
