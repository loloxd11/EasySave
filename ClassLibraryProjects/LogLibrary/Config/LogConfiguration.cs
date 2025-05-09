using System.Text.Json;

/// <summary>
/// Singleton class to manage logging configuration options.
/// Provides methods to set, retrieve, load, and save configuration options.
/// </summary>
public class LogConfiguration
{
    // Dictionary to store configuration options as key-value pairs.
    private Dictionary<string, object> configOptions = new();

    // Singleton instance of the LogConfiguration class.
    private static LogConfiguration? instance;

    // Private constructor to prevent direct instantiation.
    private LogConfiguration() { }

    /// <summary>
    /// Retrieves the singleton instance of the LogConfiguration class.
    /// If the instance does not exist, it is created.
    /// </summary>
    /// <returns>The singleton instance of LogConfiguration.</returns>
    public static LogConfiguration GetInstance() => instance ??= new LogConfiguration();

    /// <summary>
    /// Sets a configuration option with the specified key and value.
    /// If the key already exists, its value is updated.
    /// </summary>
    /// <param name="key">The key of the configuration option.</param>
    /// <param name="value">The value of the configuration option.</param>
    public void SetOption(string key, object value) => configOptions[key] = value;

    /// <summary>
    /// Retrieves the value of a configuration option by its key.
    /// If the key does not exist or the value cannot be cast to the specified type, the default value is returned.
    /// </summary>
    /// <typeparam name="T">The expected type of the configuration value.</typeparam>
    /// <param name="key">The key of the configuration option.</param>
    /// <param name="defaultValue">The default value to return if the key is not found or the type does not match.</param>
    /// <returns>The value of the configuration option or the default value.</returns>
    public T GetOption<T>(string key, T defaultValue) =>
        configOptions.TryGetValue(key, out var value) && value is T typedValue ? typedValue : defaultValue;

    /// <summary>
    /// Loads configuration options from a JSON file.
    /// If the file does not exist, the method returns false.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <returns>True if the file was successfully loaded; otherwise, false.</returns>
    public bool LoadFromFile(string configPath)
    {
        if (!File.Exists(configPath)) return false;
        var json = File.ReadAllText(configPath);
        configOptions = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
        return true;
    }

    /// <summary>
    /// Saves the current configuration options to a JSON file.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <returns>True if the file was successfully saved.</returns>
    public bool SaveToFile(string configPath)
    {
        var json = JsonSerializer.Serialize(configOptions);
        File.WriteAllText(configPath, json);
        return true;
    }
}
