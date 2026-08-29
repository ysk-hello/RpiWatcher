namespace RpiWatcher;

// The app itself (the watcher lamp).
// Blinks the LED at a fixed interval and logs
// every input it detects.
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

    // Input handler.
    // In remote debugging, set a breakpoint on this line
    // and inspect count.
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
