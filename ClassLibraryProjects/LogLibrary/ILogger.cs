// LogLibrary/Interfaces/ILogger.cs
using LogLibrary.Enums;
using System;
using System.Collections.Generic;

namespace LogLibrary.Interfaces
{
    /// <summary>
    /// Interface d�finissant les op�rations de journalisation.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Journalise un transfert de fichier.
        /// </summary>
        /// <param name="jobName">Le nom du job associ� au transfert.</param>
        /// <param name="sourcePath">Le chemin du fichier source.</param>
        /// <param name="targetPath">Le chemin du fichier de destination.</param>
        /// <param name="fileSize">La taille du fichier en octets.</param>
        /// <param name="transferTime">Le temps de transfert en millisecondes.</param>
        /// <param name="encryptionTime">Le temps de cryptage en millisecondes.</param>
        void LogTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, long encryptionTime);
        
        /// <summary>
        /// Journalise un �v�nement avec des propri�t�s personnalis�es.
        /// </summary>
        /// <param name="eventName">Le nom de l'�v�nement.</param>
        void LogEvent(string eventName);
        
        /// <summary>
        /// Obtient le format de log actuel.
        /// </summary>
        /// <returns>Le format de log actuel.</returns>
        LogFormat GetCurrentFormat();
        
        /// <summary>
        /// D�finit le format de log � utiliser.
        /// </summary>
        /// <param name="format">Le format de log � utiliser.</param>
        void SetFormat(LogFormat format);
        
        /// <summary>
        /// Obtient le chemin du fichier de log actuel.
        /// </summary>
        /// <returns>Le chemin du fichier de log actuel.</returns>
        string GetCurrentLogFilePath();
        
        /// <summary>
        /// V�rifie si le service de log est pr�t.
        /// </summary>
        /// <returns>True si le service est pr�t, sinon false.</returns>
        bool IsReady();
    }
}
