// LogLibrary/Models/LogEntry.cs
using System;
using System.Collections.Generic;

namespace LogLibrary.Models
{
    /// <summary>
    /// Repr�sente une entr�e de log contenant les d�tails d'une op�ration.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// L'horodatage de cr�ation de l'entr�e de log.
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Le nom du job associ� � cette entr�e.
        /// </summary>
        public string JobName { get; set; } = string.Empty;
        
        /// <summary>
        /// Le chemin source de l'op�ration.
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Le chemin de destination de l'op�ration.
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;
        
        /// <summary>
        /// La taille du fichier en octets.
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// Le temps de transfert en millisecondes.
        /// </summary>
        public long TransferTimeMs { get; set; }
        
        /// <summary>
        /// Le temps de cryptage en millisecondes.
        /// </summary>
        public long EncryptionTimeMs { get; set; }
    }
}
