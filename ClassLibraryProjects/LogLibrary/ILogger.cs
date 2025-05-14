// LogLibrary/Interfaces/ILogger.cs
using LogLibrary.Enums;
using System;
using System.Collections.Generic;

namespace LogLibrary.Interfaces
{
    /// <summary>
    /// Interface définissant les opérations de journalisation.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Journalise un transfert de fichier.
        /// </summary>
        /// <param name="jobName">Le nom du job associé au transfert.</param>
        /// <param name="sourcePath">Le chemin du fichier source.</param>
        /// <param name="targetPath">Le chemin du fichier de destination.</param>
        /// <param name="fileSize">La taille du fichier en octets.</param>
        /// <param name="transferTime">Le temps de transfert en millisecondes.</param>
        /// <param name="encryptionTime">Le temps de cryptage en millisecondes.</param>
        void LogTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, long encryptionTime);
        
        /// <summary>
        /// Journalise un événement avec des propriétés personnalisées.
        /// </summary>
        /// <param name="eventName">Le nom de l'événement.</param>
        void LogEvent(string eventName);
        
        /// <summary>
        /// Obtient le format de log actuel.
        /// </summary>
        /// <returns>Le format de log actuel.</returns>
        LogFormat GetCurrentFormat();
        
        /// <summary>
        /// Définit le format de log à utiliser.
        /// </summary>
        /// <param name="format">Le format de log à utiliser.</param>
        void SetFormat(LogFormat format);
        
        /// <summary>
        /// Obtient le chemin du fichier de log actuel.
        /// </summary>
        /// <returns>Le chemin du fichier de log actuel.</returns>
        string GetCurrentLogFilePath();
        
        /// <summary>
        /// Vérifie si le service de log est prêt.
        /// </summary>
        /// <returns>True si le service est prêt, sinon false.</returns>
        bool IsReady();
    }
}
