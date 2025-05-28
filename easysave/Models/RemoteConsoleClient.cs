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
    /// Client pour la console distante permettant de monitorer et lancer les jobs � distance.
    /// </summary>
    public class RemoteConsoleClient
    {
        private readonly string _host;
        private readonly int _port;
        public event Action<List<BackupManager.BackupJobStatusDto>>? JobsUpdated;
        public event Action OnDisconnected;
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _listenCts;
        private bool _isManualDisconnect = false;

        public RemoteConsoleClient(string host, int port)
        {
            _host = host;
            _port = port;
        }

        // Connexion d�di�e pour le push temps r�el
        public async Task ConnectAsync()
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port);
            _stream = _client.GetStream();
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream) { AutoFlush = true };
            _listenCts = new CancellationTokenSource();
        }

        public void Disconnect()
        {
            try
            {
                _isManualDisconnect = true;
                _listenCts?.Cancel();
                _reader?.Dispose();
                _writer?.Dispose();
                _stream?.Dispose();
                _client?.Close();
            }
            catch { }
        }

        private void HandleConnectionLost()
        {
            if (!_isManualDisconnect)
                OnDisconnected?.Invoke();
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
                    // Ignore les messages qui ne sont pas la r�ponse attendue
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
                HandleConnectionLost();
            }, _listenCts.Token);
        }

        public void StopListening()
        {
            _listenCts?.Cancel();
        }

        public async Task<(bool Success, string Message)> PauseJobAsync(int index)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            await writer.WriteLineAsync($"PAUSE {index}");
            string json = await reader.ReadLineAsync();
            return ParseResult(json);
        }

        public async Task<(bool Success, string Message)> ResumeJobAsync(int index)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            await writer.WriteLineAsync($"RESUME {index}");
            string json = await reader.ReadLineAsync();
            return ParseResult(json);
        }

        public async Task<(bool Success, string Message)> StopJobAsync(int index)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            await writer.WriteLineAsync($"STOP {index}");
            string json = await reader.ReadLineAsync();
            return ParseResult(json);
        }

        public async Task<(bool Success, string Message)> PauseAllJobsAsync()
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            await writer.WriteLineAsync($"PAUSEALL");
            string json = await reader.ReadLineAsync();
            return ParseResult(json);
        }

        public async Task<(bool Success, string Message)> ResumeAllJobsAsync()
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            await writer.WriteLineAsync($"RESUMEALL");
            string json = await reader.ReadLineAsync();
            return ParseResult(json);
        }

        private (bool Success, string Message) ParseResult(string json)
        {
            try
            {
                var result = JsonSerializer.Deserialize<StartJobResult>(json);
                if (result != null)
                    return (result.Success, result.Message);
            }
            catch { }
            return (false, "Erreur de communication");
        }

        private class StartJobResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }
    }
}
