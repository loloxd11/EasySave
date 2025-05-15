// LogLibrary/Factories/LoggerFactory.cs
using LogLibrary.Enums;
using LogLibrary.Interfaces;
using LogLibrary.Managers;

namespace LogLibrary.Factories
{
    /// <summary>
    /// Fabrique pour créer des instances de ILogger.
    /// </summary>
    public static class LoggerFactory
    {
        /// <summary>
        /// Crée une instance de ILogger avec le répertoire spécifié et le format par défaut (JSON).
        /// </summary>
        /// <param name="directory">Le répertoire où stocker les logs.</param>
        /// <returns>Une instance de ILogger.</returns>
        public static ILogger Create(string directory)
        {
            return new LoggerManager(directory);
        }
        
        /// <summary>
        /// Crée une instance de ILogger avec le répertoire et le format spécifiés.
        /// </summary>
        /// <param name="directory">Le répertoire où stocker les logs.</param>
        /// <param name="format">Le format des logs.</param>
        /// <returns>Une instance de ILogger.</returns>
        public static ILogger Create(string directory, LogFormat format)
        {
            return new LoggerManager(directory, format);
        }
    }
}
