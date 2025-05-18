using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public enum BackupType
    {
        Complete,
        Differential
    }

    public enum JobState
    {
        inactive,
        active,
        completed,
        error
    }

    public enum LogFormat
    {
        JSON,
        XML
    }
}
