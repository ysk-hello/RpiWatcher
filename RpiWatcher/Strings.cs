using System.Globalization;
using System.Resources;

namespace RpiWatcher;

// リソース文字列の窓口。
// 文言はハードコードせず Resources/*.resx に置き、
// カルチャ（ja / en）で切り替える。
// 英語版はここを差し替えず en.resx を足すだけ。
internal static class Strings
{
    private static readonly ResourceManager Rm =
        new(
            "RpiWatcher.Resources.Strings",
            typeof(Strings).Assembly);

    public static string Get(string key)
        => Rm.GetString(
               key, CultureInfo.CurrentUICulture)
           ?? key;
}
