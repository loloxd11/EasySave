using System.Threading; // Ajoutez cet using

namespace CryptoSoft;

public static class Program
{
    public static void Main(string[] args)
    {
        // Nom unique pour le mutex (par exemple, basé sur le nom de l'application)
        const string mutexName = "CryptoSoft_MonoInstance_Mutex";
        using var mutex = new Mutex(true, mutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            Console.WriteLine("Une instance de CryptoSoft est déjà en cours d'exécution.");
            Environment.Exit(-100);
        }

        try
        {
            foreach (var arg in args)
            {
                Console.WriteLine(arg);
            }

            var fileManager = new FileManager(args[0], args[1]);
            int ElapsedTime = fileManager.TransformFile();
            Environment.Exit(ElapsedTime);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Environment.Exit(-99);
        }
    }
}
