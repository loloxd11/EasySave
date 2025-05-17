using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public interface ILogger
    {
        void LogFileTransfer(string jobName, string sourcePath, string targetPath,
            long fileSize, long transferTime, long encryptionTime);
        void LogEvent(string eventName);
        LogFormat GetCurrentFormat();
        string GetCurrentLogFilePath();
        bool IsReady();
    }
}
