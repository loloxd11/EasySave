using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


internal class Backup
{
    public string SourcePath { get; private set; }
    public string TargetPath { get; private set; }
    public bool Type { get; private set; } // False for full backup, true for differential backup
    public string Name { get; private set; } // Name of the backup job

    public Backup(string sourcePath, string targetPath, bool type, string name)
        => (SourcePath, TargetPath, Type, Name) = (sourcePath, targetPath, type, name);
}

