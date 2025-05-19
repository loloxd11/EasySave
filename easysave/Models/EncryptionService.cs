using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace EasySave.Models
{
    /// <summary>
    /// Service g�rant le chiffrement des fichiers selon leurs extensions
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
        /// Obtient l'instance singleton du service de chiffrement
        /// </summary>
        public static EncryptionService GetInstance()
        {
            if (_instance == null)
            {
                _instance = new EncryptionService();
            }
            return _instance;
        }

        /// <summary>
        /// Constructeur priv� pour le pattern singleton
        /// </summary>
        private EncryptionService()
        {
            _configManager = ConfigManager.GetInstance();
            LoadConfiguration();

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string cryptoSoftPath = Path.Combine(
                appDataPath,
                "EasySave",
                "CryptoSoft",
                "bin",
                "Release",
                "net8.0",
                "win-x64",
                "CryptoSoft.exe"
            );
            _cryptoSoftPath = cryptoSoftPath;



            // Sauvegarde du chemin dans la configuration
            _configManager.SetSetting("CryptoSoftPath", _cryptoSoftPath);
        }

        /// <summary>
        /// Charge la configuration de chiffrement depuis le ConfigManager
        /// </summary>
        private void LoadConfiguration()
        {
            // Charger les extensions � chiffrer
            string extensionsStr = _configManager.GetSetting("EncryptedExtensions") ?? string.Empty;
            _encryptedExtensions = extensionsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            // Charger la cl� de chiffrement (mot de passe hash�)
            string rawPassword = _configManager.GetSetting("EncryptionPassword") ?? string.Empty;
            if (!string.IsNullOrEmpty(rawPassword))
            {
                // La cl� est d�j� stock�e sous forme de hash
                _encryptionKey = rawPassword;
            }
        }

        /// <summary>
        /// D�finit le mot de passe utilis� pour le chiffrement et le transforme en hash de 64 bits minimum
        /// </summary>
        /// <param name="password">Mot de passe en clair</param>
        public void SetEncryptionPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                _encryptionKey = string.Empty;
                _configManager.SetSetting("EncryptionPassword", string.Empty);
                return;
            }

            // Calculer un hash SHA256 (256 bits = 32 octets) du mot de passe
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convertir en cha�ne hexad�cimale pour stockage
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
        /// D�finit la liste des extensions de fichiers � chiffrer
        /// </summary>
        public void SetEncryptedExtensions(IEnumerable<string> extensions)
        {
            _encryptedExtensions = extensions.ToList();
            string extensionsStr = string.Join(",", _encryptedExtensions);
            _configManager.SetSetting("EncryptedExtensions", extensionsStr);
        }

        /// <summary>
        /// Obtient la liste des extensions � chiffrer
        /// </summary>
        public List<string> GetEncryptedExtensions()
        {
            return new List<string>(_encryptedExtensions);
        }

        /// <summary>
        /// V�rifie si un fichier doit �tre chiffr� en fonction de son extension
        /// </summary>
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
        /// Chiffre un fichier en utilisant CryptoSoft
        /// </summary>
        /// <param name="filePath">Chemin du fichier � chiffrer</param>
        /// <returns>Dur�e du chiffrement en millisecondes</returns>
        // Dans EncryptionService.cs, modifiez la m�thode suivante pour ajouter du logging :

        public long EncryptFile(string filePath)
        {
            if (string.IsNullOrEmpty(_encryptionKey))
            {
                Console.WriteLine("Erreur: cl� de chiffrement vide");
                return 0;
            }

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Erreur: fichier � chiffrer introuvable: {filePath}");
                return 0;
            }

            if (!File.Exists(_cryptoSoftPath))
            {
                Console.WriteLine($"Erreur: ex�cutable CryptoSoft introuvable: {_cryptoSoftPath}");
                // Essayer de localiser CryptoSoft ailleurs
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string alternativePath = Path.Combine(appDataPath, "EasySave", @"..\..\..\CryptoSoft\bin\Release\net8.0\win-x64\CryptoSoft.exe");
                if (File.Exists(alternativePath))
                {
                    Console.WriteLine($"CryptoSoft trouv� � l'emplacement alternatif: {alternativePath}");
                    _cryptoSoftPath = alternativePath;
                    _configManager.SetSetting("CryptoSoftPath", _cryptoSoftPath);
                }
                else
                {
                    return 0;
                }
            }

            long startTime = DateTime.Now.Ticks;

            try
            {
                // Affichage des informations pour le d�bogage
                Console.WriteLine($"Chiffrement du fichier: {filePath}");
                Console.WriteLine($"Avec l'ex�cutable: {_cryptoSoftPath}");

                // Lancer CryptoSoft avec le fichier � chiffrer et la cl� comme arguments
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
                    // Lire la sortie standard
                    string output = process.StandardOutput.ReadToEnd();
                    if (!string.IsNullOrEmpty(output))
                    {
                        Console.WriteLine($"Sortie de CryptoSoft: {output}");
                    }

                    process.WaitForExit();

                    // V�rifier si le processus s'est termin� correctement
                    if (process.ExitCode != 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        Console.WriteLine($"Erreur lors du chiffrement: {error}");
                        return 0;
                    }
                    else
                    {
                        Console.WriteLine("Chiffrement r�ussi!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception lors du chiffrement: {ex.Message}");
                return 0;
            }

            long endTime = DateTime.Now.Ticks;
            return (endTime - startTime) / TimeSpan.TicksPerMillisecond;
        }

    }
}

