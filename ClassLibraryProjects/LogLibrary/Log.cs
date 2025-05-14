// LogLibrary/Class1.cs
using LogLibrary.Enums;
using LogLibrary.Factories;
using LogLibrary.Interfaces;

namespace LogLibrary
{
    /// <summary>
    /// Point d'entrée principal de la bibliothèque de logging.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Crée un logger avec le répertoire spécifié.
        /// </summary>
        /// <param name="directory">Le répertoire où stocker les logs.</param>
        /// <returns>Une instance de ILogger.</returns>
        public static ILogger CreateLogger(string directory)
        {
            return LoggerFactory.Create(directory);
        }
        
        /// <summary>
        /// Crée un logger avec le répertoire et le format spécifiés.
        /// </summary>
        /// <param name="directory">Le répertoire où stocker les logs.</param>
        /// <param name="format">Le format des logs (JSON ou XML).</param>
        /// <returns>Une instance de ILogger.</returns>
        public static ILogger CreateLogger(string directory, LogFormat format)
        {
            return LoggerFactory.Create(directory, format);
        }
    }
}
