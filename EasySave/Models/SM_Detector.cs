using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

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

            // Si le nom est renseigné, ajouter l'extension .exe si nécessaire
            if (!string.IsNullOrEmpty(_businessSoftwareName) &&
                !_businessSoftwareName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                _businessSoftwareName += ".exe";
            }
        }

        /// <summary>
        /// Met à jour le nom du logiciel métier à surveiller et redémarre la surveillance si nécessaire
        /// </summary>
        /// <param name="newName">Nouveau nom du logiciel métier</param>
        public void UpdateBusinessSoftwareName(string newName)
        {
            bool wasEmpty = string.IsNullOrEmpty(_businessSoftwareName);
            bool willBeEmpty = string.IsNullOrEmpty(newName);

            // Si le nouveau nom est vide, désactive la surveillance
            if (willBeEmpty)
            {
                _businessSoftwareName = string.Empty;
                IsRunning = false; // Force l'état à "non en cours d'exécution"
                Console.WriteLine("SM_Detector: Surveillance désactivée (aucun logiciel métier spécifié)");
                return;
            }

            // Sinon, mettre à jour le nom avec l'extension .exe si nécessaire
            _businessSoftwareName = newName;
            if (!_businessSoftwareName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                _businessSoftwareName += ".exe";

            Console.WriteLine($"SM_Detector: Mise à jour du nom du logiciel métier surveillé '{_businessSoftwareName}'");

            // Si on passe d'un état vide à un état non-vide et que la tâche n'est pas active,
            // il faut redémarrer la surveillance
            if (wasEmpty && !willBeEmpty && (_monitoringTask == null || _monitoringTask.IsCompleted))
            {
                Console.WriteLine("SM_Detector: Redémarrage de la surveillance après mise à jour du nom");
                // Vérification initiale
                CheckIfBusinessSoftwareIsRunning();

                // Créer un nouveau token si nécessaire
                if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                // Démarrer la tâche de surveillance
                _monitoringTask = Task.Run(MonitorProcessAsync, _cancellationTokenSource.Token);
            }
            else
            {
                // Sinon, simplement mettre à jour la vérification
                CheckIfBusinessSoftwareIsRunning();
            }
        }
        /// <summary>
        /// Démarre la surveillance du logiciel métier
        /// </summary>
        public void StartMonitoring(CancellationToken token)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            // Si aucun logiciel métier n'est spécifié, ne pas démarrer la surveillance active
            if (string.IsNullOrEmpty(_businessSoftwareName))
            {
                Console.WriteLine("SM_Detector: Surveillance non démarrée (aucun logiciel métier spécifié)");
                return;
            }

            Console.WriteLine($"SM_Detector: Démarrage de la surveillance du logiciel métier '{_businessSoftwareName}'");

            // Vérification initiale
            CheckIfBusinessSoftwareIsRunning();

            // Toujours démarrer la tâche de surveillance en arrière-plan
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
                    // Si aucun logiciel métier n'est spécifié, ne rien faire
                    if (string.IsNullOrEmpty(_businessSoftwareName))
                    {
                        await Task.Delay(CheckInterval, _cancellationTokenSource.Token);
                        continue;
                    }

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
                            System.Windows.MessageBox.Show(
                            "logiciel métier détecté",
                            "Logiciel metier start",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        }
                        else
                        {
                            Console.WriteLine($"SM_Detector: Processus '{_businessSoftwareName}' arrêté");
                            _backupManager.SM_Undetected();
                            System.Windows.MessageBox.Show(
                            "logiciel métier terminé",
                            "Logiciel metier stop",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
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
                // Si aucun logiciel métier n'est spécifié, considérer qu'il n'est pas en cours d'exécution
                if (string.IsNullOrEmpty(_businessSoftwareName))
                {
                    IsRunning = false;
                    return;
                }

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