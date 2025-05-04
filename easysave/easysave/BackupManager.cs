using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using easysave;

//Create a backupmanager using the singleton pattern
namespace BackupManager
{
    internal class Manager
    {
        private static Manager _instance;
        private static readonly object _padlock = new object();
        public int MaxBackups { get; private set; } // Maximum number of backups

        private List<Backup> backups = new List<Backup>();
        public static Manager Instance
        {
            get
            {
                lock (_padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new Manager();
                    }
                    return _instance;
                }
            }
        }
        private Manager()
        {
            MaxBackups = 5; // Set the maximum number of backups
        }
        public void Backup(List<int> BackupsNbr)
        {
            foreach (int i in BackupsNbr)
            {
                try {
                    FileManager.CopyFile(backups[i].SourcePath, backups[i].TargetPath, backups[i].Name);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Job not found");
                    break;
                }
                RemoveBackupJob(i);

            }

        }
        public void AddBackupJob(string sourcePath, string targetPath, bool type, string name)
        {
            Backup backup = new Backup(sourcePath, targetPath, type, name);
            backups.Add(backup);
        }

        public void RemoveBackupJob(int Index)
        {
            try
            {
                backups.Remove(backups[Index]);
                Console.WriteLine($"removed successfully.");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine($"Not found.");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine($"Not found.");
            }

        }

        public void ListBackups()
        {
            for (int i = 0; i < backups.Count; i++)
            {
                Console.WriteLine($"Backup {i}: {backups[i].Name} - {backups[i].SourcePath} -> {backups[i].TargetPath}");
            }
        }

        public void UpdateBackupJob(int index, string name, string newSourcePath, string newTargetPath, bool newType)
        {
            if (index < 0 || index >= backups.Count)
            {
                Console.WriteLine("Index out of range.");
                return;
            }
            backups[index].SetSourcePath(newSourcePath);
            backups[index].SetTargetPath(newTargetPath);
            backups[index].SetType(newType);
            backups[index].SetName(name);
            Console.WriteLine($"Backup job '{name}' updated successfully.");

        }


    }
}
