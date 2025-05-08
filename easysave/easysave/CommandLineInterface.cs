using System;
using System.Collections.Generic;

namespace EasySave
{
    public class CommandLineInterface
    {
        private readonly BackupManager backupManager;
        private readonly LanguageManager languageManager;
        private readonly ConfigManager configManager;

        public CommandLineInterface()
        {
            backupManager = BackupManager.GetInstance();
            languageManager = LanguageManager.GetInstance();
            configManager = ConfigManager.GetInstance();

            // Définir la langue à partir de la configuration
            string language = configManager.GetSetting("Language");
            if (!string.IsNullOrEmpty(language))
            {
                languageManager.SetLanguage(language);
            }
        }

        public void ProcessCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            // Diviser la commande par espaces, mais respecter les paramètres entre guillemets
            string[] parts = SplitCommand(command);
            string mainCommand = parts.Length > 0 ? parts[0].ToLower() : "";

            switch (mainCommand)
            {
                case "add":
                    if (parts.Length >= 5)
                    {
                        string name = parts[1];
                        string source = parts[2];
                        string target = parts[3];
                        if (Enum.TryParse<BackupType>(parts[4], true, out BackupType type))
                        {
                            bool success = backupManager.AddBackupJob(name, source, target, type);
                            if (success)
                            {
                                Console.WriteLine(languageManager.GetTranslation("JobAdded"));
                            }
                        }
                        else
                        {
                            Console.WriteLine(languageManager.GetTranslation("InvalidBackupType"));
                        }
                    }
                    else
                    {
                        Console.WriteLine(languageManager.GetTranslation("AddUsage"));
                    }
                    break;

                case "remove":
                    if (parts.Length >= 2)
                    {
                        string name = parts[1];
                        bool success = backupManager.RemoveBackupJob(name);
                        if (success)
                        {
                            Console.WriteLine(languageManager.GetTranslation("JobRemoved"));
                        }
                        else
                        {
                            Console.WriteLine(languageManager.GetTranslation("JobNotFound"));
                        }
                    }
                    else
                    {
                        Console.WriteLine(languageManager.GetTranslation("RemoveUsage"));
                    }
                    break;

                case "update":
                    if (parts.Length >= 6 && int.TryParse(parts[1], out int index))
                    {
                        string name = parts[2];
                        string source = parts[3];
                        string target = parts[4];
                        if (Enum.TryParse<BackupType>(parts[5], true, out BackupType type))
                        {
                            bool success = backupManager.UpdateBackupJob(index - 1, name, source, target, type);
                            if (success)
                            {
                                Console.WriteLine(languageManager.GetTranslation("JobUpdated"));
                            }
                        }
                        else
                        {
                            Console.WriteLine(languageManager.GetTranslation("InvalidBackupType"));
                        }
                    }
                    else
                    {
                        Console.WriteLine(languageManager.GetTranslation("UpdateUsage"));
                    }
                    break;

                case "list":
                    DisplayJobsList();
                    break;

                case "execute":
                    if (parts.Length >= 2)
                    {
                        try
                        {
                            List<int> indices = new List<int>();
                            string jobParam = parts[1];

                            // Gérer une plage (ex: "1-3")
                            if (jobParam.Contains("-"))
                            {
                                string[] range = jobParam.Split('-');
                                if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                                {
                                    for (int i = start; i <= end; i++)
                                    {
                                        indices.Add(i - 1); // Conversion en index 0-based
                                    }
                                }
                            }
                            // Gérer des travaux spécifiques (ex: "1;3;5")
                            else if (jobParam.Contains(";"))
                            {
                                string[] jobs = jobParam.Split(';');
                                foreach (string job in jobs)
                                {
                                    if (int.TryParse(job, out int idx))
                                    {
                                        indices.Add(idx - 1); // Conversion en index 0-based
                                    }
                                }
                            }
                            // Gérer un seul numéro de travail
                            else if (int.TryParse(jobParam, out int idx))
                            {
                                indices.Add(idx - 1); // Conversion en index 0-based
                            }

                            backupManager.ExecuteBackupJob(indices);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{languageManager.GetTranslation("ExecuteError")}: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine(languageManager.GetTranslation("ExecuteUsage"));
                    }
                    break;

                case "lang":
                case "language":
                    if (parts.Length >= 2)
                    {
                        ChangeLanguage(parts[1]);
                    }
                    else
                    {
                        Console.WriteLine(languageManager.GetTranslation("LanguageUsage"));
                    }
                    break;

                case "help":
                    DisplayHelp();
                    break;

                default:
                    Console.WriteLine(languageManager.GetTranslation("UnknownCommand"));
                    DisplayHelp();
                    break;
            }
        }

