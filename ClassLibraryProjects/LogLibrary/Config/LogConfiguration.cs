using System.Text.Json;

// ClassLibraryProjects/LogLibrary/Config/LogConfiguration.cs
public class LogConfiguration
{
    private Dictionary<string, object> configOptions = new();
    private static LogConfiguration? instance;
    private LogConfiguration() { }
    public static LogConfiguration GetInstance() => instance ??= new LogConfiguration();

    public void SetOption(string key, object value) => configOptions[key] = value;

    public T GetOption<T>(string key, T defaultValue) =>
        configOptions.TryGetValue(key, out var value) && value is T typedValue ? typedValue : defaultValue;

    public bool LoadFromFile(string configPath)
    {
        if (!File.Exists(configPath)) return false;
        var json = File.ReadAllText(configPath);
        configOptions = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
        return true;
    }

    public bool SaveToFile(string configPath)
    {
        var json = JsonSerializer.Serialize(configOptions);
        File.WriteAllText(configPath, json);
        return true;
    }

    // Add this property
    public string LogFormat { get; set; } = "JSON"; // Default to JSON
}
