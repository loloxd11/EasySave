using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public class BackupJob
    {
        private string name;
        private string src;
        private string dst;
        private BackupType type;
        private AbstractBackupStrategy backupStrategy;

        public BackupJob(string name, string source, string target, BackupType type, AbstractBackupStrategy strategy)
        {
            this.name = name;
            this.src = source;
            this.dst = target;
            this.type = type;
            this.backupStrategy = strategy;
        }

        public bool Execute()
        {
            return backupStrategy.Execute(name, src, dst, "default");
        }

        // Getters and setters
        public string Name => name;
        public string Source => src;
        public string Destination => dst;
        public BackupType Type => type;
    }
}
