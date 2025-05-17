using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasySave.Models;
using LogLibrary.Factories;
using LogLibrary.Enums;
using LogLibrary.Interfaces;

namespace EasySave.Models
{
    /// <summary>
    /// Gère les opérations de journalisation pour l'application EasySave.
    /// Implémente le pattern Singleton pour garantir une instance unique.
    /// </summary>
    public class LogManager : ILogger, IObserver
    {
        // Instance Singleton de LogManager
        private static LogManager instance;
        private static readonly object lockObject = new object();

        // Instance de logger externe de la LogLibrary
        private readonly LogLibrary.Interfaces.ILogger _logger;

        // Chemin du répertoire de logs par défaut
        private static string defaultLogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "Logs");

        /// <summary>
        /// Constructeur privé pour empêcher l'instanciation directe.
        /// Initialise le logger avec le répertoire et le format spécifiés.
        /// </summary>
        /// <param name="logDirectory">Répertoire de stockage des logs</param>
        /// <param name="format">Format des logs (XML ou JSON)</param>
        private LogManager(string logDirectory, LogFormat format = LogFormat.XML)
        {
            _logger = LoggerFactory.Create(logDirectory, (LogLibrary.Enums.LogFormat)format);
        }

        /// <summary>
        /// Récupère l'instance singleton du LogManager.
        /// Assure la sécurité des threads et initialise le logger si ce n'est pas déjà fait.
        /// </summary>
        /// <param name="logDirectory">Répertoire personnalisé des logs (optionnel)</param>
        /// <param name="format">Format des logs (XML par défaut)</param>
        /// <returns>L'instance singleton de LogManager</returns>
        public static LogManager GetInstance(string logDirectory = null, LogFormat format = LogFormat.XML)
        {
            if (instance == null)
            {
                lock (lockObject) // Assure la sécurité des threads
                {
                    if (instance == null)
                    {
                        // Utilise le répertoire par défaut si aucun n'est spécifié
                        string directory = logDirectory ?? defaultLogDirectory;

                        // S'assure que le répertoire existe
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // Vérifie le format de log dans la configuration
                        var configManager = ConfigManager.GetInstance();
                        string formatSetting = configManager.GetSetting("LogFormat");

                        if (!string.IsNullOrEmpty(formatSetting))
                        {
                            if (formatSetting.Equals("XML", StringComparison.OrdinalIgnoreCase))
                            {
                                format = LogFormat.XML;
                            }
                            else if (formatSetting.Equals("JSON", StringComparison.OrdinalIgnoreCase))
                            {
                                format = LogFormat.JSON;
                            }
                        }

                        instance = new LogManager(directory, format);
                    }
                }
            }
            return instance;
        }

        /// <summary>
        /// Journalise un transfert de fichier.
        /// </summary>
        /// <param name="jobName">Nom du job de sauvegarde</param>
        /// <param name="sourcePath">Chemin du fichier source</param>
        /// <param name="targetPath">Chemin du fichier cible</param>
        /// <param name="fileSize">Taille du fichier en octets</param>
        /// <param name="transferTime">Temps de transfert en millisecondes</param>
        /// <param name="encryptionTime">Temps de chiffrement en millisecondes (0 par défaut)</param>
        public void LogFileTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, long encryptionTime)
        {
            _logger.LogTransfer(jobName, sourcePath, targetPath, fileSize, transferTime, encryptionTime);
        }

        /// <summary>
        /// Journalise un événement spécifique lié au processus de sauvegarde.
        /// </summary>
        /// <param name="eventName">Nom de l'événement à journaliser</param>
        public void LogEvent(string eventName)
        {
            _logger.LogEvent(eventName);
        }

        /// <summary>
        /// Récupère le format de log utilisé actuellement.
        /// </summary>
        /// <returns>Le format de log actuel</returns>
        public LogFormat GetCurrentFormat()
        {
            return (LogFormat)_logger.GetCurrentFormat();
        }

        /// <summary>
        /// Récupère le chemin du fichier de log actuel.
        /// </summary>
        /// <returns>Le chemin du fichier de log actuel</returns>
        public string GetCurrentLogFilePath()
        {
            return _logger.GetCurrentLogFilePath();
        }

        /// <summary>
        /// Vérifie si le logger est prêt à journaliser des événements ou des transferts.
        /// </summary>
        /// <returns>True si le logger est prêt, sinon false</returns>
        public bool IsReady()
        {
            return _logger.IsReady();
        }

        /// <summary>
        /// Met à jour l'observateur avec l'état actuel du BackupJob et l'action effectuée.
        /// </summary>
        /// <param name="action">Action effectuée sur le job</param>
        /// <param name="name">Nom du job</param>
        /// <param name="type">Type de sauvegarde</param>
        /// <param name="state">État du job</param>
        /// <param name="sourcePath">Chemin source</param>
        /// <param name="targetPath">Chemin cible</param>
        /// <param name="totalFiles">Nombre total de fichiers</param>
        /// <param name="totalSize">Taille totale</param>
        /// <param name="progression">Progression actuelle</param>
        public void Update(string action, string name, BackupType type, JobState state,
            string sourcePath, string targetPath, int totalFiles, long totalSize, int progression)
        {
            switch (action)
            {
                case "start":
                    // Journalisation du démarrage d'un travail de sauvegarde
                    LogEvent("JobStarted");
                    break;

                case "finish":
                    // Journalisation de la fin d'un travail de sauvegarde
                    LogEvent("JobCompleted");
                    break;

                case "error":
                    // Journalisation d'une erreur dans un travail de sauvegarde
                    LogEvent("JobError");
                    break;

                case "processing":
                    // Journalisation du traitement d'un fichier si nécessaire
                    if (sourcePath != null && targetPath != null)
                    {
                        long fileSize = GetFileSize(sourcePath);
                        LogFileTransfer(
                            name,
                            sourcePath,
                            targetPath,
                            fileSize,
                            0, // Temps de transfert sera mis à jour à la fin
                            0  // Pas de chiffrement pour l'instant
                        );
                    }
                    break;

                case "delete":
                    // Journalisation de la suppression d'un fichier
                    LogFileTransfer(
                        name,
                        "", // Pas de fichier source pour une suppression
                        targetPath,
                        0, // Pas de taille pour une suppression
                        0, // Pas de temps de transfert pour une suppression
                        0  // Pas de chiffrement pour une suppression
                    );
                    break;

                case "delete_dir":
                    // Journalisation de la suppression d'un répertoire
                    LogEvent("DirectoryDeleted");
                    break;

                case "clean_complete":
                    // Journalisation de la fin du nettoyage d'un répertoire
                    LogEvent("TargetDirectoryCleaned");
                    break;
            }
        }

        /// <summary>
        /// Méthode utilitaire pour obtenir la taille d'un fichier.
        /// </summary>
        /// <param name="filePath">Chemin du fichier</param>
        /// <returns>Taille du fichier en octets, ou 0 en cas d'erreur</returns>
        private long GetFileSize(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                return fileInfo.Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
