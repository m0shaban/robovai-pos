using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SmartPOS.Application.Messages;

/// <summary>
/// Broadcasted when window mode (Fullscreen, Maximized, Windowed) is changed in settings.
/// </summary>
public class WindowModeChangedMessage : ValueChangedMessage<string>
{
    public WindowModeChangedMessage(string mode) : base(mode)
    {
    }
}
