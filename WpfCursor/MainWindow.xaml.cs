using NHotkey.Wpf;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace WpfCursor
{
    public partial class MainWindow : Window
    {
        private CursorManager _cursorManager;

        public MainWindow()
        {
            InitializeComponent();
            HotkeyManager.Current.AddOrReplace("start", Key.Z, ModifierKeys.Shift | ModifierKeys.Alt, (_, _) => StartButton_Click(this, new RoutedEventArgs()));
            HotkeyManager.Current.AddOrReplace("stop", Key.X, ModifierKeys.Shift | ModifierKeys.Alt, (_, _) => StopButton_Click(this, new RoutedEventArgs()));

            _cursorManager = new CursorManager();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _cursorManager.Execute();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _cursorManager.Restore();
        }

        private void ArrowButton_Click(object sender, RoutedEventArgs e)
        {
            _cursorManager.Arrow();
        }

        private void BusyButton_Click(object sender, RoutedEventArgs e)
        {
            _cursorManager.Busy();
        }
    }

    public class CursorManager
    {
        private const uint OCR_NORMAL = 32512;
        private const uint OCR_IBEAM = 32513;
        private const nint DEFAULT_CURSOR = 0x0000000000010003;
        private const nint BEAM_CURSOR = 0x0000000000010005;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadCursorFromFile(string fileName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetSystemCursor(IntPtr hCursor, uint id);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyCursor(IntPtr hCursor);

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public System.Drawing.Point ptScreenPos;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        private IntPtr _customCursor;
        private IntPtr _originalDefaultCursor;
        private IntPtr _originalBeamCursor;
        private bool _isExecuting;

        public CursorManager()
        {
            _isExecuting = false;
        }

        public void Arrow()
        {
            var cusor = CopyIcon(LoadCursorFromFile(@"C:\Windows\Cursors\aero_arrow.cur"));
            SetSystemCursor(cusor, OCR_NORMAL);
        }
        public void Busy()
        {
            var cusor = CopyIcon(LoadCursorFromFile(@"C:\Windows\Cursors\aero_busy.ani"));
            SetSystemCursor(cusor, OCR_NORMAL);
        }

        public void Execute()
        {
            if (_isExecuting) return;

            _isExecuting = true;

            _originalDefaultCursor = CopyIcon(DEFAULT_CURSOR);
            _originalBeamCursor = CopyIcon(BEAM_CURSOR);

            string tempFilePath = Path.GetTempFileName();
            System.Diagnostics.Debug.WriteLine($"tmp: {tempFilePath}");
            using (Stream resourceStream = Application.GetResourceStream(new Uri("pack://application:,,,/Working_32x.ani")).Stream)
            {
                using FileStream fileStream = new(tempFilePath, FileMode.Create);
                resourceStream.CopyTo(fileStream);
            }
            _customCursor = LoadCursorFromFile(tempFilePath);
            SetSystemCursor(_customCursor, OCR_NORMAL);
            _customCursor = LoadCursorFromFile(tempFilePath);
            SetSystemCursor(_customCursor, OCR_IBEAM);
            File.Delete(tempFilePath);
        }

        public void Restore()
        {
            if (!_isExecuting) return;

            _isExecuting = false;

            // Restore original cursor
            if (_originalDefaultCursor != IntPtr.Zero)
            {
                SetSystemCursor(_originalDefaultCursor, OCR_NORMAL);
                DestroyCursor(_originalDefaultCursor);
            }
            if (_originalBeamCursor != IntPtr.Zero)
            {
                SetSystemCursor(_originalBeamCursor, OCR_IBEAM);
                DestroyCursor(_originalBeamCursor);
            }

            // Destroy the custom cursor
            if (_customCursor != IntPtr.Zero)
            {
                DestroyCursor(_customCursor);
            }
        }
    }
}
