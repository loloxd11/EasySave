using System;
using System.IO;
using System.Collections.Generic;

namespace easysave
{
    // Interface commune pour les strat�gies de sauvegarde
    public interface IBackupStrategy
    {
        void Execute(string sourcePath, string targetPath, string name);
    }

    // Strat�gie de sauvegarde compl�te
    public class CompleteBackupStrategy : IBackupStrategy
    {
        public void Execute(string sourcePath, string targetPath, string name)
        {
            Console.WriteLine($"Ex�cution d'une sauvegarde compl�te: {name}");
            // Utilise la m�thode existante dans FileManager pour copier tous les fichiers
            FileManager.CopyFile(sourcePath, targetPath, name);
        }
    }

    // Strat�gie de sauvegarde diff�rentielle
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        public void Execute(string sourcePath, string targetPath, string name)
        {
            Console.WriteLine($"Ex�cution d'une sauvegarde diff�rentielle: {name}");

            string targetDirectory = Path.Combine(targetPath, name);

            // V�rifie si le r�pertoire cible existe d�j�
            if (!Directory.Exists(targetDirectory))
            {
                // Si le r�pertoire n'existe pas, effectuer une sauvegarde compl�te
                Console.WriteLine("Aucune sauvegarde pr�c�dente trouv�e, ex�cution d'une sauvegarde compl�te.");
                FileManager.CopyFile(sourcePath, targetPath, name);
                return;
            }

            // Cr�er le r�pertoire cible s'il n'existe pas
            FileManager.CreateDirectory(targetPath, name);

            try
            {
                // Parcourir tous les fichiers du r�pertoire source
                foreach (string sourceFile in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    // Obtenir le chemin relatif par rapport au r�pertoire source
                    string relativePath = Path.GetRelativePath(sourcePath, sourceFile);

                    // Construire le chemin complet du fichier cible
                    string targetFile = Path.Combine(targetDirectory, relativePath);

                    // V�rifier si le fichier existe d�j� dans la destination
                    bool shouldCopy = !File.Exists(targetFile);

                    if (!shouldCopy)
                    {
                        // Si le fichier existe, v�rifier s'il a �t� modifi�
                        DateTime sourceLastWrite = File.GetLastWriteTime(sourceFile);
                        DateTime targetLastWrite = File.GetLastWriteTime(targetFile);

                        // Copier si le fichier source est plus r�cent
                        shouldCopy = sourceLastWrite > targetLastWrite;
                    }

                    if (shouldCopy)
                    {
                        // Cr�er les sous-r�pertoires n�cessaires
                        string targetFileDirectory = Path.GetDirectoryName(targetFile)!;
                        if (!Directory.Exists(targetFileDirectory))
                        {
                            Directory.CreateDirectory(targetFileDirectory);
                        }

                        // Copier le fichier
                        File.Copy(sourceFile, targetFile, true);
                        Console.WriteLine($"Fichier copi� (diff�rentiel): {relativePath}");
                    }
                    else
                    {
                        Console.WriteLine($"Fichier ignor� (inchang�): {relativePath}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erreur lors de la sauvegarde diff�rentielle: {e.Message}");
            }
        }
    }
}
