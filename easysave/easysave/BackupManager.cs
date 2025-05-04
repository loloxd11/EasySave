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
            
        }
        public void Backup(List<int> BackupsNbr)
        {
            foreach (int i in BackupsNbr)
            {
                FileManager.CopyFile(backups[i].SourcePath, backups[i].TargetPath, backups[i].Name);
            }

        }
        public void AddBackupJob(string sourcePath, string targetPath, bool type, string name)
        {
            Backup backup = new Backup(sourcePath, targetPath, type, name);
            backups.Add(backup);
        }
        //get file of the backup

    }
}
