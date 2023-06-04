public enum LogLevel
{
    NONE, DEBUG, INFO, WARNING, ERROR, FATAL
}

public interface ILogger
{
    void Log(object msg);

    void Info(object msg);

    void Error(object msg);

    void SetLogLevel(LogLevel level);

    LogLevel GetLogLevel();

    void Dispose();
}

public class Logger
{
    private static ILogger instanceLogger;

    public static LogLevel LogLevel
    {
        get
        {
            if (instanceLogger != null) return instanceLogger.GetLogLevel();
            return LogLevel.NONE;
        }
    }

    public static void _setLogger(ILogger logger)
    {
        instanceLogger = logger;
    }

    [System.Diagnostics.Conditional("DEBUG")]
    public static void Log(object msg)
    {
        if (instanceLogger != null)
            instanceLogger.Log(msg);
    }
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Log(object msg, object arg1)
    {
        if (instanceLogger != null)
            instanceLogger.Log(string.Format(msg.ToString(), arg1));
    }
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Log(object msg, object arg1, object arg2)
    {
        if (instanceLogger != null)
            instanceLogger.Log(string.Format(msg.ToString(), arg1, arg2));
    }
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Log(object msg, object arg1, object arg2, object arg3)
    {
        if (instanceLogger != null)
            instanceLogger.Log(string.Format(msg.ToString(), arg1, arg2, arg3));
    }
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Log(object msg, params object[] args)
    {
        if (instanceLogger != null)
            instanceLogger.Log(string.Format(msg.ToString(), args));
    }

    //[System.Diagnostics.Conditional("DEBUG")]
    public static void Info(object msg)
    {
        if (instanceLogger != null)
            instanceLogger.Info(msg);
    }
    //[System.Diagnostics.Conditional("DEBUG")]
    public static void Info(object msg, object arg1)
    {
        if (instanceLogger != null)
            instanceLogger.Info(string.Format(msg.ToString(), arg1));
    }
    //[System.Diagnostics.Conditional("DEBUG")]
    public static void Info(object msg, object arg1, object arg2)
    {
        if (instanceLogger != null)
            instanceLogger.Info(string.Format(msg.ToString(), arg1, arg2));
    }
    //[System.Diagnostics.Conditional("DEBUG")]
    public static void Info(object msg, object arg1, object arg2, object arg3)
    {
        if (instanceLogger != null)
            instanceLogger.Info(string.Format(msg.ToString(), arg1, arg2, arg3));
    }
    //[System.Diagnostics.Conditional("DEBUG")]
    public static void Info(object msg, params object[] args)
    {
        if (instanceLogger != null)
            instanceLogger.Info(string.Format(msg.ToString(), args));
    }

    //[System.Diagnostics.Conditional("DEBUG")]
    public static void Error(object msg)
    {
        if (instanceLogger != null)
            instanceLogger.Error(msg);
    }
    //[System.Diagnostics.Conditional("DEBUG")]
    public static void Error(object msg, object arg1)
    {
        if (instanceLogger != null)
            instanceLogger.Error(string.Format(msg.ToString(), arg1));
    }
    //[System.Diagnostics.Conditional("DEBUG")]
    public static void Error(object msg, object arg1, object arg2)
    {
        if (instanceLogger != null)
            instanceLogger.Error(string.Format(msg.ToString(), arg1, arg2));
    }
    //[System.Diagnostics.Conditional("DEBUG")]
    public static void Error(object msg, object arg1, object arg2, object arg3)
    {
        if (instanceLogger != null)
            instanceLogger.Error(string.Format(msg.ToString(), arg1, arg2, arg3));
    }
    //[System.Diagnostics.Conditional("DEBUG")]
    public static void Error(object msg, params object[] args)
    {
        if (instanceLogger != null)
            instanceLogger.Error(string.Format(msg.ToString(), args));
    }

    public void SetLogLevel(LogLevel level)
    {
        if (instanceLogger != null)
            instanceLogger.SetLogLevel(level);
    }

    public LogLevel GetLogLevel()
    {
        if (instanceLogger != null)
            return instanceLogger.GetLogLevel();

        return LogLevel.NONE;
    }

    public static void Dispose()
    {
        if (instanceLogger != null)
        {
            instanceLogger.Dispose();
            instanceLogger = null;
        }
    }
}
