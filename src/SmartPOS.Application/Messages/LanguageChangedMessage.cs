using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SmartPOS.Application.Messages;

/// <summary>
/// Broadcasted when application UI language is changed (e.g., "ar" or "en").
/// </summary>
public class LanguageChangedMessage : ValueChangedMessage<string>
{
    public LanguageChangedMessage(string language) : base(language)
    {
    }
}
