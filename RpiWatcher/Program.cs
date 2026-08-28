using System.Globalization;
using System.Runtime.InteropServices;

namespace RpiWatcher;

// エントリポイント。
// 役割: 設定を読み、GPIO を用意し、
// 安全に停止できる形で本体を回す。
internal static class Program
{
    // SIGTERM 登録を保持する
    // （破棄されると解除されるため）。
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
            // 実機での切り分け用に、実際の例外を常に出す。
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

    // Ctrl+C と SIGTERM（systemctl stop）で
    // 停止フラグを立てる。後始末は本体側で行う。
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
            // POSIX 以外では無視する。
        }
    }

    // --lang か環境変数で UI 言語を切り替える。
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
            // 未知のカルチャは既定のまま。
        }
    }

    // 標準出力を即時フラッシュにする。
    // systemd 経由だと既定では
    // バッファされ、journal に出ないため。
    private static void ConfigureStdout()
    {
        var w = new StreamWriter(
            Console.OpenStandardOutput())
        {
            AutoFlush = true,
        };
        Console.SetOut(w);
    }
}
