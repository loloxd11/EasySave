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

        public abstract bool Execute(string source, string target, string name);

        public int CalculateTotalFiles(string source)
        {
            if (!Directory.Exists(source))
            {
                return 0;
            }

            int count = 0;

            // Obtenir tous les fichiers dans le répertoire
            try
            {
                string[] files = Directory.GetFiles(source);
                count += files.Length;

                // Compter récursivement les fichiers dans les sous-répertoires
                string[] subdirectories = Directory.GetDirectories(source);
                foreach (string subdirectory in subdirectories)
                {
                    count += CalculateTotalFiles(subdirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du calcul du nombre total de fichiers: {ex.Message}");
            }

            return count;
        }

        protected List<string> ScanDirectory(string path)
        {
            List<string> fileList = new List<string>();

            try
            {
                // Ajouter tous les fichiers de ce répertoire
                fileList.AddRange(Directory.GetFiles(path));

                // Scanner récursivement les sous-répertoires
                foreach (string subdirectory in Directory.GetDirectories(path))
                {
                    fileList.AddRange(ScanDirectory(subdirectory));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du scan du répertoire {path}: {ex.Message}");
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
    }
}
