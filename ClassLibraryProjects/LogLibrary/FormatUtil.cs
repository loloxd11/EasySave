// LogLibrary/Utils/FormatUtil.cs
using LogLibrary.Enums;
using LogLibrary.Models;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;

namespace LogLibrary.Utils
{
    /// <summary>
    /// Utilitaires pour le formatage des entr�es de log.
    /// </summary>
    public static class FormatUtil
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        
        /// <summary>
        /// Convertit une entr�e de log en format JSON.
        /// </summary>
        /// <param name="entry">L'entr�e de log � convertir.</param>
        /// <returns>Une cha�ne JSON repr�sentant l'entr�e de log.</returns>
        public static string ToJson(LogEntry entry)
        {
            return JsonSerializer.Serialize(entry, _jsonOptions);
        }
        
        /// <summary>
        /// Convertit une entr�e de log en format XML.
        /// </summary>
        /// <param name="entry">L'entr�e de log � convertir.</param>
        /// <returns>Une cha�ne XML repr�sentant l'entr�e de log.</returns>
        public static string ToXml(LogEntry entry)
        {
            using StringWriter writer = new();
            XmlSerializer serializer = new(typeof(LogEntry));
            serializer.Serialize(writer, entry);
            return writer.ToString();
        }
        
        /// <summary>
        /// Obtient l'extension de fichier associ�e � un format de log.
        /// </summary>
        /// <param name="format">Le format de log.</param>
        /// <returns>L'extension de fichier correspondante.</returns>
        public static string GetExtension(LogFormat format)
        {
            return format switch
            {
                LogFormat.XML => "xml",
                _ => "json"
            };
        }
    }
}
