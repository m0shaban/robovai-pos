namespace SmartPOS.Core.Interfaces;

public interface INotificationService
{
    void ShowSuccess(string message, string title = "نجح");
    void ShowError(string message, string title = "خطأ");
    void ShowWarning(string message, string title = "تنبيه");
    void ShowInfo(string message, string title = "معلومة");
    bool Confirm(string message, string title = "تأكيد");
}
