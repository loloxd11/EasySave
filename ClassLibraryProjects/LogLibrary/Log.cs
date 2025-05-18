// LogLibrary/Class1.cs
using LogLibrary.Enums;
using LogLibrary.Factories;
using LogLibrary.Interfaces;

namespace LogLibrary
{
    /// <summary>
    /// Main entry point for the logging library.
    /// Provides methods to create loggers with specified configurations.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Creates a logger with the specified directory.
        /// </summary>
        /// <param name="directory">The directory where logs will be stored.</param>
        /// <returns>An instance of ILogger.</returns>
        public static ILogger CreateLogger(string directory)
        {
            // Calls the LoggerFactory to create a logger with the given directory.
            return LoggerFactory.Create(directory);
        }

        /// <summary>
        /// Creates a logger with the specified directory and log format.
        /// </summary>
        /// <param name="directory">The directory where logs will be stored.</param>
        /// <param name="format">The format of the logs (JSON or XML).</param>
        /// <returns>An instance of ILogger.</returns>
        public static ILogger CreateLogger(string directory, LogFormat format)
        {
            // Calls the LoggerFactory to create a logger with the given directory and format.
            return LoggerFactory.Create(directory, format);
        }
    }
}
