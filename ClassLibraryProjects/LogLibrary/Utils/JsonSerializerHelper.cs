using System.Text.Json;

public static class JsonSerializerHelper
{
    /// <summary>
    /// Serializes an object of type T into a JSON string using the provided JsonSerializerOptions.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="options">The serialization options to use.</param>
    /// <returns>A JSON string representation of the object.</returns>
    public static string Serialize<T>(T obj, JsonSerializerOptions options) => JsonSerializer.Serialize(obj, options);

    /// <summary>
    /// Deserializes a JSON string into an object of type T using the provided JsonSerializerOptions.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize into.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">The deserialization options to use.</param>
    /// <returns>An object of type T deserialized from the JSON string.</returns>
    public static T Deserialize<T>(string json, JsonSerializerOptions options) => JsonSerializer.Deserialize<T>(json, options)!;

    /// <summary>
    /// Creates a default instance of JsonSerializerOptions with predefined settings.
    /// </summary>
    /// <returns>A JsonSerializerOptions instance with WriteIndented set to false.</returns>
    public static JsonSerializerOptions CreateDefaultOptions() => new JsonSerializerOptions { WriteIndented = false };
}
