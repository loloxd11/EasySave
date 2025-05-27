using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace EasySave.Models
{
    /// <summary>
    /// Singleton pour coordonner les transferts de fichiers entre jobs (priorisation et limitation de taille)
    /// </summary>
    public class TransferCoordinator
    {
        private static readonly object _instanceLock = new object();
        private static TransferCoordinator _instance;
        private readonly object _lock = new object();

        private HashSet<string> _priorityExtensions;
        private int _maxParallelSizeKB;
        private readonly HashSet<string> _transferringLargeFiles = new HashSet<string>();
        private readonly HashSet<string> _transferringPriorityFiles = new HashSet<string>();
        private readonly HashSet<string> _pendingPriorityFiles = new HashSet<string>();

        private TransferCoordinator()
        {
            LoadSettings();
        }

        public static TransferCoordinator Instance
        {
            get
            {
                lock (_instanceLock)
                {
                    return _instance ??= new TransferCoordinator();
                }
            }
        }

        /// <summary>
        /// Recharge les paramètres depuis la configuration
        /// </summary>
        public void LoadSettings()
        {
            var config = ConfigManager.GetInstance();
            string extStr = config.GetSetting("PriorityExtensions") ?? string.Empty;
            _priorityExtensions = new HashSet<string>(
                extStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim().ToLowerInvariant())
            );
            if (int.TryParse(config.GetSetting("MaxParallelSizeKB"), out int n))
                _maxParallelSizeKB = n;
            else
                _maxParallelSizeKB = 1024; // défaut 1 Mo
        }

        /// <summary>
        /// Doit être appelé par chaque job pour signaler les fichiers prioritaires en attente
        /// </summary>
        public void RegisterPendingPriorityFile(string filePath)
        {
            lock (_lock)
            {
                _pendingPriorityFiles.Add(filePath);
            }
        }

        /// <summary>
        /// Doit être appelé quand un fichier prioritaire n'est plus en attente (transféré ou ignoré)
        /// </summary>
        public void UnregisterPendingPriorityFile(string filePath)
        {
            lock (_lock)
            {
                _pendingPriorityFiles.Remove(filePath);
            }
        }

        /// <summary>
        /// Demande l'autorisation de transférer un fichier (bloque si non autorisé)
        /// </summary>
        public void RequestTransfer(string filePath, long fileSize)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            bool isPriority = _priorityExtensions.Contains(ext);
            bool isLarge = fileSize > _maxParallelSizeKB * 1024;

            lock (_lock)
            {
                if (isPriority)
                    _pendingPriorityFiles.Remove(filePath);

                while (true)
                {
                    // Règle 1 : priorisation (ne bloque que si la liste n'est pas vide et qu'il y a des fichiers prioritaires en attente)
                    if (_priorityExtensions.Count > 0 && _pendingPriorityFiles.Count > 0 && !isPriority)
                    {
                        Monitor.Wait(_lock);
                        continue;
                    }
                    // Règle 2 : limitation taille
                    if (isLarge && _transferringLargeFiles.Count > 0)
                    {
                        Monitor.Wait(_lock);
                        continue;
                    }
                    // Si tout est ok, on enregistre le transfert
                    if (isPriority)
                        _transferringPriorityFiles.Add(filePath);
                    if (isLarge)
                        _transferringLargeFiles.Add(filePath);
                    break;
                }
            }
        }

        /// <summary>
        /// Doit être appelé après chaque transfert pour libérer la ressource
        /// </summary>
        public void ReleaseTransfer(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            bool isPriority = _priorityExtensions.Contains(ext);
            bool isLarge = false;
            lock (_lock)
            {
                if (isPriority)
                    _transferringPriorityFiles.Remove(filePath);
                // On ne connaît pas la taille ici, donc on retire si présent
                _transferringLargeFiles.Remove(filePath);
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        /// Permet de savoir si une extension est prioritaire
        /// </summary>
        public bool IsPriorityExtension(string ext)
        {
            return _priorityExtensions.Contains(ext.ToLowerInvariant());
        }
    }
}
