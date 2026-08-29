using System.Device.Gpio;

namespace RpiWatcher;

// Production implementation backed by System.Device.Gpio.
// Drives the LED as output, sets the input pin to
// internal pull-up, and catches the moment it is pulled
// to GND (the falling edge). A push button pressing the
// pin to GND triggers it; a jumper wire works too.
internal sealed class RealGpio : IGpio
{
    private readonly int _ledPin;
    private readonly int _inputPin;
    private readonly int _debounceMs;
    private readonly object _gate = new();
    private DateTime _lastEdge = DateTime.MinValue;
    private GpioController? _controller;

    public event Action? InputTriggered;

    public RealGpio(
        int ledPin, int inputPin, int debounceMs)
    {
        _ledPin = ledPin;
        _inputPin = inputPin;
        _debounceMs = debounceMs;
    }

    public void Start()
    {
        _controller = new GpioController();

        _controller.OpenPin(
            _ledPin, PinMode.Output);
        _controller.OpenPin(
            _inputPin, PinMode.InputPullUp);

        _controller
            .RegisterCallbackForPinValueChangedEvent(
                _inputPin,
                PinEventTypes.Falling,
                OnPinFalling);
    }

    // Note: this is invoked on a separate thread.
    // Collapses contact bounce (multiple edges from one
    // press) into one by ignoring edges that arrive
    // within a set window of the previous edge
    // (software debounce).
    private void OnPinFalling(
        object sender,
        PinValueChangedEventArgs e)
    {
        DateTime now = DateTime.UtcNow;
        lock (_gate)
        {
            double ms =
                (now - _lastEdge).TotalMilliseconds;
            if (ms < _debounceMs)
                return;
            _lastEdge = now;
        }
        InputTriggered?.Invoke();
    }

    public void SetLed(bool on)
    {
        _controller?.Write(
            _ledPin,
            on ? PinValue.High : PinValue.Low);
    }

    // Cleanup: turn the LED off and release the pins.
    // Skip this and the LED stays lit after the app stops.
    public void Dispose()
    {
        if (_controller is null)
            return;

        try
        {
            _controller.Write(
                _ledPin, PinValue.Low);
        }
        catch
        {
            // Ignore if it was already released.
        }

        _controller.Dispose();
        _controller = null;
    }
}
