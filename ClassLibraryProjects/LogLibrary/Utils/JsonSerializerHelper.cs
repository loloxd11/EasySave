using System.Text.Json;

public static class JsonSerializerHelper
{
    public static string Serialize<T>(T obj, JsonSerializerOptions options) => JsonSerializer.Serialize(obj, options);
    public static T Deserialize<T>(string json, JsonSerializerOptions options) => JsonSerializer.Deserialize<T>(json, options);
    public static JsonSerializerOptions CreateDefaultOptions() => new JsonSerializerOptions { WriteIndented = false };

}
