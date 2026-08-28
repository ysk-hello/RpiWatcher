namespace RpiWatcher;

// アプリ本体（見張りランプ）。
// LED を一定間隔で点滅させ、
// 入力を検知したらログに残す。
internal sealed class WatcherService : IDisposable
{
    private readonly IGpio _gpio;
    private readonly int _intervalMs;
    private int _count;

    public WatcherService(IGpio gpio, int intervalMs)
    {
        _gpio = gpio;
        _intervalMs = intervalMs;
        _gpio.InputTriggered += OnInput;
    }

    public async Task RunAsync(CancellationToken token)
    {
        Log.Info(Strings.Get("Ready"));

        bool on = false;
        while (!token.IsCancellationRequested)
        {
            on = !on;
            _gpio.SetLed(on);
            try
            {
                await Task.Delay(_intervalMs, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        Log.Info(Strings.Get("Stopping"));
    }

    // 入力ハンドラ。
    // リモートデバッグでは、この行に
    // ブレークポイントを張って count を見る。
    private void OnInput()
    {
        _count++;
        int count = _count;
        Log.Info(string.Format(
            Strings.Get("InputDetected"), count));
    }

    public void Dispose()
    {
        _gpio.InputTriggered -= OnInput;
        _gpio.Dispose();
    }
}
