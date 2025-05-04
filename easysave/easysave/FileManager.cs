using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace easysave
{
    internal class FileManager
    {
        public static void CopyFile(string sourcePath, string targetPath, string name)
        {
            CreateDirectory(targetPath, name);
            try
            {
                foreach (string file in System.IO.Directory.GetFiles(sourcePath))
                {
                    string fileName = System.IO.Path.GetFileName(file);
                    string destFile = System.IO.Path.Combine(targetPath, name);
                    destFile = System.IO.Path.Combine(destFile, fileName);
                    System.IO.File.Copy(file, destFile, true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error copying directory: " + e.Message);
            }
        }
        public static int GetFileSize(string filePath)
        {
            try
            {
                return (int)new System.IO.FileInfo(filePath).Length;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error getting file size: " + e.Message);
                return -1;
            }
        }

        private static void CreateDirectory(string path, string name)
        {
            string directoryPath = System.IO.Path.Combine(path, name);
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }
        }
    }
}
