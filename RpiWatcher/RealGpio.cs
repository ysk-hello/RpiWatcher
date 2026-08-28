using System.Device.Gpio;

namespace RpiWatcher;

// System.Device.Gpio を使う本番の実装。
// LED を出力、入力ピンを内部プルアップにし、
// GND に落ちた瞬間（Falling）を拾う。
// ボタンは不要。入力ピンと GND を
// ジャンパー線でつなぐだけでよい。
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

    // 別スレッドで呼ばれる点に注意。
    // チャタリング（1回の接触で複数エッジ）を、
    // 直前のエッジから一定時間内は無視して
    // 1回にまとめる（ソフトウェアデバウンス）。
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

    // 後始末: LED を消してピンを解放する。
    // ここを怠ると停止後も点きっぱなしになる。
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
            // すでに解放済みなら無視。
        }

        _controller.Dispose();
        _controller = null;
    }
}
