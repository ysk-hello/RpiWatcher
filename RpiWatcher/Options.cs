namespace RpiWatcher;

// Command-line options and their defaults.
// Examples:
//   RpiWatcher --led 18 --input 24
//   RpiWatcher --sim --lang en
internal sealed class Options
{
    // GPIO number (BCM) the LED is wired to.
    public int LedPin { get; private set; } = 18;

    // GPIO number (BCM) of the input pin.
    public int InputPin { get; private set; } = 24;

    // Blink interval in milliseconds.
    public int IntervalMs { get; private set; } = 1000;

    // Debounce time in milliseconds.
    // Consecutive inputs within this window are
    // collapsed into one (contact-bounce guard).
    public int DebounceMs { get; private set; } = 200;

    // Run without real hardware (for development).
    public bool Sim { get; private set; }

    // Emit verbose logs.
    public bool Verbose { get; private set; }

    // UI language (e.g. "ja" / "en").
    public string? Lang { get; private set; }

    public static Options Parse(string[] args)
    {
        var o = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--led":
                    o.LedPin = NextInt(args, ref i);
                    break;
                case "--input":
                    o.InputPin = NextInt(args, ref i);
                    break;
                case "--interval":
                    o.IntervalMs =
                        NextInt(args, ref i);
                    break;
                case "--debounce":
                    o.DebounceMs =
                        NextInt(args, ref i);
                    break;
                case "--lang":
                    o.Lang = Next(args, ref i);
                    break;
                case "--sim":
                    o.Sim = true;
                    break;
                case "--verbose":
                    o.Verbose = true;
                    break;
            }
        }
        return o;
    }

    private static string? Next(
        string[] args, ref int i)
    {
        if (i + 1 >= args.Length)
            return null;
        i++;
        return args[i];
    }

    private static int NextInt(
        string[] args, ref int i)
    {
        string? s = Next(args, ref i);
        return int.TryParse(s, out int v) ? v : 0;
    }
}
