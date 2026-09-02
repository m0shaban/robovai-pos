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

        private readonly string[] _tips = new[]
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

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtUsername.Focus();
            Keyboard.Focus(txtUsername);
            txtUsername.SelectAll();

            // Set random tip
            ShowRandomTip();
        }

        private void ShowRandomTip()
        {
            if (txtRandomTip != null)
            {
                Random rng = new Random();
                int index = rng.Next(_tips.Length);
                txtRandomTip.Text = _tips[index];
            }
        }

        private void NextTipButton_Click(object sender, RoutedEventArgs e)
        {
            ShowRandomTip();
        }

        private void LoginWindow_Closed(object? sender, EventArgs e)
        {
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
