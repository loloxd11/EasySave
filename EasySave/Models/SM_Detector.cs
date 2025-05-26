using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Models
{
    internal class SM_Detector
    {
        private CancellationTokenSource _cancellationTokenSource;
        private string _businessSoftwareName;
        private BackupManager _backupManager;
        private Task _monitoringTask;

        // Période entre chaque vérification en millisecondes
        private const int CheckInterval = 500;

        public bool IsRunning { get; private set; }

        public SM_Detector(BackupManager backupManager)
        {
            _backupManager = backupManager;

            // Récupération du nom du processus depuis les paramètres
            _businessSoftwareName = ConfigManager.GetInstance().GetSetting("PriorityProcess");
            if (string.IsNullOrEmpty(_businessSoftwareName))
                _businessSoftwareName = "businessApp.exe"; // Valeur par défaut

            // Ajoute l'extension .exe si absente
            if (!_businessSoftwareName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                _businessSoftwareName += ".exe";
        }

        /// <summary>
        /// Démarre la surveillance du logiciel métier
        /// </summary>
        public void StartMonitoring(CancellationToken token)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            Console.WriteLine($"SM_Detector: Démarrage de la surveillance du logiciel métier '{_businessSoftwareName}'");

            // Vérification initiale
            CheckIfBusinessSoftwareIsRunning();

            // Notification immédiate si le logiciel est déjà lancé
            if (IsRunning)

            // Démarrer la tâche de surveillance en arrière-plan
            _monitoringTask = Task.Run(MonitorProcessAsync, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Arrête proprement la surveillance
        /// </summary>
        public void StopMonitoring()
        {
            try
            {
                _cancellationTokenSource?.Cancel();

                // Attendre que la tâche de surveillance se termine (avec un délai raisonnable)
                if (_monitoringTask != null && !_monitoringTask.IsCompleted)
                {
                    _monitoringTask.Wait(TimeSpan.FromSeconds(2));
                }

                Console.WriteLine("SM_Detector: Surveillance arrêtée.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SM_Detector: Erreur lors de l'arrêt de la surveillance - {ex.Message}");
            }
        }

        /// <summary>
        /// Boucle de surveillance asynchrone du processus métier
        /// </summary>
        private async Task MonitorProcessAsync()
        {
            bool previousState = IsRunning;

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    // Vérifier si le processus est en cours d'exécution
                    CheckIfBusinessSoftwareIsRunning();

                    // Si l'état a changé, notifier le BackupManager
                    if (IsRunning != previousState)
                    {
                        if (IsRunning)
                        {
                            Console.WriteLine($"SM_Detector: Processus '{_businessSoftwareName}' démarré");
                            // Notifier le BackupManager que le processus est lancé
                            _backupManager.SM_Detected();
                        }
                        else
                        {
                            Console.WriteLine($"SM_Detector: Processus '{_businessSoftwareName}' arrêté");
                            _backupManager.SM_Undetected();
                        }

                        previousState = IsRunning;
                    }

                    // Attendre avant la prochaine vérification
                    // Utilisation de Task.Delay pour une attente asynchrone qui respecte l'annulation
                    await Task.Delay(CheckInterval, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // Surveillance annulée, sortir de la boucle
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SM_Detector: Erreur pendant la surveillance - {ex.Message}");
                    // Attendre un peu avant de réessayer
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }
        }

        /// <summary>
        /// Vérifie si le processus métier est en cours d'exécution
        /// </summary>
        private void CheckIfBusinessSoftwareIsRunning()
        {
            try
            {
                // Extraire le nom du processus sans l'extension .exe
                string processName = _businessSoftwareName;
                if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    processName = processName.Substring(0, processName.Length - 4);

                // Vérifier si le processus est en cours d'exécution
                Process[] processes = Process.GetProcessesByName(processName);
                IsRunning = processes.Length > 0;

                // Libérer les ressources
                foreach (var process in processes)
                {
                    process.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SM_Detector: Erreur lors de la vérification du processus - {ex.Message}");
            }
        }
    }
}