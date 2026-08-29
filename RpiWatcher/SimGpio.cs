namespace RpiWatcher;

// Development implementation with no real hardware.
// LED state is written to the log, and input is
// simulated with the Enter key.
// The book works with the real device (RealGpio);
// this is a helper for trying things on the dev machine.
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
