using System.IO;

namespace EasySave.Models
{
    public class CompleteBackupStrategy : AbstractBackupStrategy
    {
        private string currentFile;
        private string destinationFile;
        private int totalFiles;
        private int remainFiles;


        public override List<string> GetFilesToCopy(string sourcePath, string targetPath)
        {
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }
            else
            {
                Directory.CreateDirectory(targetPath);
            }

            return ScanDirectory(sourcePath);
        }
    }
}
