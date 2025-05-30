using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace EasySave.Models
{
    /// <summary>
    /// Server for the remote console, allowing monitoring and launching backup jobs remotely.
    /// </summary>
    public class RemoteConsoleServer : IObserver
    {
        private readonly int _port;
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private readonly BackupManager _backupManager;
        private readonly List<StreamWriter> _clients = new();
        private readonly object _clientsLock = new();

        /// <summary>
        /// Event triggered when the server status changes (started/stopped).
        /// </summary>
        public event Action<bool> ServerStatusChanged;

        /// <summary>
        /// Initializes a new instance of the RemoteConsoleServer class.
        /// </summary>
        /// <param name="port">Port to listen for remote console connections.</param>
        public RemoteConsoleServer(int port)
        {
            _port = port;
            _backupManager = BackupManager.GetInstance();
        }

        /// <summary>
        /// Starts the remote console server and begins accepting client connections.
        /// </summary>
        public void Start()
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _backupManager.AttachObserver(this);
            Task.Run(() => AcceptLoop(_cts.Token));
            ServerStatusChanged?.Invoke(true);
        }

        /// <summary>
        /// Stops the remote console server and disconnects all clients.
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            lock (_clientsLock)
            {
                foreach (var writer in _clients)
                {
                    try { writer.Dispose(); } catch { }
                }
                _clients.Clear();
            }
            _backupManager.DetachObserver(this);
            ServerStatusChanged?.Invoke(false);
        }

        /// <summary>
        /// Asynchronous loop to accept incoming TCP client connections.
        /// </summary>
        /// <param name="token">Cancellation token to stop the loop.</param>
        private async Task AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(token);
                    _ = Task.Run(() => HandleClient(client, token));
                }
                catch (Exception)
                {
                    if (token.IsCancellationRequested) break;
                }
            }
        }

        /// <summary>
        /// Handles communication with a connected client, processing commands and sending responses.
        /// </summary>
        /// <param name="client">The connected TCP client.</param>
        /// <param name="token">Cancellation token for the client session.</param>
        private async Task HandleClient(TcpClient client, CancellationToken token)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream) { AutoFlush = true })
            {
                lock (_clientsLock)
                {
                    _clients.Add(writer);
                }
                // Immediately send the current job statuses to the new client
                var statuses = _backupManager.GetJobStatuses();
                string json = JsonSerializer.Serialize(statuses);
                await writer.WriteLineAsync(json);
                try
                {
                    while (!token.IsCancellationRequested && client.Connected)
                    {
                        string line = await reader.ReadLineAsync();
                        if (line == null) break;
                        if (line.StartsWith("LIST", StringComparison.OrdinalIgnoreCase))
                        {
                            statuses = _backupManager.GetJobStatuses();
                            json = JsonSerializer.Serialize(statuses);
                            await writer.WriteLineAsync(json);
                        }
                        else if (line.StartsWith("START ", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(line.Substring(6), out int idx))
                            {
                                // Respond immediately before waiting for backup completion
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = true, Message = "Job launched" }));
                                // Launch the backup in the background
                                _ = _backupManager.ExecuteJobsAsync(new List<int> { idx });
                            }
                            else
                            {
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = false, Message = "Invalid index" }));
                            }
                        }
                        else if (line.StartsWith("PAUSEALL", StringComparison.OrdinalIgnoreCase))
                        {
                            _backupManager.PauseBackupJobs(reason: "Remote pause all");
                            await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = true, Message = "All jobs paused" }));
                        }
                        else if (line.StartsWith("RESUMEALL", StringComparison.OrdinalIgnoreCase))
                        {
                            _backupManager.ResumeBackupJobs();
                            await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = true, Message = "All jobs resumed" }));
                        }
                        else if (line.StartsWith("PAUSE ", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(line.Substring(6), out int idx))
                            {
                                _backupManager.PauseBackupJobs(new[] { idx }, "Remote pause");
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = true, Message = $"Job {idx} paused" }));
                            }
                            else
                            {
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = false, Message = "Invalid index" }));
                            }
                        }
                        else if (line.StartsWith("RESUME ", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(line.Substring(7), out int idx))
                            {
                                _backupManager.ResumeBackupJobs(new[] { idx });
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = true, Message = $"Job {idx} resumed" }));
                            }
                            else
                            {
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = false, Message = "Invalid index" }));
                            }
                        }
                        else if (line.StartsWith("STOP ", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(line.Substring(5), out int idx))
                            {
                                bool killed = _backupManager.KillBackupJob(idx);
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = killed, Message = killed ? $"Job {idx} stopped" : "Unable to stop job" }));
                            }
                            else
                            {
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = false, Message = "Invalid index" }));
                            }
                        }
                        else
                        {
                            await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = false, Message = "Unknown command" }));
                        }
                    }
                }
                finally
                {
                    lock (_clientsLock)
                    {
                        _clients.Remove(writer);
                    }
                }
            }
        }

        /// <summary>
        /// Method called by the observer on each state/progress change of a backup job.
        /// </summary>
        public void Update(string action, string name, BackupType type, JobState state, string sourcePath, string targetPath, int totalFiles, long totalSize, long transferTime, long encryptionTime, int progression)
        {
            BroadcastJobStatuses();
        }

        /// <summary>
        /// Broadcasts the current job statuses to all connected clients.
        /// </summary>
        private void BroadcastJobStatuses()
        {
            var statuses = _backupManager.GetJobStatuses();
            string json = JsonSerializer.Serialize(statuses);
            lock (_clientsLock)
            {
                foreach (var writer in _clients.ToArray())
                {
                    try { writer.WriteLine(json); } catch { }
                }
            }
        }
    }
}
