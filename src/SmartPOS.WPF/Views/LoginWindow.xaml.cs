using SmartPOS.Application.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartPOS.WPF.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.RequestClose += ViewModel_RequestClose;
            Loaded += LoginWindow_Loaded;
            Closed += LoginWindow_Closed;
        }

        private static readonly string[] _tipKeys = new[]
        {
            "Loc_Tip_1", "Loc_Tip_2", "Loc_Tip_3", "Loc_Tip_4",
            "Loc_Tip_5", "Loc_Tip_6", "Loc_Tip_7", "Loc_Tip_8",
            "Loc_Tip_9", "Loc_Tip_10", "Loc_Tip_11"
        };

        private static readonly string[] _fallbackTips = new[]
        {
            "قم بإغلاق الوردية يومياً للحفاظ على دقة الحسابات والمبيعات.",
            "يمكنك استخدام الباركود لتسريع عملية البيع في شاشة الكاشير.",
            "مراجعة تقرير النواقص أسبوعياً يجنبك نفاذ البضاعة.",
            "استخدم صلاحيات المستخدمين لحماية بيانات متجرك.",
            "عمل نسخة احتياطية من البيانات يضمن لك عدم فقدان أي معلومات هامة.",
            "تابع قسم المصروفات بدقة لتعرف صافي أرباحك الحقيقية.",
            "يمكنك التبديل بين وضع الشبكة والوضع المحلي بضغطة زر.",
            "قال رسول الله ﷺ: «البيعان بالخيار ما لم يتفرقا، فإن صدقا وبينا بورك لهما في بيعهما» - تقواك في البيع تجلب البركة.",
            "تذكر أن الأمانة في المعاملات هي رأس مال التاجر الناجح، أخلص النية لله.",
            "ابتسم للعميل وتذكر أن «تبسمك في وجه أخيك صدقة».",
            "تعامل مع بيانات العملاء والمحل بأمانة، فهي أمانة بين يديك ستحاسب عليها."
        };

        private int _currentTipIndex = 0;
        private SmartPOS.WPF.Services.LocalizationService? _locService;

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtUsername.Focus();
            Keyboard.Focus(txtUsername);
            txtUsername.SelectAll();

            _locService = (System.Windows.Application.Current as App)?.Host.Services.GetService(typeof(SmartPOS.Core.Interfaces.ILocalizationService)) as SmartPOS.WPF.Services.LocalizationService;
            if (_locService != null)
            {
                _locService.LanguageChanged += LocService_LanguageChanged;
                // Set initial combo selection
                if (CmbLoginLanguage != null)
                {
                    foreach (ComboBoxItem item in CmbLoginLanguage.Items)
                    {
                        if (item.Tag is string code && code.Equals(_locService.CurrentLanguage, StringComparison.OrdinalIgnoreCase))
                        {
                            CmbLoginLanguage.SelectedItem = item;
                            break;
                        }
                    }
                }
            }

            // Set random tip
            ShowRandomTip();
        }

        private void LocService_LanguageChanged(object? sender, EventArgs e)
        {
            RefreshTipText();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
            {
                _locService ??= (System.Windows.Application.Current as App)?.Host.Services.GetService(typeof(SmartPOS.Core.Interfaces.ILocalizationService)) as SmartPOS.WPF.Services.LocalizationService;
                _locService?.SetLanguage(langCode);
            }
        }

        private void ShowRandomTip()
        {
            if (txtRandomTip != null)
            {
                Random rng = new Random();
                _currentTipIndex = rng.Next(_tipKeys.Length);
                RefreshTipText();
            }
        }

        private void RefreshTipText()
        {
            if (txtRandomTip != null && _currentTipIndex >= 0 && _currentTipIndex < _tipKeys.Length)
            {
                var key = _tipKeys[_currentTipIndex];
                var fallback = _fallbackTips[_currentTipIndex];
                txtRandomTip.Text = SmartPOS.Core.Localization.Loc.Tr(key, fallback);
            }
        }

        private void NextTipButton_Click(object sender, RoutedEventArgs e)
        {
            ShowRandomTip();
        }

        private void LoginWindow_Closed(object? sender, EventArgs e)
        {
            if (_locService != null)
            {
                _locService.LanguageChanged -= LocService_LanguageChanged;
            }
            _viewModel.RequestClose -= ViewModel_RequestClose;
            Loaded -= LoginWindow_Loaded;
            Closed -= LoginWindow_Closed;
        }

        private void ViewModel_RequestClose()
        {
            DialogResult = true;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
