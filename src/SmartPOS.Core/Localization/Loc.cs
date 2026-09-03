using SmartPOS.Core.Interfaces;

namespace SmartPOS.Core.Localization;

public static class Loc
{
    private static ILocalizationService? _service;

    public static void Initialize(ILocalizationService service)
    {
        _service = service;
    }

    public static string Tr(string key, string fallback)
    {
        return _service?.GetString(key, fallback) ?? fallback;
    }
}
