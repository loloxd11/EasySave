// LogLibrary/Models/LogEntry.cs
using System;
using System.Collections.Generic;

namespace LogLibrary.Models
{
    /// <summary>
    /// Représente une entrée de log contenant les détails d'une opération.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// L'horodatage de création de l'entrée de log.
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Le nom du job associé à cette entrée.
        /// </summary>
        public string JobName { get; set; } = string.Empty;
        
        /// <summary>
        /// Le chemin source de l'opération.
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Le chemin de destination de l'opération.
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
