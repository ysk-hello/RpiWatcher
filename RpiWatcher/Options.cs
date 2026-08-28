namespace RpiWatcher;

// コマンドライン引数と既定値。
// 例:
//   RpiWatcher --led 18 --input 24
//   RpiWatcher --sim --lang en
internal sealed class Options
{
    // LED をつなぐ GPIO 番号（BCM）。
    public int LedPin { get; private set; } = 18;

    // 入力ピンの GPIO 番号（BCM）。
    public int InputPin { get; private set; } = 24;

    // 点滅の間隔（ミリ秒）。
    public int IntervalMs { get; private set; } = 1000;

    // デバウンス時間（ミリ秒）。
    // この時間内の連続した入力は1回にまとめる
    // （チャタリング対策）。
    public int DebounceMs { get; private set; } = 200;

    // 実機を使わず動かす（開発用）。
    public bool Sim { get; private set; }

    // 詳細ログを出す。
    public bool Verbose { get; private set; }

    // UI 言語（例: "ja" / "en"）。
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
