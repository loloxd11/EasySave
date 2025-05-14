// LogLibrary/Class1.cs
using LogLibrary.Enums;
using LogLibrary.Factories;
using LogLibrary.Interfaces;

namespace LogLibrary
{
    /// <summary>
    /// Point d'entr�e principal de la biblioth�que de logging.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Cr�e un logger avec le r�pertoire sp�cifi�.
        /// </summary>
        /// <param name="directory">Le r�pertoire o� stocker les logs.</param>
        /// <returns>Une instance de ILogger.</returns>
        public static ILogger CreateLogger(string directory)
        {
            return LoggerFactory.Create(directory);
        }
        
        /// <summary>
        /// Cr�e un logger avec le r�pertoire et le format sp�cifi�s.
        /// </summary>
        /// <param name="directory">Le r�pertoire o� stocker les logs.</param>
        /// <param name="format">Le format des logs (JSON ou XML).</param>
        /// <returns>Une instance de ILogger.</returns>
        public static ILogger CreateLogger(string directory, LogFormat format)
        {
            return LoggerFactory.Create(directory, format);
        }
    }
}
