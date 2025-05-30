using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EasySave.Models
{
    /// <summary>
    /// Service managing file encryption based on their extensions.
    /// </summary>
    public class EncryptionService
    {
        private static EncryptionService _instance;
        private readonly ConfigManager _configManager;
        private readonly object _lockObject = new object();
        private List<string> _encryptedExtensions;
        private string _encryptionKey;
        private string _cryptoSoftPath;

        /// <summary>
        /// Gets the singleton instance of the encryption service.
        /// </summary>
        /// <returns>The singleton EncryptionService instance.</returns>
        public static EncryptionService GetInstance()
        {
            if (_instance == null)
            {
                _instance = new EncryptionService();
            }
            return _instance;
        }

        /// <summary>
        /// Private constructor for singleton pattern.
        /// Initializes configuration and tries to locate CryptoSoft executable.
        /// </summary>
        private EncryptionService()
        {
            _configManager = ConfigManager.GetInstance();
            LoadConfiguration();

            // Try to get CryptoSoft path from configuration
            _cryptoSoftPath = _configManager.GetSetting("CryptoSoftPath");

            // If path is not set or file does not exist, try possible locations
            if (string.IsNullOrEmpty(_cryptoSoftPath) || !File.Exists(_cryptoSoftPath))
            {
                string[] possiblePaths = new string[]
                {
                        // Standard absolute path
                        @"C:\Program Files\CryptoSoft\CryptoSoft.exe",
                        // Relative to application directory
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft", "CryptoSoft.exe"),
                        // Relative to parent directory of application
                        Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName, "CryptoSoft", "CryptoSoft.exe"),
                        // In AppData folder
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave", "CryptoSoft", "CryptoSoft.exe")
                };

                // Search for the first valid path
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        _cryptoSoftPath = path;
                        break;
                    }
                }
            }

            // Save found path in configuration
            _configManager.SetSetting("CryptoSoftPath", _cryptoSoftPath);
        }

        /// <summary>
        /// Loads encryption configuration from ConfigManager.
        /// </summary>
        private void LoadConfiguration()
        {
            // Load extensions to encrypt
            string extensionsStr = _configManager.GetSetting("EncryptedExtensions") ?? string.Empty;
            _encryptedExtensions = extensionsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            // Load encryption key (hashed password)
            string rawPassword = _configManager.GetSetting("EncryptionPassword") ?? string.Empty;
            if (!string.IsNullOrEmpty(rawPassword))
            {
                // Key is already stored as hash
                _encryptionKey = rawPassword;
            }
        }

        /// <summary>
        /// Sets the password used for encryption and hashes it (at least 64 bits).
        /// </summary>
        /// <param name="password">Plain password.</param>
        public void SetEncryptionPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                _encryptionKey = string.Empty;
                _configManager.SetSetting("EncryptionPassword", string.Empty);
                return;
            }

            // Compute SHA256 hash (256 bits = 32 bytes) of the password
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convert to hexadecimal string for storage
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }

                _encryptionKey = builder.ToString();
                _configManager.SetSetting("EncryptionPassword", _encryptionKey);
            }
        }

        /// <summary>
        /// Sets the list of file extensions to encrypt.
        /// </summary>
        /// <param name="extensions">Enumerable of file extensions.</param>
        public void SetEncryptedExtensions(IEnumerable<string> extensions)
        {
            _encryptedExtensions = extensions.ToList();
            string extensionsStr = string.Join(",", _encryptedExtensions);
            _configManager.SetSetting("EncryptedExtensions", extensionsStr);
        }

        /// <summary>
        /// Gets the list of extensions to encrypt.
        /// </summary>
        /// <returns>List of extensions.</returns>
        public List<string> GetEncryptedExtensions()
        {
            return new List<string>(_encryptedExtensions);
        }

        /// <summary>
        /// Checks if a file should be encrypted based on its extension.
        /// </summary>
        /// <param name="filePath">File path to check.</param>
        /// <returns>True if the file should be encrypted, false otherwise.</returns>
        public bool ShouldEncryptFile(string filePath)
        {
            if (_encryptedExtensions == null || _encryptedExtensions.Count == 0 ||
                string.IsNullOrEmpty(_encryptionKey))
            {
                return false;
            }

            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return !string.IsNullOrEmpty(extension) && _encryptedExtensions.Contains(extension);
        }

        /// <summary>
        /// Encrypts a file using CryptoSoft.
        /// </summary>
        /// <param name="filePath">Path of the file to encrypt.</param>
        /// <returns>Encryption duration in milliseconds.</returns>
        public long EncryptFile(string filePath)
        {
            if (string.IsNullOrEmpty(_encryptionKey))
            {
                Console.WriteLine("Error: encryption key is empty");
                return 0;
            }

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: file to encrypt not found: {filePath}");
                return 0;
            }

            if (!File.Exists(_cryptoSoftPath))
            {
                Console.WriteLine($"Error: CryptoSoft executable not found: {_cryptoSoftPath}");
                return 0;
            }

            try
            {
                Console.WriteLine($"Encrypting file: {filePath}");
                Console.WriteLine($"Using executable: {_cryptoSoftPath}");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _cryptoSoftPath,
                    Arguments = $"\"{filePath}\" \"{_encryptionKey}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    if (!string.IsNullOrEmpty(output))
                    {
                        Console.WriteLine($"CryptoSoft output: {output}");
                    }

                    process.WaitForExit();

                    if (process.ExitCode < 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        Console.WriteLine($"Error during encryption: {error}");
                        return 0;
                    }
                    else
                    {
                        Console.WriteLine("Encryption successful!");
                        // Exit code contains encryption duration in milliseconds
                        return process.ExitCode;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during encryption: {ex.Message}");
                return 0;
            }
        }
    }
}
