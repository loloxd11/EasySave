using System;
using System.IO;
using LogLibrary;
using System.Reflection;
using System.Text.Json;

namespace EasySave
{
    public class LogManager : IObserver
    {
        private readonly ILogService logService;
        private readonly string logDirectory;

        public LogManager(string directory)
        {
            logDirectory = directory;

            // Créer le répertoire de logs s'il n'existe pas
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Obtenir l'instance de ILogService à partir de la DLL
            logService = GetLogServiceInstance(logDirectory);
        }

        private ILogService GetLogServiceInstance(string directory)
        {
            try
            {
                // Essayer de charger l'assembly LogLibrary
                Assembly logLibrary = Assembly.Load("LogLibrary");

                // Chercher le type LogServiceFactory
                Type factoryType = logLibrary.GetType("LogLibrary.LogServiceFactory");
                if (factoryType != null)
                {
                    // Chercher la méthode CreateLogService qui prend un string en paramètre
                    MethodInfo createMethod = factoryType.GetMethod("CreateLogService", new[] { typeof(string) });
                    if (createMethod != null)
                    {
                        // Appeler la méthode statique
                        return (ILogService)createMethod.Invoke(null, new object[] { directory });
                    }
                }

                // Deuxième approche : essayer de trouver l'implémentation directe de ILogService
                Type logManagerType = logLibrary.GetType("LogLibrary.LogManager");
                if (logManagerType != null)
                {
                    // Chercher la méthode GetInstance
                    MethodInfo getInstance = logManagerType.GetMethod("GetInstance", new[] { typeof(string) });
                    if (getInstance != null)
                    {
                        // Appeler la méthode statique
                        return (ILogService)getInstance.Invoke(null, new object[] { directory });
                    }

                    // Essayer de créer une instance avec le constructeur
                    var constructor = logManagerType.GetConstructor(new[] { typeof(string) });
                    if (constructor != null)
                    {
                        return (ILogService)constructor.Invoke(new object[] { directory });
                    }
                }

                // Si tout échoue, utiliser une implémentation de secours
                return new FallbackLogService(directory);
            }
            catch (Exception)
            {
                return new FallbackLogService(directory);
            }
        }

        public void Update(BackupJob job, string action)
        {
            if (action == "file")
            {
                bool isServiceReady = true;

                // Vérifier si la méthode IsLogServiceReady existe et l'appeler
                try
                {
                    isServiceReady = logService.IsLogServiceReady();
                }
                catch
                {
                    // En cas d'erreur, supposer que le service est prêt
                    isServiceReady = true;
                }

                if (isServiceReady)
                {
                    // Vérifier que les chemins de fichiers sont valides
                    if (!string.IsNullOrEmpty(job.CurrentSourceFile) && !string.IsNullOrEmpty(job.CurrentTargetFile))
                    {
                        try
                        {
                            // Calculer la taille du fichier
                            long fileSize = 0;
                            if (File.Exists(job.CurrentSourceFile))
                            {
                                fileSize = new FileInfo(job.CurrentSourceFile).Length;
                            }

                            // Utiliser la nouvelle méthode SerializeLogEntry pour obtenir la chaîne JSON
                            DateTime timestamp = DateTime.Now;
                            string jsonEntry = logService.SerializeLogEntry(
                                job.Name,
                                job.CurrentSourceFile,
                                job.CurrentTargetFile,
                                fileSize,
                                job.LastFileTime,
                                timestamp);

                            // Écrire l'entrée JSON dans le fichier de log
                            string logFilePath = logService.GetDailyLogFilePath(timestamp);
                            File.AppendAllText(logFilePath, jsonEntry + Environment.NewLine);

                            // Aussi appeler LogFileTransfer pour la compatibilité
                            logService.LogFileTransfer(
                                job.Name,
                                job.CurrentSourceFile,
                                job.CurrentTargetFile,
                                fileSize,
                                job.LastFileTime);
                        }
                        catch (Exception)
                        {
                            // Ne pas afficher l'erreur dans la console
                        }
                    }
                }
            }
        }
    }

    // Implémentation de secours pour ILogService en cas de problème avec la DLL
    internal class FallbackLogService : ILogService
    {
        private readonly string logDirectory;
        private readonly JsonSerializerOptions jsonOptions;

        public FallbackLogService(string directory)
        {
            logDirectory = directory;

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Configurer les options de sérialisation JSON
            jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false // Pas de mise en forme pour avoir une entrée par ligne
            };
        }

        public string GetDailyLogFilePath(DateTime date)
        {
            string fileName = $"{date:yyyy-MM-dd}.json";
            return Path.Combine(logDirectory, fileName);
        }

        public void LogFileTransfer(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime)
        {
            try
            {
                string logFilePath = GetDailyLogFilePath(DateTime.Now);

                // Utiliser SerializeLogEntry pour obtenir l'entrée JSON
                string jsonEntry = SerializeLogEntry(jobName, sourcePath, targetPath, fileSize, transferTime, DateTime.Now);

                // Ajouter une nouvelle ligne à la fin du fichier
                File.AppendAllText(logFilePath, jsonEntry + Environment.NewLine);
            }
            catch (Exception)
            {
                // Ne pas afficher l'erreur dans la console
            }
        }

        public bool IsLogServiceReady()
        {
            return true;
        }

        public string SerializeLogEntry(string jobName, string sourcePath, string targetPath, long fileSize, long transferTime, DateTime timestamp)
        {
            // Créer l'objet de log avec le format exact demandé
            var logEntry = new
            {
                Timestamp = timestamp.ToString("yyyy-MM-ddTHH:mm:ss"),
                JobName = jobName,
                Source = sourcePath,
                Target = targetPath,
                FileSize = fileSize,
                TransferTimeMs = transferTime
            };

            // Sérialiser en JSON
            return JsonSerializer.Serialize(logEntry, jsonOptions);
        }
    }
}
