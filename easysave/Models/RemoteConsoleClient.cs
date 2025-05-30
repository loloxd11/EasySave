using System.IO;
using System.Net.Sockets;
using System.Text.Json;

namespace EasySave.Models
{
    /// <summary>
    /// Client for the remote console, allowing monitoring and launching jobs remotely.
    /// </summary>
    public class RemoteConsoleClient
    {
        private readonly string _host;
        private readonly int _port;

        /// <summary>
        /// Event triggered when the list of jobs is updated.
        /// </summary>
        public event Action<List<BackupManager.BackupJobStatusDto>>? JobsUpdated;

        /// <summary>
        /// Event triggered when the connection is lost.
        /// </summary>
        public event Action OnDisconnected;

        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _listenCts;
        private bool _isManualDisconnect = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteConsoleClient"/> class.
        /// </summary>
        /// <param name="host">The remote host address.</param>
        /// <param name="port">The remote port number.</param>
        public RemoteConsoleClient(string host, int port)
        {
            _host = host;
            _port = port;
        }

        /// <summary>
        /// Connects to the remote server for real-time push updates.
        /// </summary>
        public async Task ConnectAsync()
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port);
            _stream = _client.GetStream();
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream) { AutoFlush = true };
            _listenCts = new CancellationTokenSource();
        }

        /// <summary>
        /// Disconnects from the remote server and releases resources.
        /// </summary>
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

        /// <summary>
        /// Handles the event when the connection is lost unexpectedly.
        /// </summary>
        private void HandleConnectionLost()
        {
            if (!_isManualDisconnect)
                OnDisconnected?.Invoke();
        }

        /// <summary>
        /// Gets the list of job statuses from the server (one-time connection).
        /// </summary>
        /// <returns>List of <see cref="BackupManager.BackupJobStatusDto"/>.</returns>
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

        /// <summary>
        /// Sends a command to start a job by index.
        /// </summary>
        /// <param name="index">Index of the job to start.</param>
        /// <returns>Tuple indicating success and a message.</returns>
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
                }
            }
        }

        /// <summary>
        /// Starts listening for real-time job updates from the server.
        /// </summary>
        /// <param name="token">Cancellation token to stop listening.</param>
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

        /// <summary>
        /// Stops listening for real-time updates.
        /// </summary>
        public void StopListening()
        {
            _listenCts?.Cancel();
        }

        /// <summary>
        /// Sends a command to pause a job by index.
        /// </summary>
        /// <param name="index">Index of the job to pause.</param>
        /// <returns>Tuple indicating success and a message.</returns>
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

        /// <summary>
        /// Sends a command to resume a job by index.
        /// </summary>
        /// <param name="index">Index of the job to resume.</param>
        /// <returns>Tuple indicating success and a message.</returns>
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

        /// <summary>
        /// Sends a command to stop a job by index.
        /// </summary>
        /// <param name="index">Index of the job to stop.</param>
        /// <returns>Tuple indicating success and a message.</returns>
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

        /// <summary>
        /// Sends a command to pause all jobs.
        /// </summary>
        /// <returns>Tuple indicating success and a message.</returns>
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

        /// <summary>
        /// Sends a command to resume all jobs.
        /// </summary>
        /// <returns>Tuple indicating success and a message.</returns>
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

        /// <summary>
        /// Parses the result JSON string into a tuple.
        /// </summary>
        /// <param name="json">JSON string to parse.</param>
        /// <returns>Tuple indicating success and a message.</returns>
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

        /// <summary>
        /// DTO for job start/stop/pause/resume command results.
        /// </summary>
        private class StartJobResult
        {
            /// <summary>
            /// Indicates if the command was successful.
            /// </summary>
            public bool Success { get; set; }
            /// <summary>
            /// Message returned by the server.
            /// </summary>
            public string Message { get; set; }
        }
    }
}
