namespace RpiWatcher;

internal enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
}

// 最小のログ。標準出力に1行ずつ書く。
// 実機では systemd がこれを journal に集める
// （journalctl -u rpiwatcher で読める）。
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
