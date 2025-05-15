// LogLibrary/Utils/FormatUtil.cs
using LogLibrary.Enums;
using LogLibrary.Models;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;

namespace LogLibrary.Utils
{
    /// <summary>
    /// Utilities for formatting log entries.
    /// </summary>
    public static class FormatUtil
    {
        // JSON serializer options with indentation enabled for better readability.
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        /// <summary>
        /// Converts a log entry to JSON format.
        /// </summary>
        /// <param name="entry">The log entry to convert.</param>
        /// <returns>A JSON string representing the log entry.</returns>
        public static string ToJson(LogEntry entry)
        {
            return JsonSerializer.Serialize(entry, _jsonOptions);
        }

        /// <summary>
        /// Converts a log entry to XML format.
        /// </summary>
        /// <param name="entry">The log entry to convert.</param>
        /// <returns>An XML string representing the log entry.</returns>
        public static string ToXml(LogEntry entry)
        {
            using StringWriter writer = new();
            XmlSerializer serializer = new(typeof(LogEntry));
            serializer.Serialize(writer, entry);
            return writer.ToString();
        }

        /// <summary>
        /// Gets the file extension associated with a log format.
        /// </summary>
        /// <param name="format">The log format.</param>
        /// <returns>The corresponding file extension.</returns>
        public static string GetExtension(LogFormat format)
        {
            return format switch
            {
                LogFormat.XML => "xml", // Returns "xml" for XML format.
                _ => "json"            // Defaults to "json" for JSON format.
            };
        }
    }
}
