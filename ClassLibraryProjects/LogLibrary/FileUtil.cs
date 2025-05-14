// LogLibrary/Utils/FileUtil.cs
using System;
using System.IO;
using System.Linq;

namespace LogLibrary.Utils
{
    /// <summary>
    /// Utilitaires pour les opérations sur les fichiers.
    /// </summary>
    public static class FileUtil
    {
        /// <summary>
        /// S'assure qu'un répertoire existe, le crée si nécessaire.
        /// </summary>
        /// <param name="path">Le chemin du répertoire.</param>
        /// <returns>True si le répertoire a été créé, false s'il existait déjà.</returns>
        public static bool EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Obtient la taille d'un fichier.
        /// </summary>
        /// <param name="path">Le chemin du fichier.</param>
        /// <returns>La taille du fichier en octets.</returns>
        public static long GetFileSize(string path)
        {
            if (!File.Exists(path))
                return 0;
                
            return new FileInfo(path).Length;
        }
        
        /// <summary>
        /// Récupère les fichiers de log pour une date spécifique.
        /// </summary>
        /// <param name="directory">Le répertoire des logs.</param>
        /// <param name="datePattern">Le motif de date à rechercher.</param>
        /// <returns>Un tableau de chemins de fichiers.</returns>
        public static string[] GetDailyLogFiles(string directory, string datePattern)
        {
            if (!Directory.Exists(directory))
                return Array.Empty<string>();
                
            return Directory.GetFiles(directory, $"{datePattern}*.*")
                .OrderBy(f => f)
                .ToArray();
        }
        
        /// <summary>
        /// Ajoute du contenu à un fichier.
        /// </summary>
        /// <param name="filePath">Le chemin du fichier.</param>
        /// <param name="content">Le contenu à ajouter.</param>
        public static void AppendToFile(string filePath, string content)
        {
            string? directory = Path.GetDirectoryName(filePath);
            
            if (!string.IsNullOrEmpty(directory))
                EnsureDirectoryExists(directory);
                
            File.AppendAllText(filePath, content + Environment.NewLine);
        }
    }
}
