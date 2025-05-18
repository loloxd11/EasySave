using System.Collections.Generic;
using System.Xml.Serialization;
using LogLibrary.Models; // Ajoutez ceci

/// <summary>
/// Represents a collection of log entries for serialization and manipulation.
/// </summary>
[XmlRoot("LogEntries")]
public class LogEntries
{
    /// <summary>
    /// Gets or sets the list of log entries.
    /// </summary>
    [XmlElement("LogEntry")]
    public List<LogEntry> Entries { get; set; } = new();
}
