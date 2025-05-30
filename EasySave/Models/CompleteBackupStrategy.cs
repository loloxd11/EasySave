using System.IO;

namespace EasySave.Models
{
    /// <summary>
    /// Complete backup strategy implementation.
    /// This strategy deletes the target directory (if it exists) and copies all files from the source.
    /// </summary>
    public class CompleteBackupStrategy : AbstractBackupStrategy
    {
        // Path of the current file being processed
        private string currentFile;
        // Path of the destination file being processed
        private string destinationFile;
        // Total number of files to copy
        private int totalFiles;
        // Number of files remaining to copy
        private int remainFiles;

        /// <summary>
        /// Gets the list of files to copy for a complete backup.
        /// If the target directory exists, it is deleted before copying.
        /// </summary>
        /// <param name="sourcePath">The source directory path.</param>
        /// <param name="targetPath">The target directory path.</param>
        /// <returns>List of file paths to copy from the source directory.</returns>
        public override List<string> GetFilesToCopy(string sourcePath, string targetPath)
        {
            // If the target directory exists, delete it and all its contents
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }
            else
            {
                // Otherwise, create the target directory
                Directory.CreateDirectory(targetPath);
            }

            // Recursively scan the source directory and return all file paths
            return ScanDirectory(sourcePath);
        }
    }
}
