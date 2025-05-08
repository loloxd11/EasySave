public static class FileHelper
{
    public static bool EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            return true;
        }
        return false;
    }

    public static void AppendToFile(string filePath, string content)
    {
        File.AppendAllText(filePath, content + "\n");
    }

    public static string ReadAllText(string filePath) => File.ReadAllText(filePath);
    public static bool FileExists(string filePath) => File.Exists(filePath);
}
