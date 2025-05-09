using System;
using System.Collections.Generic;
using System.IO;

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

        public void Start()
        {
            bool exit = false;

            while (!exit)
            {
                DisplayMainMenu();
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        AddBackupMenu();
                        break;
                    case "2":
                        UpdateOrRemoveBackupMenu();
                        break;
                    case "3":
                        ExecuteBackupMenu();
                        break;
                    case "4":
                        ListBackups();
                        break;
                    case "5":
                        ChangeLanguageMenu();
                        break;
                    case "6":
                        exit = true;
                        Console.WriteLine(languageManager.GetTranslation("MenuExit"));
                        break;
                    default:
                        Console.WriteLine(languageManager.GetTranslation("UnknownCommand"));
                        break;
                }
            }
        }

        private void DisplayMainMenu()
        {
            Console.WriteLine("===== EasySave =====");
            Console.WriteLine("1. " + languageManager.GetTranslation("MenuAddJob"));
            Console.WriteLine("2. " + languageManager.GetTranslation("MenuUpdateJob"));
            Console.WriteLine("3. " + languageManager.GetTranslation("MenuExecuteJob"));
            Console.WriteLine("4. " + languageManager.GetTranslation("MenuListJobs"));
            Console.WriteLine("5. " + languageManager.GetTranslation("MenuChangeLanguage"));
            Console.WriteLine("6. " + languageManager.GetTranslation("MenuExit"));
            Console.WriteLine("=============================");
            Console.Write("Votre choix : ");
        }

        private void AddBackupMenu()
        {
            Console.Clear();
            Console.WriteLine("=========== "+languageManager.GetTranslation("MenuAddJob")+ " ===========");
            Console.Write(languageManager.GetTranslation("PromptJobName"));
            string name = Console.ReadLine();

            Console.Write(languageManager.GetTranslation("PromptJobSrc"));
            string source = Console.ReadLine();

            Console.Write(languageManager.GetTranslation("PromptJobDst"));
            string target = Console.ReadLine();

            Console.Write(languageManager.GetTranslation("PromptJobType"));
            string typeInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target) || !int.TryParse(typeInput, out int type) || (type != 0 && type != 1))
            {
                Console.WriteLine(languageManager.GetTranslation("InvalidInput"));
                return;
            }

            if (source.StartsWith("\"") && source.EndsWith("\""))
            {
                source = source.Trim('"');
            }

            if (target.StartsWith("\"") && target.EndsWith("\""))
            {
                target = target.Trim('"');
            }

            BackupType backupType = type == 0 ? BackupType.Complete : BackupType.Differential;
            bool success = backupManager.AddBackupJob(name, source, target, backupType);

            if (success)
            {
                Console.WriteLine(languageManager.GetTranslation("JobAdded"));
            }
            else
            {
                Console.WriteLine(languageManager.GetTranslation("JobNameExists"));
            }
        }

        private void UpdateOrRemoveBackupMenu()
        {
            Console.Clear();
            Console.WriteLine("====="+ languageManager.GetTranslation("AddOrDeletetTitle")+ "=====");
            ListBackups();

            Console.Write(languageManager.GetTranslation("PromptAddOrDelete"));
            string input = Console.ReadLine();

            if (!int.TryParse(input, out int index) || index < 1 || index > backupManager.ListBackups().Count)
            {
                Console.WriteLine(languageManager.GetTranslation("InvalidJobIndex"));
                return;
            }

            Console.WriteLine("1. " + languageManager.GetTranslation("MenuUpdateJob"));
            Console.WriteLine("2. " + languageManager.GetTranslation("MenuRemoveJob"));
            Console.Write("Votre choix : ");
            string choice = Console.ReadLine();

            if (choice == "1")
            {
                UpdateBackup(index - 1);
            }
            else if (choice == "2")
            {
                RemoveBackup(index - 1);
            }
            else
            {
                Console.WriteLine(languageManager.GetTranslation("UnknownCommand"));
            }
        }

        private void UpdateBackup(int index)
        {
            Console.Clear();
            Console.WriteLine("=====" + languageManager.GetTranslation("MenuUpdateJob") + "=====");
            Console.Write(languageManager.GetTranslation("PromptJobName"));
            string name = Console.ReadLine();

            Console.Write(languageManager.GetTranslation("PromptJobSrc"));
            string source = Console.ReadLine();

            Console.Write(languageManager.GetTranslation("PromptJobDst"));
            string target = Console.ReadLine();

            Console.Write(languageManager.GetTranslation("PromptJobType"));
            string typeInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target) || !int.TryParse(typeInput, out int type) || (type != 0 && type != 1))
            {
                Console.WriteLine(languageManager.GetTranslation("InvalidInput"));
                return;
            }

            BackupType backupType = type == 0 ? BackupType.Complete : BackupType.Differential;
            bool success = backupManager.UpdateBackupJob(index, name, source, target, backupType);

            if (success)
            {
                Console.WriteLine(languageManager.GetTranslation("JobUpdated"));
            }
            else
            {
                Console.WriteLine(languageManager.GetTranslation("JobNotFound"));
            }
        }

        private void RemoveBackup(int index)
        {
            Console.Clear();
            bool success = backupManager.RemoveBackupJob(index);

            if (success)
            {
                Console.WriteLine(languageManager.GetTranslation("JobRemoved"));
            }
            else
            {
                Console.WriteLine(languageManager.GetTranslation("JobNotFound"));
            }
        }

        private void ExecuteBackupMenu()
        {
            Console.Clear();
            Console.WriteLine("====="+ languageManager.GetTranslation("MenuExecuteJob") + "=====");
            ListBackups();

            Console.Write(languageManager.GetTranslation("PromptExecuteJobIndex"));
            string input = Console.ReadLine();

            List<int> indices = new List<int>();

            if (input.ToLower() == "all")
            {
                indices = new List<int>();
                for (int i = 0; i < backupManager.ListBackups().Count; i++)
                {
                    indices.Add(i);
                }
            }
            else if (int.TryParse(input, out int index) && index >= 1 && index <= backupManager.ListBackups().Count)
            {
                indices.Add(index - 1);
            }
            else
            {
                Console.WriteLine(languageManager.GetTranslation("InvalidJobIndex"));
                return;
            }

            backupManager.ExecuteBackupJob(indices);
            Console.WriteLine(languageManager.GetTranslation("ExecutingJob"));
        }

        private void ListBackups()
        {
            Console.Clear();
            Console.WriteLine("====="+ languageManager.GetTranslation("ListJobs") + "=====");
            var backups = backupManager.ListBackups();

            if (backups.Count == 0)
            {
                Console.WriteLine(languageManager.GetTranslation("NoJobsDefined"));
                return;
            }

            for (int i = 0; i < backups.Count; i++)
            {
                var job = backups[i];
                Console.WriteLine($"{i + 1}. {job.Name} ({job.Type}): {job.SourcePath} -> {job.TargetPath}");
            }
        }

        private void ChangeLanguageMenu()
        {
            Console.Clear();
            Console.WriteLine("====="+ languageManager.GetTranslation("MenuChangeLanguage") + "=====");
            Console.WriteLine(languageManager.GetTranslation("PromptChangeLanguage"));
            string language = Console.ReadLine();

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

    }
}
