public static class LogServiceFactory
{
    public static ILogService CreateLogService(string baseDirectory) => LogManager.GetInstance(baseDirectory);

    public static ILogService CreateLogService(string baseDirectory, Dictionary<string, object> options)
    {
        var config = LogConfiguration.GetInstance();
        foreach (var option in options)
            config.SetOption(option.Key, option.Value);
        return LogManager.GetInstance(baseDirectory);
    }
}
