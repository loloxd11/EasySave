using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

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

            // Essayer de r�cup�rer le chemin de CryptoSoft depuis la configuration
            _cryptoSoftPath = _configManager.GetSetting("CryptoSoftPath");

            // Si le chemin n'est pas d�fini ou si le fichier n'existe pas � cet emplacement
            if (string.IsNullOrEmpty(_cryptoSoftPath) || !File.Exists(_cryptoSoftPath))
            {
                // Essayer diff�rents emplacements possibles
                string[] possiblePaths = new string[]
                {
                    // Chemin absolu standard
                    @"C:\Program Files\CryptoSoft\CryptoSoft.exe",
            
                    // Chemin relatif par rapport au r�pertoire de l'application
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft", "CryptoSoft.exe"),
            
                    // Chemin relatif par rapport au r�pertoire parent de l'application
                    Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName, "CryptoSoft", "CryptoSoft.exe"),
            
                    // Dans le dossier AppData
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave", "CryptoSoft", "CryptoSoft.exe")
                };

                // Chercher le premier chemin valide
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        _cryptoSoftPath = path;
                        break;
                    }
                }
            }

            // Sauvegarde du chemin trouv� dans la configuration
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
                return 0;
            }

            try
            {
                Console.WriteLine($"Chiffrement du fichier: {filePath}");
                Console.WriteLine($"Avec l'ex�cutable: {_cryptoSoftPath}");

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
                        Console.WriteLine($"Sortie de CryptoSoft: {output}");
                    }

                    process.WaitForExit();

                    if (process.ExitCode < 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        Console.WriteLine($"Erreur lors du chiffrement: {error}");
                        return 0;
                    }
                    else
                    {
                        Console.WriteLine("Chiffrement r�ussi!");
                        // L'exit code contient la dur�e du chiffrement en millisecondes
                        return process.ExitCode;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception lors du chiffrement: {ex.Message}");
                return 0;
            }
        }
    }
}
