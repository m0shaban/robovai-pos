using System;
using System.Windows;
using SmartPOS.Application.ViewModels;

namespace SmartPOS.WPF.Views
{
    public partial class NumpadWindow : Window
    {
        public NumpadWindow()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Keep window on top of the POS page owner
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Notify ViewModel that the window was closed (e.g. user clicked X)
            if (DataContext is MainPOSViewModel vm && vm.IsTouchNumpadVisible)
            {
                vm.IsTouchNumpadVisible = false;
            }
        }
    }
}
