// LogLibrary/Managers/LoggerManager.cs
using LogLibrary.Enums;
using LogLibrary.Interfaces;
using LogLibrary.Models;
using LogLibrary.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogLibrary.Managers
{
    /// <summary>
    /// Implémentation concrète du service de journalisation.
    /// </summary>
    public class LoggerManager : ILogger
    {
        // Taille maximale des fichiers de log (1 Mo)
        private const long MAX_FILE_SIZE_BYTES = 1_048_576;
        
        private readonly string _logDirectory;
        private LogFormat _format;
        private Dictionary<string, int> _sequenceNumbers = new();
        private string? _currentLogFilePath;
        
        /// <summary>
        /// Initialise une nouvelle instance avec le répertoire spécifié.
        /// </summary>
        /// <param name="directory">Le répertoire des fichiers de log.</param>
        public LoggerManager(string directory)
            : this(directory, LogFormat.JSON)
        {
        }
        
        /// <summary>
        /// Initialise une nouvelle instance avec le répertoire et le format spécifiés.
        /// </summary>
        /// <param name="directory">Le répertoire des fichiers de log.</param>
        /// <param name="format">Le format des logs.</param>
        public LoggerManager(string directory, LogFormat format)
        {
            _logDirectory = directory;
            _format = format;
            
            // Assurer que le répertoire existe
            FileUtil.EnsureDirectoryExists(_logDirectory);
        }
        
        /// <summary>
        /// Récupère le format actuel des logs.
        /// </summary>
        /// <returns>Le format actuel.</returns>
        public LogFormat GetCurrentFormat()
        {
            return _format;
        }
        
        /// <summary>
        /// Définit le format des logs.
        /// </summary>
        /// <param name="format">Le nouveau format à utiliser.</param>
        public void SetFormat(LogFormat format)
        {
            if (_format == format)
                return;
                
            _format = format;
            _currentLogFilePath = null; // Force la création d'un nouveau fichier
        }
        
        /// <summary>
        /// Récupère le chemin du fichier de log actuel.
        /// </summary>
        /// <returns>Le chemin du fichier de log courant.</returns>
        public string GetCurrentLogFilePath()
        {
            if (_currentLogFilePath == null)
            {
                _currentLogFilePath = GetFilePath(DateTime.Now);
            }
            
            return _currentLogFilePath;
        }
        
        /// <summary>
        /// Vérifie si le service de log est prêt.
        /// </summary>
        /// <returns>True si le service est prêt à être utilisé.</returns>
        public bool IsReady()
        {
            try
            {
                string testFile = Path.Combine(_logDirectory, "test.tmp");
                File.WriteAllText(testFile, "Test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Journalise un transfert de fichier.
        /// </summary>
        /// <param name="jobName">Le nom du job.</param>
        /// <param name="sourcePath">Le chemin source.</param>
        /// <param name="targetPath">Le chemin de destination.</param>
        /// <param name="fileSize">La taille du fichier.</param>
        /// <param name="transferTime">Le temps de transfert.</param>
        /// <param name="encryptionTime">Le temps de cryptage.</param>
        public void LogTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, long encryptionTime)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                JobName = jobName,
                SourcePath = sourcePath,
                TargetPath = targetPath,
                FileSize = fileSize,
                TransferTimeMs = transferTime,
                EncryptionTimeMs = encryptionTime
            };
            
            string content = CreateLogEntry(entry);
            string filePath = GetCurrentLogFilePath();
            filePath = RotateFileIfNeeded(filePath);
            
            FileUtil.AppendToFile(filePath, content);
        }
        
        /// <summary>
        /// Journalise un événement avec des propriétés personnalisées.
        /// </summary>
        /// <param name="eventName">Le nom de l'événement.</param>
        public void LogEvent(string eventName)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                JobName = eventName
            };
            
            string content = CreateLogEntry(entry);
            string filePath = GetCurrentLogFilePath();
            filePath = RotateFileIfNeeded(filePath);
            
            FileUtil.AppendToFile(filePath, content);
        }
        
        /// <summary>
        /// Crée une chaîne formatée pour l'entrée de log.
        /// </summary>
        /// <param name="entry">L'entrée de log.</param>
        /// <returns>Une chaîne formatée selon le format actuel.</returns>
        private string CreateLogEntry(LogEntry entry)
        {
            return _format switch
            {
                LogFormat.XML => FormatUtil.ToXml(entry),
                _ => FormatUtil.ToJson(entry)
            };
        }
        
        /// <summary>
        /// Génère le chemin du fichier de log pour une date donnée.
        /// </summary>
        /// <param name="date">La date pour laquelle générer le chemin.</param>
        /// <returns>Le chemin complet du fichier de log.</returns>
        private string GetFilePath(DateTime date)
        {
            string dateKey = date.ToString("yyyy-MM-dd");
            string extension = FormatUtil.GetExtension(_format);
            bool formatChanged = _currentLogFilePath != null;
            
            int sequenceNumber = GetNextSequenceNumber(dateKey, formatChanged);
            
            return Path.Combine(_logDirectory, $"{dateKey}.{sequenceNumber}.{extension}");
        }
        
        /// <summary>
        /// Détermine le prochain numéro de séquence pour la date et le format.
        /// </summary>
        /// <param name="dateKey">La clé de date (format yyyy-MM-dd).</param>
        /// <param name="formatChanged">Indique si le format a changé.</param>
        /// <returns>Le prochain numéro de séquence à utiliser.</returns>
        private int GetNextSequenceNumber(string dateKey, bool formatChanged)
        {
            // Si le format a changé, on force un nouveau numéro de séquence
            if (formatChanged)
            {
                // Déterminer le plus grand numéro de séquence actuel
                int maxSequence = 0;
                
                string[] existingFiles = FileUtil.GetDailyLogFiles(_logDirectory, dateKey);
                foreach (string filePath in existingFiles)
                {
                    string fileName = Path.GetFileName(filePath);
                    string[] parts = fileName.Split('.');
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int seqNum))
                    {
                        maxSequence = Math.Max(maxSequence, seqNum);
                    }
                }
                
                return maxSequence + 1;
            }
            
            // Vérifier si on a déjà un numéro pour cette date
            if (!_sequenceNumbers.TryGetValue(dateKey, out int sequence))
            {
                // Pas de numéro existant, commencer à 1
                sequence = 1;
                _sequenceNumbers[dateKey] = sequence;
            }
            
            return sequence;
        }
        
        /// <summary>
        /// Vérifie si le fichier dépasse la taille maximale et effectue une rotation si nécessaire.
        /// </summary>
        /// <param name="filePath">Le chemin du fichier à vérifier.</param>
        /// <returns>Le nouveau chemin de fichier à utiliser.</returns>
        private string RotateFileIfNeeded(string filePath)
        {
            if (File.Exists(filePath) && FileUtil.GetFileSize(filePath) >= MAX_FILE_SIZE_BYTES)
            {
                // Le fichier existe et dépasse la taille max, en créer un nouveau
                string dateKey = DateTime.Now.ToString("yyyy-MM-dd");
                string extension = FormatUtil.GetExtension(_format);
                
                // Incrémenter la séquence
                if (_sequenceNumbers.TryGetValue(dateKey, out int currentSeq))
                {
                    _sequenceNumbers[dateKey] = currentSeq + 1;
                }
                
                // Générer un nouveau nom de fichier
                string newPath = Path.Combine(
                    _logDirectory, 
                    $"{dateKey}.{_sequenceNumbers[dateKey]}.{extension}"
                );
                
                _currentLogFilePath = newPath;
                return newPath;
            }
            
            return filePath;
        }
    }
}
