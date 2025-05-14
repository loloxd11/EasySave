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
    /// Impl�mentation concr�te du service de journalisation.
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
        /// Initialise une nouvelle instance avec le r�pertoire sp�cifi�.
        /// </summary>
        /// <param name="directory">Le r�pertoire des fichiers de log.</param>
        public LoggerManager(string directory)
            : this(directory, LogFormat.JSON)
        {
        }
        
        /// <summary>
        /// Initialise une nouvelle instance avec le r�pertoire et le format sp�cifi�s.
        /// </summary>
        /// <param name="directory">Le r�pertoire des fichiers de log.</param>
        /// <param name="format">Le format des logs.</param>
        public LoggerManager(string directory, LogFormat format)
        {
            _logDirectory = directory;
            _format = format;
            
            // Assurer que le r�pertoire existe
            FileUtil.EnsureDirectoryExists(_logDirectory);
        }
        
        /// <summary>
        /// R�cup�re le format actuel des logs.
        /// </summary>
        /// <returns>Le format actuel.</returns>
        public LogFormat GetCurrentFormat()
        {
            return _format;
        }
        
        /// <summary>
        /// D�finit le format des logs.
        /// </summary>
        /// <param name="format">Le nouveau format � utiliser.</param>
        public void SetFormat(LogFormat format)
        {
            if (_format == format)
                return;
                
            _format = format;
            _currentLogFilePath = null; // Force la cr�ation d'un nouveau fichier
        }
        
        /// <summary>
        /// R�cup�re le chemin du fichier de log actuel.
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
        /// V�rifie si le service de log est pr�t.
        /// </summary>
        /// <returns>True si le service est pr�t � �tre utilis�.</returns>
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
        /// Journalise un �v�nement avec des propri�t�s personnalis�es.
        /// </summary>
        /// <param name="eventName">Le nom de l'�v�nement.</param>
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
        /// Cr�e une cha�ne format�e pour l'entr�e de log.
        /// </summary>
        /// <param name="entry">L'entr�e de log.</param>
        /// <returns>Une cha�ne format�e selon le format actuel.</returns>
        private string CreateLogEntry(LogEntry entry)
        {
            return _format switch
            {
                LogFormat.XML => FormatUtil.ToXml(entry),
                _ => FormatUtil.ToJson(entry)
            };
        }
        
        /// <summary>
        /// G�n�re le chemin du fichier de log pour une date donn�e.
        /// </summary>
        /// <param name="date">La date pour laquelle g�n�rer le chemin.</param>
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
        /// D�termine le prochain num�ro de s�quence pour la date et le format.
        /// </summary>
        /// <param name="dateKey">La cl� de date (format yyyy-MM-dd).</param>
        /// <param name="formatChanged">Indique si le format a chang�.</param>
        /// <returns>Le prochain num�ro de s�quence � utiliser.</returns>
        private int GetNextSequenceNumber(string dateKey, bool formatChanged)
        {
            // Si le format a chang�, on force un nouveau num�ro de s�quence
            if (formatChanged)
            {
                // D�terminer le plus grand num�ro de s�quence actuel
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
            
            // V�rifier si on a d�j� un num�ro pour cette date
            if (!_sequenceNumbers.TryGetValue(dateKey, out int sequence))
            {
                // Pas de num�ro existant, commencer � 1
                sequence = 1;
                _sequenceNumbers[dateKey] = sequence;
            }
            
            return sequence;
        }
        
        /// <summary>
        /// V�rifie si le fichier d�passe la taille maximale et effectue une rotation si n�cessaire.
        /// </summary>
        /// <param name="filePath">Le chemin du fichier � v�rifier.</param>
        /// <returns>Le nouveau chemin de fichier � utiliser.</returns>
        private string RotateFileIfNeeded(string filePath)
        {
            if (File.Exists(filePath) && FileUtil.GetFileSize(filePath) >= MAX_FILE_SIZE_BYTES)
            {
                // Le fichier existe et d�passe la taille max, en cr�er un nouveau
                string dateKey = DateTime.Now.ToString("yyyy-MM-dd");
                string extension = FormatUtil.GetExtension(_format);
                
                // Incr�menter la s�quence
                if (_sequenceNumbers.TryGetValue(dateKey, out int currentSeq))
                {
                    _sequenceNumbers[dateKey] = currentSeq + 1;
                }
                
                // G�n�rer un nouveau nom de fichier
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
