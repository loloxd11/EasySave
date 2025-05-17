// LogLibrary/Factories/LoggerFactory.cs
using LogLibrary.Enums;
using LogLibrary.Interfaces;
using LogLibrary.Managers;

namespace LogLibrary.Factories
{
    /// <summary>
    /// Fabrique pour cr�er des instances de ILogger.
    /// </summary>
    public static class LoggerFactory
    {
        /// <summary>
        /// Cr�e une instance de ILogger avec le r�pertoire sp�cifi� et le format par d�faut (JSON).
        /// </summary>
        /// <param name="directory">Le r�pertoire o� stocker les logs.</param>
        /// <returns>Une instance de ILogger.</returns>
        public static ILogger Create(string directory)
        {
            return new LoggerManager(directory);
        }
        
        /// <summary>
        /// Cr�e une instance de ILogger avec le r�pertoire et le format sp�cifi�s.
        /// </summary>
        /// <param name="directory">Le r�pertoire o� stocker les logs.</param>
        /// <param name="format">Le format des logs.</param>
        /// <returns>Une instance de ILogger.</returns>
        public static ILogger Create(string directory, LogFormat format)
        {
            return new LoggerManager(directory, format);
        }
    }
}
