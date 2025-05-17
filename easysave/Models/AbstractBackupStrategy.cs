using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public abstract class AbstractBackupStrategy
    {
        protected List<IObserver> observers = new List<IObserver>();
        protected JobState state;
        protected int totalFiles;
        protected long totalSize;
        protected int progression;
        protected long LastFileTime;
        protected string name;

        public void AttachObserver(IObserver observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
        }

        public void NotifyObserver(string action, string name, string sourcePath, string targetPath,
            long fileSize, long transferTime, long encryptionTime)
        {
            foreach (var observer in observers)
            {
                observer.Update(action, name, BackupType.Complete, state, sourcePath, targetPath,
                    totalFiles, totalSize, progression);
            }
        }

        public void UpdateProgress(int files, long size)
        {
            progression = files;
            // Update logic
        }

        public void UpdateCurrentFile(string source, string target)
        {
            // Update current file being processed
            LastFileTime = DateTime.Now.Ticks;
            NotifyObserver("processing", name, source, target, 0, 0, 0);
        }

        public abstract bool Execute(string name, string src, string dst, string order);

        public int CalculateTotalFiles(string source)
        {
            var files = ScanDirectory(source);
            return files.Count;
        }

        protected List<string> ScanDirectory(string path)
        {
            var result = new List<string>();

            try
            {
                // Add files in the current directory
                result.AddRange(Directory.GetFiles(path));

                // Recursively add files from subdirectories
                foreach (var directory in Directory.GetDirectories(path))
                {
                    result.AddRange(ScanDirectory(directory));
                }
            }
            catch (Exception ex)
            {
                // Log exception
                Console.WriteLine($"Error scanning directory {path}: {ex.Message}");
            }

            return result;
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
            long size = 0;
            List<string> files = ScanDirectory(source);

            foreach (string file in files)
            {
                size += GetFileSize(file);
            }

            return size;
        }
    }
}
