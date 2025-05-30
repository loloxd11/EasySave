using System.IO;

namespace EasySave.Models
{
    /// <summary>
    /// Singleton to coordinate file transfers between jobs (priority and size limitation).
    /// </summary>
    public class TransferCoordinator
    {
        // Lock object for singleton instance creation
        private static readonly object _instanceLock = new object();
        private static TransferCoordinator _instance;
        // Lock object for synchronizing access to transfer state
        private readonly object _lock = new object();

        // Set of file extensions considered as priority
        private HashSet<string> _priorityExtensions;
        // Maximum size (in KB) allowed for parallel transfers
        private int _maxParallelSizeKB;
        // Set of currently transferring large files
        private readonly HashSet<string> _transferringLargeFiles = new HashSet<string>();
        // Set of currently transferring priority files
        private readonly HashSet<string> _transferringPriorityFiles = new HashSet<string>();
        // Set of pending priority files waiting to be transferred
        private readonly HashSet<string> _pendingPriorityFiles = new HashSet<string>();

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// Loads settings from configuration.
        /// </summary>
        private TransferCoordinator()
        {
            LoadSettings();
        }

        /// <summary>
        /// Gets the singleton instance of the TransferCoordinator.
        /// </summary>
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
        /// Reloads settings from the configuration.
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
                _maxParallelSizeKB = 1024; // Default 1 MB
        }

        /// <summary>
        /// Should be called by each job to register pending priority files.
        /// </summary>
        /// <param name="filePath">The path of the priority file to register as pending.</param>
        public void RegisterPendingPriorityFile(string filePath)
        {
            lock (_lock)
            {
                _pendingPriorityFiles.Add(filePath);
            }
        }

        /// <summary>
        /// Should be called when a priority file is no longer pending (transferred or ignored).
        /// </summary>
        /// <param name="filePath">The path of the priority file to unregister.</param>
        public void UnregisterPendingPriorityFile(string filePath)
        {
            lock (_lock)
            {
                _pendingPriorityFiles.Remove(filePath);
            }
        }

        /// <summary>
        /// Requests authorization to transfer a file (blocks if not authorized).
        /// </summary>
        /// <param name="filePath">The path of the file to transfer.</param>
        /// <param name="fileSize">The size of the file in bytes.</param>
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
                    // Rule 1: Prioritization (block if there are pending priority files and this file is not priority)
                    if (_priorityExtensions.Count > 0 && _pendingPriorityFiles.Count > 0 && !isPriority)
                    {
                        Monitor.Wait(_lock);
                        continue;
                    }
                    // Rule 2: Size limitation (block if a large file is already being transferred)
                    if (isLarge && _transferringLargeFiles.Count > 0)
                    {
                        Monitor.Wait(_lock);
                        continue;
                    }
                    // If all checks pass, register the transfer
                    if (isPriority)
                        _transferringPriorityFiles.Add(filePath);
                    if (isLarge)
                        _transferringLargeFiles.Add(filePath);
                    break;
                }
            }
        }

        /// <summary>
        /// Should be called after each transfer to release the resource.
        /// </summary>
        /// <param name="filePath">The path of the file whose transfer is complete.</param>
        public void ReleaseTransfer(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            bool isPriority = _priorityExtensions.Contains(ext);
            // We do not know the size here, so just remove if present
            lock (_lock)
            {
                if (isPriority)
                    _transferringPriorityFiles.Remove(filePath);
                _transferringLargeFiles.Remove(filePath);
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        /// Checks if an extension is considered priority.
        /// </summary>
        /// <param name="ext">The file extension to check.</param>
        /// <returns>True if the extension is priority, false otherwise.</returns>
        public bool IsPriorityExtension(string ext)
        {
            return _priorityExtensions.Contains(ext.ToLowerInvariant());
        }
    }
}
