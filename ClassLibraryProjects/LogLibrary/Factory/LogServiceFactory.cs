// ClassLibraryProjects/LogLibrary/Factory/LogServiceFactory.cs
public static class LogServiceFactory
{
    public static ILogService CreateLogService(string baseDirectory) => LogManager.GetInstance(baseDirectory);

    public static ILogService CreateLogService(string baseDirectory, Dictionary<string, object> options)
    {
        var config = LogConfiguration.GetInstance();

        foreach (var option in options)
            config.SetOption(option.Key, option.Value);

        // Get the log format from options, if provided
        string logFormat = options.ContainsKey("LogFormat") ? options["LogFormat"].ToString().ToUpper() : "JSON";
        config.LogFormat = logFormat;

        return LogManager.GetInstance(baseDirectory, logFormat);
    }
}