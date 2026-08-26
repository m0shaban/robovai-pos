using System;
using System.Media;
using System.Threading.Tasks;
using SmartPOS.Core.Interfaces;

namespace SmartPOS.WPF.Services;

public class SoundService : ISoundService
{
    private readonly ISettingsService _settingsService;

    public SoundService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void PlayBarcodeBeep()
    {
        Task.Run(() =>
        {
            try
            {
                // High crisp short beep (1800Hz, 45ms)
                Console.Beep(1800, 45);
            }
            catch
            {
                try { SystemSounds.Beep.Play(); } catch { }
            }
        });
    }

    public void PlayCheckoutSuccess()
    {
        Task.Run(() =>
        {
            try
            {
                // Pleasant rising chord (523Hz C5 -> 659Hz E5 -> 784Hz G5)
                Console.Beep(523, 60);
                Console.Beep(659, 60);
                Console.Beep(784, 100);
            }
            catch
            {
                try { SystemSounds.Asterisk.Play(); } catch { }
            }
        });
    }

    public void PlayWarningAlert()
    {
        Task.Run(() =>
        {
            try
            {
                // Warning low double-beep (400Hz, 80ms)
                Console.Beep(400, 80);
                Console.Beep(350, 100);
            }
            catch
            {
                try { SystemSounds.Exclamation.Play(); } catch { }
            }
        });
    }

    public void PlayClick()
    {
        Task.Run(() =>
        {
            try
            {
                Console.Beep(1200, 20);
            }
            catch { }
        });
    }
}
