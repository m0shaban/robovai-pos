using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Application.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartPOS.WPF.Views;

public partial class POSPage : Page
{
    private readonly MainPOSViewModel _viewModel;
    private NumpadWindow? _numpadWindow;

    public POSPage()
    {
        InitializeComponent();

        // Get ViewModel from DI
        _viewModel = ((App)System.Windows.Application.Current).Host.Services.GetRequiredService<MainPOSViewModel>();
        DataContext = _viewModel;

        Unloaded += Page_Unloaded;

        // Watch ViewModel property to open/close numpad window
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        BarcodeTextBox.Focus();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        CloseNumpadWindow();

        if (_viewModel is IDisposable disposable)
            disposable.Dispose();
    }

    // ── Sync numpad Window with ViewModel.IsTouchNumpadVisible ──────────────
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainPOSViewModel.IsTouchNumpadVisible))
        {
            if (_viewModel.IsTouchNumpadVisible)
                OpenNumpadWindow();
            else
                CloseNumpadWindow();
        }
    }

    private void OpenNumpadWindow()
    {
        if (_numpadWindow != null && _numpadWindow.IsLoaded)
        {
            _numpadWindow.Activate();
            return;
        }

        // Position near the right-side of the main window
        var mainWindow = Window.GetWindow(this);
        double left = mainWindow != null ? mainWindow.Left + mainWindow.ActualWidth - 380 : 200;
        double top  = mainWindow != null ? mainWindow.Top  + 100 : 100;

        _numpadWindow = new NumpadWindow
        {
            DataContext = _viewModel,
            Owner       = mainWindow,
            Left        = left,
            Top         = top
        };

        _numpadWindow.Closed += NumpadWindow_Closed;
        _numpadWindow.Show();
    }

    private void CloseNumpadWindow()
    {
        if (_numpadWindow != null)
        {
            _numpadWindow.Closed -= NumpadWindow_Closed;
            if (_numpadWindow.IsLoaded)
                _numpadWindow.Close();
            _numpadWindow = null;
        }
    }

    private void NumpadWindow_Closed(object? sender, EventArgs e)
    {
        _numpadWindow = null;
        // Keep ViewModel in sync when user closes the window via X button
        if (_viewModel.IsTouchNumpadVisible)
            _viewModel.IsTouchNumpadVisible = false;
    }

    // ── Keyboard shortcuts ───────────────────────────────────────────────────
    private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F1:
                BarcodeTextBox.Focus();
                BarcodeTextBox.SelectAll();
                e.Handled = true;
                break;

            case Key.F2:
                if (CartListView.SelectedItem != null)
                    _viewModel.IncreaseQuantityCommand.Execute(CartListView.SelectedItem);
                e.Handled = true;
                break;

            case Key.F3:
                if (CartListView.SelectedItem != null)
                    _viewModel.DecreaseQuantityCommand.Execute(CartListView.SelectedItem);
                e.Handled = true;
                break;

            case Key.F4:
                if (CartListView.SelectedItem != null)
                    _viewModel.RemoveItemCommand.Execute(CartListView.SelectedItem);
                e.Handled = true;
                break;

            case Key.F5:
                _viewModel.ApplyDiscountCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.F7:
                _viewModel.HoldSaleCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.F8:
                _viewModel.OpenCashDrawerCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.F9:
                _ = _viewModel.SubmitOrderCommand.ExecuteAsync(null);
                e.Handled = true;
                break;

            case Key.F12:
                _ = _viewModel.SubmitOrderCommand.ExecuteAsync(null);
                e.Handled = true;
                break;

            case Key.F10:
                _viewModel.ClearCartCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    // ── Legacy canvas drag handlers (kept for safety - no-op now) ───────────
    private void NumpadHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { }
    private void NumpadHeader_MouseMove(object sender, MouseEventArgs e) { }
    private void NumpadHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }
}
