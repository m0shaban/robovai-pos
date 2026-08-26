using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SmartPOS.Application.Messages;

/// <summary>
/// A global message broadcasted when a barcode is scanned via IBarcodeService.
/// ViewModels can subscribe to this message to react to barcode scans (e.g., add to cart, search, etc.).
/// </summary>
public class BarcodeScannedMessage : ValueChangedMessage<string>
{
    public BarcodeScannedMessage(string barcode) : base(barcode)
    {
    }
}
