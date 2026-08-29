namespace RpiWatcher;

internal enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
}

// Minimal logger. Writes one line at a time to stdout.
// On the device, systemd collects these into the journal
// (read them with journalctl -u rpiwatcher).
internal static class Log
{
    public static LogLevel Level = LogLevel.Info;

    public static void Debug(string m)
        => Write(LogLevel.Debug, m);

    public static void Info(string m)
        => Write(LogLevel.Info, m);

    public static void Warn(string m)
        => Write(LogLevel.Warn, m);

    public static void Error(string m)
        => Write(LogLevel.Error, m);

    private static void Write(
        LogLevel level, string message)
    {
        if (level < Level)
            return;

        string ts = DateTime.Now
            .ToString("HH:mm:ss");
        Console.WriteLine(
            $"{ts} [{level,-5}] {message}");
    }
}
