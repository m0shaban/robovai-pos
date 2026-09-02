using System;

namespace SmartPOS.Core.Interfaces;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    bool IsRtl { get; }
    event EventHandler? LanguageChanged;
    void SetLanguage(string languageCode);
    string GetString(string key, string fallback = "");
}
