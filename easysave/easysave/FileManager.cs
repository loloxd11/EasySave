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
            string targetDirectory = System.IO.Path.Combine(targetPath, name);
            CreateDirectory(targetPath, name);

            try
            {
                // Copier tous les fichiers dans le répertoire source
                foreach (string file in System.IO.Directory.GetFiles(sourcePath, "*.*", System.IO.SearchOption.AllDirectories))
                {
                    // Obtenir le chemin relatif du fichier par rapport au répertoire source
                    string relativePath = System.IO.Path.GetRelativePath(sourcePath, file);

                    // Construire le chemin complet dans le répertoire cible
                    string destFile = System.IO.Path.Combine(targetDirectory, relativePath);

                    // Créer les sous-dossiers nécessaires dans le répertoire cible
                    string destDirectory = System.IO.Path.GetDirectoryName(destFile)!;
                    if (!System.IO.Directory.Exists(destDirectory))
                    {
                        System.IO.Directory.CreateDirectory(destDirectory);
                    }

                    // Copier le fichier
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

        public static void CreateDirectory(string path, string name)
        {
            string directoryPath = System.IO.Path.Combine(path, name);
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }
        }

    }
}
