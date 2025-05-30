using System.IO;
using System.Security.Cryptography;

namespace EasySave.Models
{
    /// <summary>
    /// Implements the differential backup strategy.
    /// Only files that have changed since the last complete backup are copied.
    /// </summary>
    public class DifferentialBackupStrategy : AbstractBackupStrategy
    {
        private string currentFile;
        private string destinationFile;
        private int totalFiles;
        private int remainFiles;
        private BackupType backupType = BackupType.Differential;

        /// <summary>
        /// Determines which files need to be copied for a differential backup.
        /// Only files that are new or have changed since the last backup are included.
        /// Also deletes files in the destination that no longer exist in the source.
        /// </summary>
        /// <param name="sourcePath">The source directory path.</param>
        /// <param name="targetPath">The target directory path.</param>
        /// <returns>List of file paths to copy from source to target.</returns>
        public override List<string> GetFilesToCopy(string sourcePath, string targetPath)
        {
            // Create the destination directory if it does not exist
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            // Get the list of source and destination files
            var sourceFiles = ScanDirectory(sourcePath);
            var destinationFiles = Directory.Exists(targetPath) ? ScanDirectory(targetPath) : new List<string>();

            var filesToCopy = new List<string>();
            var sourceRelativePaths = new HashSet<string>();

            // Iterate through all source files
            foreach (var sourceFile in sourceFiles)
            {
                // Get the relative path with respect to the source directory
                string relativePath = sourceFile.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                sourceRelativePaths.Add(relativePath);

                // Compute the corresponding destination file path
                string destinationFile = Path.Combine(targetPath, relativePath);

                bool shouldCopy = false;

                // Check if the file exists in the destination
                if (File.Exists(destinationFile))
                {
                    // Compare file hashes to determine if there are changes
                    string sourceHash = CalculateFileHash(sourceFile);
                    string destinationHash = CalculateFileHash(destinationFile);

                    if (sourceHash != destinationHash)
                    {
                        // Files are different, need to copy
                        shouldCopy = true;

                        // Delete the old file
                        File.Delete(destinationFile);
                    }
                }
                else
                {
                    // File does not exist in the destination, need to copy
                    shouldCopy = true;

                    // Create the destination directory if necessary
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                }

                if (shouldCopy)
                {
                    filesToCopy.Add(sourceFile);
                }
            }

            // Delete files that exist in the destination but not in the source
            foreach (var destinationFile in destinationFiles)
            {
                string relativePath = destinationFile.Substring(targetPath.Length).TrimStart(Path.DirectorySeparatorChar);

                if (!sourceRelativePaths.Contains(relativePath))
                {
                    // This file exists in the destination but not in the source, delete it
                    File.Delete(destinationFile);
                }
            }

            return filesToCopy;
        }

        /// <summary>
        /// Calculates the MD5 hash of a file.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <returns>The MD5 hash as a hexadecimal string.</returns>
        private string CalculateFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}