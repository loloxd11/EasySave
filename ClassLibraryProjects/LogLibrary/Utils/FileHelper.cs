public static class FileHelper
{
    /// <summary>
    /// Ensures that the specified directory exists. 
    /// If it does not exist, it creates the directory.
    /// </summary>
    /// <param name="path">The path of the directory to check or create.</param>
    /// <returns>True if the directory was created, false if it already existed.</returns>
    public static bool EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Appends the specified content to a file. 
    /// If the file does not exist, it will be created.
    /// </summary>
    /// <param name="filePath">The path of the file to append to.</param>
    /// <param name="content">The content to append to the file.</param>
    public static void AppendToFile(string filePath, string content)
    {
        File.AppendAllText(filePath, content + "\n");
    }

    /// <summary>
    /// Reads all text from the specified file.
    /// </summary>
    /// <param name="filePath">The path of the file to read.</param>
    /// <returns>The content of the file as a string.</returns>
    public static string ReadAllText(string filePath) => File.ReadAllText(filePath);

    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="filePath">The path of the file to check.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    public static bool FileExists(string filePath) => File.Exists(filePath);
}
