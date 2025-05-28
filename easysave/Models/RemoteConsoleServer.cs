using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace EasySave.Models
{
    /// <summary>
    /// Serveur pour la console distante permettant de monitorer et lancer les jobs � distance.
    /// </summary>
    public class RemoteConsoleServer : IObserver
    {
        private readonly int _port;
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private readonly BackupManager _backupManager;
        private readonly List<StreamWriter> _clients = new();
        private readonly object _clientsLock = new();

        public event Action<bool> ServerStatusChanged;

        public RemoteConsoleServer(int port)
        {
            _port = port;
            _backupManager = BackupManager.GetInstance();
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _backupManager.AttachObserver(this);
            Task.Run(() => AcceptLoop(_cts.Token));
            ServerStatusChanged?.Invoke(true);
        }

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
                // Envoi immédiat de l'état courant au nouveau client
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
                                // Répondre immédiatement avant d'attendre la fin de la sauvegarde
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = true, Message = "Job lancé" }));
                                // Lancer la sauvegarde en tache de fond
                                _ = _backupManager.ExecuteJobsAsync(new List<int> { idx });
                            }
                            else
                            {
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = false, Message = "Index invalide" }));
                            }
                        }
                        else if (line.StartsWith("PAUSEALL", StringComparison.OrdinalIgnoreCase))
                        {
                            _backupManager.PauseBackupJobs(reason: "Remote pause all");
                            await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = true, Message = "Tous les jobs mis en pause" }));
                        }
                        else if (line.StartsWith("RESUMEALL", StringComparison.OrdinalIgnoreCase))
                        {
                            _backupManager.ResumeBackupJobs();
                            await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = true, Message = "Tous les jobs repris" }));
                        }
                        else if (line.StartsWith("PAUSE ", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(line.Substring(6), out int idx))
                            {
                                _backupManager.PauseBackupJobs(new[] { idx }, "Remote pause");
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = true, Message = $"Job {idx} mis en pause" }));
                            }
                            else
                            {
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = false, Message = "Index invalide" }));
                            }
                        }
                        else if (line.StartsWith("RESUME ", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(line.Substring(7), out int idx))
                            {
                                _backupManager.ResumeBackupJobs(new[] { idx });
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = true, Message = $"Job {idx} repris" }));
                            }
                            else
                            {
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = false, Message = "Index invalide" }));
                            }
                        }
                        else if (line.StartsWith("STOP ", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(line.Substring(5), out int idx))
                            {
                                bool killed = _backupManager.KillBackupJob(idx);
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = killed, Message = killed ? $"Job {idx} arrêté" : "Impossible d'arrêter le job" }));
                            }
                            else
                            {
                                await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = false, Message = "Index invalide" }));
                            }
                        }
                        else
                        {
                            await writer.WriteLineAsync(JsonSerializer.Serialize(new { Success = false, Message = "Commande inconnue" }));
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

        // Méthode appelée par l'observer à chaque changement d'état/progression
        public void Update(string action, string name, BackupType type, JobState state, string sourcePath, string targetPath, int totalFiles, long totalSize, long transferTime, long encryptionTime, int progression)
        {
            BroadcastJobStatuses();
        }

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
