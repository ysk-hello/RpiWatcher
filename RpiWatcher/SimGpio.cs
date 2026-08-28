namespace RpiWatcher;

// 実機がない開発用の実装。
// LED の状態はログに出し、
// 入力は Enter キーで代用する。
// 本文では実機（RealGpio）を扱う。
// これは母艦だけで動作を試すための補助。
internal sealed class SimGpio : IGpio
{
    private CancellationTokenSource? _cts;
    private Task? _keyLoop;

    public event Action? InputTriggered;

    public void Start()
    {
        Log.Info(Strings.Get("SimMode"));
        _cts = new CancellationTokenSource();
        _keyLoop = Task.Run(
            () => KeyLoop(_cts.Token));
    }

    private void KeyLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            string? line = Console.ReadLine();
            if (line is null)
                break;
            InputTriggered?.Invoke();
        }
    }

    public void SetLed(bool on)
        => Log.Debug(on ? "LED ON" : "LED OFF");

    public void Dispose()
        => _cts?.Cancel();
}
