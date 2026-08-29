using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace RpiWatcher;

// Entry point.
// Role: read the options, set up GPIO, and run the
// main loop in a way that can stop safely.
internal static class Program
{
    // Keep the SIGTERM registration alive
    // (it is unregistered when disposed).
    private static readonly List<IDisposable> Signals
        = new();

    private static async Task<int> Main(string[] args)
    {
        Options opt = Options.Parse(args);
        ConfigureCulture(opt);
        ConfigureStdout();
        Log.Level = opt.Verbose
            ? LogLevel.Debug
            : LogLevel.Info;

        Log.Info(Strings.Get("Starting"));

        using var cts = new CancellationTokenSource();
        HookShutdown(cts);

        IGpio gpio = opt.Sim
            ? new SimGpio()
            : new RealGpio(
                opt.LedPin, opt.InputPin, opt.DebounceMs);

        try
        {
            gpio.Start();
        }
        catch (Exception ex)
        {
            Log.Error(Strings.Get("GpioInitFailed"));
            // Always print the real exception to help
            // diagnose the cause on the device.
            Log.Error(ex.Message);
            gpio.Dispose();
            return 1;
        }

        using var watcher =
            new WatcherService(gpio, opt.IntervalMs);
        await watcher.RunAsync(cts.Token);

        Log.Info(Strings.Get("Stopped"));
        return 0;
    }

    // Raise the stop flag on Ctrl+C and SIGTERM
    // (systemctl stop). Cleanup happens in the main loop.
    private static void HookShutdown(
        CancellationTokenSource cts)
    {
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            var reg = PosixSignalRegistration.Create(
                PosixSignal.SIGTERM,
                ctx =>
                {
                    ctx.Cancel = true;
                    cts.Cancel();
                });
            Signals.Add(reg);
        }
        catch
        {
            // Ignore on non-POSIX platforms.
        }
    }

    // Switch the UI language via --lang or an env var.
    private static void ConfigureCulture(Options opt)
    {
        string? lang = opt.Lang
            ?? Environment.GetEnvironmentVariable(
                "RPIWATCHER_LANG");
        if (string.IsNullOrEmpty(lang))
            return;

        try
        {
            var c = new CultureInfo(lang);
            CultureInfo.CurrentUICulture = c;
            CultureInfo.CurrentCulture = c;
        }
        catch
        {
            // Keep the default for an unknown culture.
        }
    }

    // Flush stdout immediately. Under systemd the output
    // is buffered by default and would not reach the
    // journal until the buffer fills.
    // Also force UTF-8 so localized (e.g. Japanese)
    // messages are not garbled on a non-UTF-8 console
    // (Japanese Windows defaults to code page 932).
    private static void ConfigureStdout()
    {
        var utf8 = new UTF8Encoding(false);
        try
        {
            // Switches the console code page to UTF-8
            // so the terminal reads the bytes correctly.
            Console.OutputEncoding = utf8;
        }
        catch
        {
            // Ignore if stdout is redirected in a way
            // that rejects an encoding change.
        }

        var w = new StreamWriter(
            Console.OpenStandardOutput(), utf8)
        {
            AutoFlush = true,
        };
        Console.SetOut(w);
    }
}
