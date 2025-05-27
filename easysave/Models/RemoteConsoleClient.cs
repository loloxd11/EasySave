using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Models
{
    /// <summary>
    /// Client pour la console distante permettant de monitorer et lancer les jobs à distance.
    /// </summary>
    public class RemoteConsoleClient
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _listenCts;
        public event Action<List<BackupManager.BackupJobStatusDto>>? JobsUpdated;

        public RemoteConsoleClient(string host, int port)
        {
            _host = host;
            _port = port;
        }

        // Connexion dédiée pour le push temps réel
        public async Task ConnectAsync()
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port);
            var stream = _client.GetStream();
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream) { AutoFlush = true };
        }

        // Connexion ponctuelle pour les commandes (LIST, START)
        public async Task<List<BackupManager.BackupJobStatusDto>> GetJobStatusesAsync()
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            await writer.WriteLineAsync("LIST");
            string json = await reader.ReadLineAsync();
            return JsonSerializer.Deserialize<List<BackupManager.BackupJobStatusDto>>(json);
        }

        public async Task<(bool Success, string Message)> StartJobAsync(int index)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            await writer.WriteLineAsync($"START {index}");
            while (true)
            {
                string json = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(json)) continue;
                try
                {
                    var result = JsonSerializer.Deserialize<StartJobResult>(json);
                    if (result != null && (json.Contains("\"Success\"") && json.Contains("\"Message\"")))
                        return (result.Success, result.Message);
                }
                catch
                {
                    // Ignore les messages qui ne sont pas la réponse attendue
                }
            }
        }

        public void StartListening(CancellationToken token)
        {
            _listenCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            Task.Run(async () =>
            {
                while (!_listenCts.IsCancellationRequested)
                {
                    try
                    {
                        string? json = await _reader.ReadLineAsync();
                        if (json == null) break;
                        var jobs = JsonSerializer.Deserialize<List<BackupManager.BackupJobStatusDto>>(json);
                        if (jobs != null)
                            JobsUpdated?.Invoke(jobs);
                    }
                    catch { break; }
                }
            }, _listenCts.Token);
        }

        public void StopListening()
        {
            _listenCts?.Cancel();
        }

        private class StartJobResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }
    }
}
