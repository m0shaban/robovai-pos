using System;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using SmartPOS.Application.Messages;
using SmartPOS.Core.Interfaces;

namespace SmartPOS.WPF.Services;

public class LocalizationService : ILocalizationService
{
    private readonly ISettingsService _settingsService;
    private const string ArabicUri = "Resources/Strings.ar.xaml";
    private const string EnglishUri = "Resources/Strings.en.xaml";

    public string CurrentLanguage { get; private set; } = "ar";
    public bool IsRtl => CurrentLanguage == "ar" || CurrentLanguage == "ur";

    public event EventHandler? LanguageChanged;

    public LocalizationService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Initialize()
    {
        var savedLang = _settingsService.AppLanguage;
        if (string.IsNullOrWhiteSpace(savedLang))
        {
            savedLang = "ar";
        }
        ApplyLanguage(savedLang, false);
    }

    public void SetLanguage(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode)) return;

        var clean = languageCode.Trim().ToLowerInvariant();
        string[] supported = { "ar", "en", "fr", "es", "tr", "ur" };
        if (!supported.Contains(clean))
        {
            clean = "ar";
        }

        if (CurrentLanguage == clean) return;

        ApplyLanguage(clean, true);
    }

    private void ApplyLanguage(string languageCode, bool persist)
    {
        CurrentLanguage = languageCode;

        var targetDictUri = $"Resources/Strings.{languageCode}.xaml";

        try
        {
            var app = System.Windows.Application.Current;
            if (app != null)
            {
                var newDict = new ResourceDictionary
                {
                    Source = new Uri(targetDictUri, UriKind.Relative)
                };

                // Find and replace existing string dictionary or add new
                var existingDict = app.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Strings."));

                if (existingDict != null)
                {
                    var index = app.Resources.MergedDictionaries.IndexOf(existingDict);
                    app.Resources.MergedDictionaries[index] = newDict;
                }
                else
                {
                    app.Resources.MergedDictionaries.Add(newDict);
                }

                // Update all open windows FlowDirection dynamically
                var targetFlow = IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                app.Resources["AppFlowDirection"] = targetFlow;

                foreach (Window win in app.Windows)
                {
                    try
                    {
                        win.FlowDirection = targetFlow;
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalizationService] Error applying language: {ex.Message}");
        }

        if (persist)
        {
            _ = _settingsService.SaveSettingAsync("AppLanguage", languageCode);
        }

        LanguageChanged?.Invoke(this, EventArgs.Empty);

        try
        {
            WeakReferenceMessenger.Default.Send(new LanguageChangedMessage(languageCode));
        }
        catch { }
    }

    public string GetString(string key, string fallback = "")
    {
        if (System.Windows.Application.Current != null && System.Windows.Application.Current.Resources.Contains(key))
        {
            return System.Windows.Application.Current.Resources[key]?.ToString() ?? fallback;
        }
        return fallback;
    }
}
