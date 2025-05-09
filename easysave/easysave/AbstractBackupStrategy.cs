using System;
using System.Collections.Generic;
using System.IO;

namespace EasySave
{
    public abstract class AbstractBackupStrategy
    {
        protected StateManager stateManager;
        protected LogManager logManager;

        public AbstractBackupStrategy(StateManager stateManager, LogManager logManager)
        {
            this.stateManager = stateManager;
            this.logManager = logManager;
        }

        public abstract bool Execute(BackupJob job);

        public int CalculateTotalFiles(string source)
        {
            if (!Directory.Exists(source))
            {
                return 0;
            }

            int count = 0;

            // Get all files in the directory
            try
            {
                string[] files = Directory.GetFiles(source);
                count += files.Length;

                // Recursively count files in subdirectories
                string[] subdirectories = Directory.GetDirectories(source);
                foreach (string subdirectory in subdirectories)
                {
                    count += CalculateTotalFiles(subdirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while calculating total number of files: {ex.Message}");
            }

            return count;
        }

        protected List<string> ScanDirectory(string path)
        {
            List<string> fileList = new List<string>();

            try
            {
                // Add all files in this directory
                fileList.AddRange(Directory.GetFiles(path));

                // Recursively scan subdirectories
                foreach (string subdirectory in Directory.GetDirectories(path))
                {
                    fileList.AddRange(ScanDirectory(subdirectory));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while scanning directory {path}: {ex.Message}");
            }

            return fileList;
        }

        protected long GetFileSize(string path)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                return fileInfo.Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public long CalculateTotalSize(string source)
        {
            if (!Directory.Exists(source))
            {
                return 0;
            }

            long size = 0;

            try
            {
                // Calculate size of all files in the directory
                foreach (string file in Directory.GetFiles(source))
                {
                    size += GetFileSize(file);
                }

                // Recursively calculate size in subdirectories
                foreach (string subdirectory in Directory.GetDirectories(source))
                {
                    size += CalculateTotalSize(subdirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while calculating total size: {ex.Message}");
            }

            return size;
        }
    }
}
