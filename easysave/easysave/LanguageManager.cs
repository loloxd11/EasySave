using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave
{
    public class LanguageManager
    {
        private static LanguageManager instance;
        private string currentLanguage;
        private Dictionary<string, string> translations;
        private readonly string enJsonPath;
        private readonly string frJsonPath;

        private LanguageManager()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appDataPath, "EasySave", "Config");

            // S'assurer que le r�pertoire de configuration existe
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            enJsonPath = Path.Combine(configDir, "en.json");
            frJsonPath = Path.Combine(configDir, "fr.json");

            // Initialiser avec la langue par d�faut
            currentLanguage = "fr";
            LoadLanguageFile(currentLanguage);
        }

        public static LanguageManager GetInstance()
        {
            if (instance == null)
            {
                instance = new LanguageManager();
            }
            return instance;
        }

        public void SetLanguage(string language)
        {
            if (language.ToLower() == "en" || language.ToLower() == "fr")
            {
                currentLanguage = language.ToLower();
                LoadLanguageFile(currentLanguage);
            }
        }

        public string GetTranslation(string key)
        {
            if (translations != null && translations.ContainsKey(key))
            {
                return translations[key];
            }
            return key; // Retourner la cl� elle-m�me si la traduction n'est pas trouv�e
        }

        private void LoadLanguageFile(string language)
        {
            try
            {
                string filePath = (language.ToLower() == "fr") ? frJsonPath : enJsonPath;

                // Cr�er des fichiers de traduction par d�faut s'ils n'existent pas
                if (!File.Exists(filePath))
                {
                    CreateDefaultTranslationFile(language);
                }

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                }
                else
                {
                    // Utiliser un dictionnaire vide en cas d'�chec de chargement du fichier
                    translations = new Dictionary<string, string>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement du fichier de langue: {ex.Message}");
                translations = new Dictionary<string, string>();
            }
        }

        private void CreateDefaultTranslationFile(string language)
        {
            Dictionary<string, string> defaultTranslations = new Dictionary<string, string>();

            if (language.ToLower() == "fr")
            {
                defaultTranslations = new Dictionary<string, string>
                {
                    { "MaxBackupJobsReached", "Le nombre maximum de travaux de sauvegarde a �t� atteint (5)." },
                    { "AddOrDeletetTitle", "Modifier ou Supprimer une sauvegarde" },
                    { "PromptAddOrDelete", "Entrez le num�ro de la sauvegarde � modifier ou supprimer : " },
                    { "PromptExecuteJobIndex" ,"Entrez le num�ro de la sauvegarde � ex�cuter ou 'all' pour toutes : "},
                    { "YourChoice", "Votre choix : " },
                    { "PromptJobName", "Nom de la sauvegarde : " },
                    { "ListJobs", "Liste des sauvegardes" },
                    { "PromptJobSrc", "Chemin source : " },
                    { "PromptJobDst", "Chemin cible : " },
                    { "PromptJobType", "Type de sauvegarde (0 pour compl�te, 1 pour diff�rentielle) : " },
                    { "SourceDirNotFound", "Le r�pertoire source n'existe pas." },
                    { "TargetDirCreateFailed", "Impossible de cr�er le r�pertoire cible" },
                    { "JobNameExists", "Un travail avec ce nom existe d�j�." },
                    { "ExecutingJob", "Ex�cution du travail" },
                    { "ErrorExecutingJob", "Erreur lors de l'ex�cution du travail" },
                    { "InvalidJobIndex", "Index de travail invalide" },
                    { "JobAdded", "Travail de sauvegarde ajout� avec succ�s" },
                    { "JobRemoved", "Travail de sauvegarde supprim� avec succ�s" },
                    { "JobUpdated", "Travail de sauvegarde mis � jour avec succ�s" },
                    { "JobNotFound", "Travail de sauvegarde non trouv�" },
                    { "InvalidBackupType", "Type de sauvegarde invalide" },
                    { "AddUsage", "Usage: add <nom> <source> <cible> <type>" },
                    { "RemoveUsage", "Usage: remove <nom>" },
                    { "UpdateUsage", "Usage: update <index> <nom> <source> <cible> <type>" },
                    { "ExecuteUsage", "Usage: execute <index> ou execute <debut-fin> ou execute <index1;index2;...>" },
                    { "LanguageUsage", "Usage: language <en|fr>" },
                    { "ExecuteError", "Erreur lors de l'ex�cution" },
                    { "UnknownCommand", "Commande inconnue" },
                    { "MenuCommands", "Commandes disponibles:" },
                    { "MenuAddJob", "Ajouter un travail de sauvegarde" },
                    { "MenuRemoveJob", "Supprimer un travail de sauvegarde" },
                    { "MenuUpdateJob", "Mettre � jour un travail de sauvegarde" },
                    { "MenuListJobs", "Lister les travaux de sauvegarde" },
                    { "MenuExecuteJob", "Ex�cuter un travail de sauvegarde" },
                    { "MenuChangeLanguage", "Changer de langue" },
                    { "PromptChangeLanguage","Entrez 'en' pour anglais ou 'fr' pour fran�ais : " },
                    { "MenuHelp", "Afficher l'aide" },
                    { "MenuExit", "Quitter" },
                    { "BackupJobs", "Travaux de sauvegarde" },
                    { "NoJobsDefined", "Aucun travail de sauvegarde d�fini" },
                    { "LanguageChanged", "Langue chang�e avec succ�s" },
                    { "InvalidLanguage", "Langue invalide. Utilisez 'en' ou 'fr'" },
                };
            }
            else // English is default
            {
                defaultTranslations = new Dictionary<string, string>
                {
                    { "MaxBackupJobsReached", "Maximum number of backup jobs reached (5)." },
                    { "AddOrDeletetTitle", "Modify or Delete a backup" },
                    { "PromptAddOrDelete", "Enter the number of the backup to modify or delete: " },
                    { "PromptExecuteJobIndex", "Enter the number of the backup to execute or 'all' for all: " },
                    { "YourChoice", "Your choice: " },
                    { "PromptJobName", "Backup name: " },
                    { "ListJobs", "List of backups" },
                    { "PromptJobSrc", "Source path: " },
                    { "PromptJobDst", "Target path: " },
                    { "PromptJobType", "Backup type (0 for complete, 1 for differential): " },
                    { "SourceDirNotFound", "Source directory does not exist." },
                    { "TargetDirCreateFailed", "Failed to create target directory" },
                    { "JobNameExists", "A job with this name already exists." },
                    { "ExecutingJob", "Executing job" },
                    { "ErrorExecutingJob", "Error executing job" },
                    { "InvalidJobIndex", "Invalid job index" },
                    { "JobAdded", "Backup job added successfully" },
                    { "JobRemoved", "Backup job removed successfully" },
                    { "JobUpdated", "Backup job updated successfully" },
                    { "JobNotFound", "Backup job not found" },
                    { "InvalidBackupType", "Invalid backup type" },
                    { "AddUsage", "Usage: add <name> <source> <target> <type>" },
                    { "RemoveUsage", "Usage: remove <name>" },
                    { "UpdateUsage", "Usage: update <index> <name> <source> <target> <type>" },
                    { "ExecuteUsage", "Usage: execute <index> or execute <start-end> or execute <index1;index2;...>" },
                    { "LanguageUsage", "Usage: language <en|fr>" },
                    { "ExecuteError", "Error executing" },
                    { "UnknownCommand", "Unknown command" },
                    { "MenuCommands", "Available commands:" },
                    { "MenuAddJob", "Add a backup job" },
                    { "MenuRemoveJob", "Remove a backup job" },
                    { "MenuUpdateJob", "Update a backup job" },
                    { "MenuListJobs", "List backup jobs" },
                    { "MenuExecuteJob", "Execute a backup job" },
                    { "MenuChangeLanguage", "Change language" },
                    { "PromptChangeLanguage", "Enter 'en' for English or 'fr' for French: " },
                    { "MenuHelp", "Display help" },
                    { "MenuExit", "Exit" },
                    { "BackupJobs", "Backup jobs" },
                    { "NoJobsDefined", "No backup jobs defined" },
                    { "LanguageChanged", "Language changed successfully" },
                    { "InvalidLanguage", "Invalid language. Use 'en' or 'fr'" },
                };
            }

            try
            {
                string filePath = (language.ToLower() == "fr") ? frJsonPath : enJsonPath;
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true // Pour l'affichage avec des sauts de ligne
                };

                string json = JsonSerializer.Serialize(defaultTranslations, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la cr�ation du fichier de traduction par d�faut: {ex.Message}");
            }
        }
    }
}
