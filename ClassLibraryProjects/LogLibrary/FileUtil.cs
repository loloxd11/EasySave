// LogLibrary/Utils/FileUtil.cs
using System;
using System.IO;
using System.Linq;

namespace LogLibrary.Utils
{
    /// <summary>
    /// Utilities for file operations.
    /// </summary>
    public static class FileUtil
    {
        /// <summary>
        /// Ensures that a directory exists, creating it if necessary.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        /// <returns>True if the directory was created, false if it already existed.</returns>
        public static bool EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the size of a file.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <returns>The size of the file in bytes.</returns>
        public static long GetFileSize(string path)
        {
            if (!File.Exists(path))
                return 0;

            return new FileInfo(path).Length;
        }

        /// <summary>
        /// Retrieves log files for a specific date based on a date pattern.
        /// </summary>
        /// <param name="directory">The directory containing the log files.</param>
        /// <param name="datePattern">The date pattern to search for in file names.</param>
        /// <returns>An array of file paths matching the date pattern.</returns>
        public static string[] GetDailyLogFiles(string directory, string datePattern)
        {
            if (!Directory.Exists(directory))
                return Array.Empty<string>();

            return Directory.GetFiles(directory, $"{datePattern}*.*")
                .OrderBy(f => f)
                .ToArray();
        }

        /// <summary>
        /// Appends content to a file, creating the file and its directory if necessary.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="content">The content to append to the file.</param>
        public static void AppendToFile(string filePath, string content)
        {
            string? directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory))
                EnsureDirectoryExists(directory);

            File.AppendAllText(filePath, content + Environment.NewLine);
        }
    }
}
