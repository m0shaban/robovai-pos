using SmartPOS.Core.Interfaces;
using System.IO.Ports;

namespace SmartPOS.Infrastructure.Services;

/// <summary>
/// Barcode scanner service.
/// Supports HID (USB keyboard emulation) and Serial (COM port) modes.
/// Timeout is configurable so slow scanners also work.
/// </summary>
public class BarcodeService : IBarcodeService
{
    private bool _isListening;
    private string _barcodeBuffer = string.Empty;
    private DateTime _lastKeyTime = DateTime.Now;

    // Configurable from settings (call Configure before StartListening)
    private int _timeoutMs = 100;
    private string _mode = "HID"; // "HID" | "Serial"

    private SerialPort? _serialPort;

    public event EventHandler<string>? BarcodeScanned;
    public bool IsListening => _isListening;

    // ─── Configuration ────────────────────────────────────────────────────────
    /// <summary>Call once after loading settings, before StartListening.</summary>
    public void Configure(string mode, string comPort, int baudRate, int timeoutMs)
    {
        _mode      = mode;
        _timeoutMs = timeoutMs > 0 ? timeoutMs : 100;

        // Close any existing serial port
        CloseSerial();

        if (_mode.Equals("Serial", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                _serialPort = new SerialPort(comPort, baudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout  = 500,
                    WriteTimeout = 500
                };
            }
            catch { /* invalid port – will fall back to HID */ }
        }
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────
    public void StartListening()
    {
        _isListening = true;

        if (_mode.Equals("Serial", StringComparison.OrdinalIgnoreCase) && _serialPort != null)
        {
            try
            {
                if (!_serialPort.IsOpen) _serialPort.Open();
                _serialPort.DataReceived += SerialPort_DataReceived;
            }
            catch { /* fall back silently to HID */ }
        }
    }

    public void StopListening()
    {
        _isListening = false;
        _barcodeBuffer = string.Empty;
        CloseSerial();
    }

    private void CloseSerial()
    {
        if (_serialPort != null)
        {
            try
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
                if (_serialPort.IsOpen) _serialPort.Close();
            }
            catch { }
        }
    }

    private bool _isFastStream = false;

    // ─── HID (Keyboard Emulation) ─────────────────────────────────────────────
    /// <summary>Call this from MainWindow.PreviewKeyDown/PreviewTextInput for HID scanners.</summary>
    public bool ProcessKeyInput(string key)
    {
        if (!_isListening) return false;

        var now = DateTime.Now;
        var diff = (now - _lastKeyTime).TotalMilliseconds;
        _lastKeyTime = now;

        if (diff > _timeoutMs)
        {
            _barcodeBuffer = string.Empty;
            _isFastStream = false;
        }
        else if (diff < 65) // Keystroke interval < 65ms indicates hardware scanner burst
        {
            _isFastStream = true;
        }

        if (key == "\r" || key == "\n")
        {
            if (!string.IsNullOrWhiteSpace(_barcodeBuffer) && _barcodeBuffer.Length >= 3)
            {
                var scanned = _barcodeBuffer.Trim();
                _barcodeBuffer = string.Empty;
                var wasFast = _isFastStream;
                _isFastStream = false;
                BarcodeScanned?.Invoke(this, scanned);
                return true;
            }
            _barcodeBuffer = string.Empty;
            _isFastStream = false;
            return false;
        }
        else
        {
            _barcodeBuffer += key;
            return _isFastStream;
        }
    }

    // ─── Serial (COM Port) ────────────────────────────────────────────────────
    private string _serialBuffer = string.Empty;

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (!_isListening || _serialPort == null) return;
        try
        {
            _serialBuffer += _serialPort.ReadExisting();
            int idx;
            while ((idx = _serialBuffer.IndexOf('\r')) >= 0 ||
                   (idx = _serialBuffer.IndexOf('\n')) >= 0)
            {
                var barcode = _serialBuffer[..idx].Trim();
                _serialBuffer = _serialBuffer[(idx + 1)..];
                if (!string.IsNullOrWhiteSpace(barcode))
                    BarcodeScanned?.Invoke(this, barcode);
            }
        }
        catch { }
    }
}
