using NHotkey.Wpf;
using System.Windows;
using System.Windows.Input;

namespace WpfCursor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            HotkeyManager.Current.AddOrReplace("start", Key.Z, ModifierKeys.Shift | ModifierKeys.Alt, (_, _) => StartButton_Click(this, new RoutedEventArgs()));
            HotkeyManager.Current.AddOrReplace("stop", Key.X, ModifierKeys.Shift | ModifierKeys.Alt, (_, _) => StopButton_Click(this, new RoutedEventArgs()));
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            CursorManager.Execute();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            CursorManager.Restore();
        }

        private void ArrowButton_Click(object sender, RoutedEventArgs e)
        {
            CursorManager.Arrow();
        }

        private void BusyButton_Click(object sender, RoutedEventArgs e)
        {
            CursorManager.Busy();
        }
    }
}
