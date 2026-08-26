namespace SmartPOS.Core.Interfaces;

/// <summary>
/// Barcode scanner service.
/// Supports HID (USB keyboard emulation) and Serial (COM port) modes.
/// </summary>
public interface IBarcodeService
{
    event EventHandler<string>? BarcodeScanned;

    bool IsListening { get; }

    /// <summary>Configure before calling StartListening.</summary>
    void Configure(string mode, string comPort, int baudRate, int timeoutMs);

    void StartListening();
    void StopListening();

    /// <summary>Feed one keystroke for HID mode detection. Returns true if fast scanner stream.</summary>
    bool ProcessKeyInput(string key);
}
