namespace SmartPOS.Core.Interfaces;

public interface ISoundService
{
    void PlayBarcodeBeep();
    void PlayCheckoutSuccess();
    void PlayWarningAlert();
    void PlayClick();
}
