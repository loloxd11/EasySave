/// <summary>
/// Factory class for creating instances of ILogService.
/// Provides methods to create log services with or without additional configuration options.
/// </summary>
public static class LogServiceFactory
{
    /// <summary>
    /// Creates an instance of ILogService using the specified base directory.
    /// </summary>
    /// <param name="baseDirectory">The base directory where log files will be stored.</param>
    /// <returns>An instance of ILogService.</returns>
    public static ILogService CreateLogService(string baseDirectory) => LogManager.GetInstance(baseDirectory);

    /// <summary>
    /// Creates an instance of ILogService using the specified base directory and configuration options.
    /// </summary>
    /// <param name="baseDirectory">The base directory where log files will be stored.</param>
    /// <param name="options">A dictionary of configuration options to customize the log service.</param>
    /// <returns>An instance of ILogService.</returns>
    public static ILogService CreateLogService(string baseDirectory, Dictionary<string, object> options)
    {
        // Retrieve the singleton instance of the log configuration.
        var config = LogConfiguration.GetInstance();

        // Apply each configuration option to the log configuration.
        foreach (var option in options)
            config.SetOption(option.Key, option.Value);

        // Create and return an instance of ILogService using the configured base directory.
        return LogManager.GetInstance(baseDirectory);
    }
}
