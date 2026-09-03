using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace SmartPOS.Application.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "جاهز";

    /// <summary>
    /// Executes a long-running task while managing the IsLoading and StatusMessage state.
    /// Also provides central error handling.
    /// </summary>
    /// <param name="action">The async action to execute.</param>
    /// <param name="loadingMessage">Optional message to display while loading.</param>
    /// <param name="successMessage">Optional message to display upon successful completion.</param>
    protected async Task ExecuteBusyAsync(Func<Task> action, string? loadingMessage = null, string? successMessage = null)
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            if (!string.IsNullOrWhiteSpace(loadingMessage))
            {
                StatusMessage = loadingMessage;
            }

            await action();

            if (!string.IsNullOrWhiteSpace(successMessage))
            {
                StatusMessage = successMessage;
                // Optional: Show success dialog if needed globally, but usually just update status
            }
            else if (!string.IsNullOrWhiteSpace(loadingMessage))
            {
                StatusMessage = SmartPOS.Core.Localization.Loc.Tr("Loc_Ready", "جاهز");
            }
        }
        catch (Exception ex)
        {
            var errTitle = SmartPOS.Core.Localization.Loc.Tr("Loc_Error", "خطأ");
            StatusMessage = $"❌ {errTitle}: {ex.Message}";
            MessageBox.Show(ex.Message, errTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
