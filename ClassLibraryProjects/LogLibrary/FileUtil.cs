// LogLibrary/Utils/FileUtil.cs
using System;
using System.IO;
using System.Linq;

namespace LogLibrary.Utils
{
    /// <summary>
    /// Utilitaires pour les op�rations sur les fichiers.
    /// </summary>
    public static class FileUtil
    {
        /// <summary>
        /// S'assure qu'un r�pertoire existe, le cr�e si n�cessaire.
        /// </summary>
        /// <param name="path">Le chemin du r�pertoire.</param>
        /// <returns>True si le r�pertoire a �t� cr��, false s'il existait d�j�.</returns>
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
        /// R�cup�re les fichiers de log pour une date sp�cifique.
        /// </summary>
        /// <param name="directory">Le r�pertoire des logs.</param>
        /// <param name="datePattern">Le motif de date � rechercher.</param>
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
        /// Ajoute du contenu � un fichier.
        /// </summary>
        /// <param name="filePath">Le chemin du fichier.</param>
        /// <param name="content">Le contenu � ajouter.</param>
        public static void AppendToFile(string filePath, string content)
        {
            string? directory = Path.GetDirectoryName(filePath);
            
            if (!string.IsNullOrEmpty(directory))
                EnsureDirectoryExists(directory);
                
            File.AppendAllText(filePath, content + Environment.NewLine);
        }
    }
}
