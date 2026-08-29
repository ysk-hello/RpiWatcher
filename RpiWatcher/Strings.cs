using System.Globalization;
using System.Resources;

namespace RpiWatcher;

// Gateway to the resource strings.
// Messages are not hard-coded; they live in
// Resources/*.resx and switch by culture (ja / en).
// The English build just adds en.resx here, no changes.
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
