using System.Diagnostics;
using System.Windows;

namespace EasySave.Models
{
    /// <summary>
    /// Detector class responsible for monitoring the execution state of a specified business software process.
    /// Notifies the BackupManager when the process starts or stops.
    /// </summary>
    internal class SM_Detector
    {
        private CancellationTokenSource _cancellationTokenSource;
        private string _businessSoftwareName;
        private BackupManager _backupManager;
        private Task _monitoringTask;

        // Interval between each process check in milliseconds
        private const int CheckInterval = 500;

        /// <summary>
        /// Indicates whether the business software is currently running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Initializes a new instance of the SM_Detector class.
        /// Loads the business software name from configuration and appends ".exe" if necessary.
        /// </summary>
        /// <param name="backupManager">Reference to the BackupManager for notification.</param>
        public SM_Detector(BackupManager backupManager)
        {
            _backupManager = backupManager;

            // Retrieve the process name from settings
            _businessSoftwareName = ConfigManager.GetInstance().GetSetting("PriorityProcess");

            // Append ".exe" if not present
            if (!string.IsNullOrEmpty(_businessSoftwareName) &&
                !_businessSoftwareName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                _businessSoftwareName += ".exe";
            }
        }

        /// <summary>
        /// Updates the name of the business software to monitor and restarts monitoring if necessary.
        /// </summary>
        /// <param name="newName">New name of the business software process.</param>
        public void UpdateBusinessSoftwareName(string newName)
        {
            bool wasEmpty = string.IsNullOrEmpty(_businessSoftwareName);
            bool willBeEmpty = string.IsNullOrEmpty(newName);

            // If the new name is empty, disable monitoring
            if (willBeEmpty)
            {
                _businessSoftwareName = string.Empty;
                IsRunning = false; // Force state to "not running"
                Console.WriteLine("SM_Detector: Monitoring disabled (no business software specified)");
                return;
            }

            // Update the name and append ".exe" if necessary
            _businessSoftwareName = newName;
            if (!_businessSoftwareName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                _businessSoftwareName += ".exe";

            Console.WriteLine($"SM_Detector: Updated monitored business software name to '{_businessSoftwareName}'");

            // If transitioning from empty to non-empty and the task is not active, restart monitoring
            if (wasEmpty && !willBeEmpty && (_monitoringTask == null || _monitoringTask.IsCompleted))
            {
                Console.WriteLine("SM_Detector: Restarting monitoring after name update");
                // Initial check
                CheckIfBusinessSoftwareIsRunning();

                // Create a new token if necessary
                if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                // Start the monitoring task
                _monitoringTask = Task.Run(MonitorProcessAsync, _cancellationTokenSource.Token);
            }
            else
            {
                // Otherwise, just update the check
                CheckIfBusinessSoftwareIsRunning();
            }
        }

        /// <summary>
        /// Starts monitoring the business software process.
        /// </summary>
        /// <param name="token">Cancellation token to support cooperative cancellation.</param>
        public void StartMonitoring(CancellationToken token)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            // Do not start monitoring if no business software is specified
            if (string.IsNullOrEmpty(_businessSoftwareName))
            {
                Console.WriteLine("SM_Detector: Monitoring not started (no business software specified)");
                return;
            }

            Console.WriteLine($"SM_Detector: Starting monitoring for business software '{_businessSoftwareName}'");

            // Initial check
            CheckIfBusinessSoftwareIsRunning();

            // Always start the background monitoring task
            _monitoringTask = Task.Run(MonitorProcessAsync, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Stops monitoring the business software process gracefully.
        /// </summary>
        public void StopMonitoring()
        {
            try
            {
                _cancellationTokenSource?.Cancel();

                // Wait for the monitoring task to complete (with a reasonable timeout)
                if (_monitoringTask != null && !_monitoringTask.IsCompleted)
                {
                    _monitoringTask.Wait(TimeSpan.FromSeconds(2));
                }

                Console.WriteLine("SM_Detector: Monitoring stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SM_Detector: Error while stopping monitoring - {ex.Message}");
            }
        }

        /// <summary>
        /// Asynchronous monitoring loop for the business software process.
        /// Notifies the BackupManager when the process starts or stops.
        /// </summary>
        private async Task MonitorProcessAsync()
        {
            bool previousState = IsRunning;

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    // If no business software is specified, do nothing
                    if (string.IsNullOrEmpty(_businessSoftwareName))
                    {
                        await Task.Delay(CheckInterval, _cancellationTokenSource.Token);
                        continue;
                    }

                    // Check if the process is running
                    CheckIfBusinessSoftwareIsRunning();

                    // If the state has changed, notify the BackupManager
                    if (IsRunning != previousState)
                    {
                        if (IsRunning)
                        {
                            Console.WriteLine($"SM_Detector: Process '{_businessSoftwareName}' started");
                            // Notify BackupManager that the process has started
                            _backupManager.SM_Detected();
                            System.Windows.MessageBox.Show(
                            "Business software detected",
                            "Business software start",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        }
                        else
                        {
                            Console.WriteLine($"SM_Detector: Process '{_businessSoftwareName}' stopped");
                            _backupManager.SM_Undetected();
                            System.Windows.MessageBox.Show(
                            "Business software stopped",
                            "Business software stop",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        }

                        previousState = IsRunning;
                    }

                    // Wait before the next check (supports cancellation)
                    await Task.Delay(CheckInterval, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // Monitoring cancelled, exit the loop
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SM_Detector: Error during monitoring - {ex.Message}");
                    // Wait a bit before retrying
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }
        }

        /// <summary>
        /// Checks if the business software process is currently running.
        /// Updates the IsRunning property accordingly.
        /// </summary>
        private void CheckIfBusinessSoftwareIsRunning()
        {
            try
            {
                // If no business software is specified, consider it not running
                if (string.IsNullOrEmpty(_businessSoftwareName))
                {
                    IsRunning = false;
                    return;
                }

                // Extract the process name without the ".exe" extension
                string processName = _businessSoftwareName;
                if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    processName = processName.Substring(0, processName.Length - 4);

                // Check if the process is running
                Process[] processes = Process.GetProcessesByName(processName);
                IsRunning = processes.Length > 0;

                // Release resources
                foreach (var process in processes)
                {
                    process.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SM_Detector: Error while checking process - {ex.Message}");
            }
        }
    }
}