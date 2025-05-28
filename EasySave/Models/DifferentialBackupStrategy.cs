using System.IO;
using System.Security.Cryptography;

namespace EasySave.Models
{
    public class DifferentialBackupStrategy : AbstractBackupStrategy
    {
        private string currentFile;
        private string destinationFile;
        private int totalFiles;
        private int remainFiles;
        private BackupType backupType = BackupType.Differential;

        public override List<string> GetFilesToCopy(string sourcePath, string targetPath)
        {
            // Créer le répertoire de destination s'il n'existe pas
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            // Obtenir la liste des fichiers source et destination
            var sourceFiles = ScanDirectory(sourcePath);
            var destinationFiles = Directory.Exists(targetPath) ? ScanDirectory(targetPath) : new List<string>();

            var filesToCopy = new List<string>();
            var sourceRelativePaths = new HashSet<string>();

            // Parcourir tous les fichiers sources
            foreach (var sourceFile in sourceFiles)
            {
                // Obtenir le chemin relatif par rapport au répertoire source
                string relativePath = sourceFile.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                sourceRelativePaths.Add(relativePath);

                // Calculer le chemin destination correspondant
                string destinationFile = Path.Combine(targetPath, relativePath);

                bool shouldCopy = false;

                // Vérifier si le fichier existe dans la destination
                if (File.Exists(destinationFile))
                {
                    // Comparer les hash des fichiers pour déterminer s'il y a eu des changements
                    string sourceHash = CalculateFileHash(sourceFile);
                    string destinationHash = CalculateFileHash(destinationFile);

                    if (sourceHash != destinationHash)
                    {
                        // Les fichiers sont différents, on doit copier
                        shouldCopy = true;

                        // Supprimer l'ancien fichier
                        File.Delete(destinationFile);
                    }
                }
                else
                {
                    // Le fichier n'existe pas dans la destination, on doit le copier
                    shouldCopy = true;

                    // Créer le répertoire de destination si nécessaire
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                }

                if (shouldCopy)
                {
                    filesToCopy.Add(sourceFile);
                }
            }

            // Supprimer les fichiers qui existent dans la destination mais pas dans la source
            foreach (var destinationFile in destinationFiles)
            {
                string relativePath = destinationFile.Substring(targetPath.Length).TrimStart(Path.DirectorySeparatorChar);

                if (!sourceRelativePaths.Contains(relativePath))
                {
                    // Ce fichier existe dans la destination mais pas dans la source, le supprimer
                    File.Delete(destinationFile);
                }
            }

            return filesToCopy;
        }

        /// <summary>
        /// Calcule le hash MD5 d'un fichier
        /// </summary>
        /// <param name="filePath">Chemin du fichier</param>
        /// <returns>Le hash MD5 sous forme de chaîne hexadécimale</returns>
        private string CalculateFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}