        public void DisplayMenu()
        {
            Console.WriteLine("\n==== EasySave Outil de Sauvegarde ====");
            DisplayJobsList();
            Console.WriteLine("\n" + languageManager.GetTranslation("MenuCommands"));
            Console.WriteLine("1. add <nom> <source> <cible> <type>: " + languageManager.GetTranslation("MenuAddJob"));
            Console.WriteLine("2. remove <nom>: " + languageManager.GetTranslation("MenuRemoveJob"));
            Console.WriteLine("3. update <index> <nom> <source> <cible> <type>: " + languageManager.GetTranslation("MenuUpdateJob"));
            Console.WriteLine("4. list: " + languageManager.GetTranslation("MenuListJobs"));
            Console.WriteLine("5. execute <index>: " + languageManager.GetTranslation("MenuExecuteJob"));
            Console.WriteLine("6. language <en|fr>: " + languageManager.GetTranslation("MenuChangeLanguage"));
            Console.WriteLine("7. help: " + languageManager.GetTranslation("MenuHelp"));
            Console.WriteLine("8. exit: " + languageManager.GetTranslation("MenuExit"));
            Console.Write("\n> ");
        }

        public void DisplayJobsList()
        {
            List<BackupJob> jobs = backupManager.ListBackups();
            Console.WriteLine("\n-- " + languageManager.GetTranslation("BackupJobs") + " --");

            if (jobs.Count == 0)
            {
                Console.WriteLine(languageManager.GetTranslation("NoJobsDefined"));
                return;
            }

            for (int i = 0; i < jobs.Count; i++)
            {
                BackupJob job = jobs[i];
                Console.WriteLine($"{i + 1}. {job.Name} ({job.Type}): {job.SourcePath} -> {job.TargetPath}");
            }
        }

        public void ChangeLanguage(string language)
        {
            if (language.ToLower() == "en" || language.ToLower() == "fr")
            {
                languageManager.SetLanguage(language.ToLower());
                configManager.SetSetting("Language", language.ToLower());
                Console.WriteLine(languageManager.GetTranslation("LanguageChanged"));
            }
            else
            {
                Console.WriteLine(languageManager.GetTranslation("InvalidLanguage"));
            }
        }

        private void DisplayHelp()
        {
            Console.WriteLine("\n-- " + languageManager.GetTranslation("Help") + " --");
            Console.WriteLine("add <nom> <source> <cible> <type>: " + languageManager.GetTranslation("HelpAddJob"));
            Console.WriteLine("remove <nom>: " + languageManager.GetTranslation("HelpRemoveJob"));
            Console.WriteLine("update <index> <nom> <source> <cible> <type>: " + languageManager.GetTranslation("HelpUpdateJob"));
            Console.WriteLine("list: " + languageManager.GetTranslation("HelpListJobs"));
            Console.WriteLine("execute <index>: " + languageManager.GetTranslation("HelpExecuteJob"));
            Console.WriteLine("execute <debut-fin>: " + languageManager.GetTranslation("HelpExecuteRange"));
            Console.WriteLine("execute <index1;index2;...>: " + languageManager.GetTranslation("HelpExecuteMultiple"));
            Console.WriteLine("language <en|fr>: " + languageManager.GetTranslation("HelpChangeLanguage"));
            Console.WriteLine("help: " + languageManager.GetTranslation("HelpDisplayHelp"));
            Console.WriteLine("exit: " + languageManager.GetTranslation("HelpExit"));
        }

        private string[] SplitCommand(string command)
        {
            List<string> parts = new List<string>();
            string currentPart = "";
            bool inQuotes = false;

            for (int i = 0; i < command.Length; i++)
            {
                char c = command[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ' ' && !inQuotes)
                {
                    if (!string.IsNullOrEmpty(currentPart))
                    {
                        parts.Add(currentPart);
                        currentPart = "";
                    }
                }
                else
                {
                    currentPart += c;
                }
            }

            if (!string.IsNullOrEmpty(currentPart))
            {
                parts.Add(currentPart);
            }

            return parts.ToArray();
        }
    }
}
