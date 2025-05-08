using System;
using System.IO;
using System.Collections.Generic;

namespace easysave
{
    // Interface commune pour les stratégies de sauvegarde
    public interface IBackupStrategy
    {
        void Execute(string sourcePath, string targetPath, string name);
    }

    // Stratégie de sauvegarde complète
    public class CompleteBackupStrategy : IBackupStrategy
    {
        public void Execute(string sourcePath, string targetPath, string name)
        {
            Console.WriteLine($"Exécution d'une sauvegarde complète: {name}");
            // Utilise la méthode existante dans FileManager pour copier tous les fichiers
            FileManager.CopyFile(sourcePath, targetPath, name);
        }
    }

    // Stratégie de sauvegarde différentielle
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        public void Execute(string sourcePath, string targetPath, string name)
        {
            Console.WriteLine($"Exécution d'une sauvegarde différentielle: {name}");

            string targetDirectory = Path.Combine(targetPath, name);

            // Vérifie si le répertoire cible existe déjà
            if (!Directory.Exists(targetDirectory))
            {
                // Si le répertoire n'existe pas, effectuer une sauvegarde complète
                Console.WriteLine("Aucune sauvegarde précédente trouvée, exécution d'une sauvegarde complète.");
                FileManager.CopyFile(sourcePath, targetPath, name);
                return;
            }

            // Créer le répertoire cible s'il n'existe pas
            FileManager.CreateDirectory(targetPath, name);

            try
            {
                // Parcourir tous les fichiers du répertoire source
                foreach (string sourceFile in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    // Obtenir le chemin relatif par rapport au répertoire source
                    string relativePath = Path.GetRelativePath(sourcePath, sourceFile);

                    // Construire le chemin complet du fichier cible
                    string targetFile = Path.Combine(targetDirectory, relativePath);

                    // Vérifier si le fichier existe déjà dans la destination
                    bool shouldCopy = !File.Exists(targetFile);

                    if (!shouldCopy)
                    {
                        // Si le fichier existe, vérifier s'il a été modifié
                        DateTime sourceLastWrite = File.GetLastWriteTime(sourceFile);
                        DateTime targetLastWrite = File.GetLastWriteTime(targetFile);

                        // Copier si le fichier source est plus récent
                        shouldCopy = sourceLastWrite > targetLastWrite;
                    }

                    if (shouldCopy)
                    {
                        // Créer les sous-répertoires nécessaires
                        string targetFileDirectory = Path.GetDirectoryName(targetFile)!;
                        if (!Directory.Exists(targetFileDirectory))
                        {
                            Directory.CreateDirectory(targetFileDirectory);
                        }

                        // Copier le fichier
                        File.Copy(sourceFile, targetFile, true);
                        Console.WriteLine($"Fichier copié (différentiel): {relativePath}");
                    }
                    else
                    {
                        Console.WriteLine($"Fichier ignoré (inchangé): {relativePath}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erreur lors de la sauvegarde différentielle: {e.Message}");
            }
        }
    }
}
