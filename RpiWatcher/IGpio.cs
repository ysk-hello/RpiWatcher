namespace RpiWatcher;

// Minimal surface with just LED output and an input event.
// Lets the real device (RealGpio) and the development
// stub (SimGpio) be treated the same way.
internal interface IGpio : IDisposable
{
    // Fires when an input is detected.
    // Note: on the real device this is raised on a
    // separate thread.
    event Action? InputTriggered;

    // Open the pins. Throws if it fails on the device.
    void Start();

    // Turn the LED on / off.
    void SetLed(bool on);
}
