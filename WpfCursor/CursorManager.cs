using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace WpfCursor;


public class CursorManager
{
    private const uint OCR_NORMAL = 32512;
    private const nint NORMAL_CURSOR = 0x0000000000010003;

    private const uint OCR_IBEAM = 32513;
    private const nint BEAM_CURSOR = 0x0000000000010005;

    private static bool _isExecuting;
    private static IntPtr _originalNormalCursor;
    private static IntPtr _originalBeamCursor;
    private static IntPtr _customNormalCursor;
    private static IntPtr _customBeamCursor;
    private static IntPtr _errorNormalCursor;
    private static IntPtr _errorBeamCursor;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr LoadCursorFromFile(string fileName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool SetSystemCursor(IntPtr hCursor, uint id);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CopyIcon(IntPtr hIcon);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyCursor(IntPtr hCursor);

    public static void Execute()
    {
        if (_isExecuting) return;

        _isExecuting = true;

        // Save the original cursor
        _originalNormalCursor = CopyIcon(NORMAL_CURSOR);
        _originalBeamCursor = CopyIcon(BEAM_CURSOR);

        var tempFilePath = Path.GetTempFileName();
        try
        {
            using (var resourceStream =
                   Application.GetResourceStream(new Uri(
                       "Working_32x.ani",
                       UriKind.Relative))!.Stream)
            {
                using FileStream fileStream = new(tempFilePath, FileMode.Create);
                resourceStream.CopyTo(fileStream);
            }

            _customNormalCursor = LoadCursorFromFile(tempFilePath);
            SetSystemCursor(_customNormalCursor, OCR_NORMAL);
            _customBeamCursor = LoadCursorFromFile(tempFilePath);
            SetSystemCursor(_customBeamCursor, OCR_IBEAM);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    public static void Error()
    {
        const string errorCursorPath = @"C:\Windows\Cursors\aero_unavail_l.cur";
        _errorNormalCursor = LoadCursorFromFile(errorCursorPath);
        SetSystemCursor(_errorNormalCursor, OCR_NORMAL);
        _errorBeamCursor = LoadCursorFromFile(errorCursorPath);
        SetSystemCursor(_errorBeamCursor, OCR_IBEAM);
    }

    public static void Restore()
    {
        if (!_isExecuting) return;

        _isExecuting = false;

        // Restore original cursor
        if (_originalNormalCursor != IntPtr.Zero)
        {
            SetSystemCursor(_originalNormalCursor, OCR_NORMAL);
            DestroyCursor(_originalNormalCursor);
            _originalNormalCursor = IntPtr.Zero;
        }

        if (_originalBeamCursor != IntPtr.Zero)
        {
            SetSystemCursor(_originalBeamCursor, OCR_IBEAM);
            DestroyCursor(_originalBeamCursor);
            _originalBeamCursor = IntPtr.Zero;
        }

        // Destroy the custom cursor
        if (_customNormalCursor != IntPtr.Zero)
        {
            DestroyCursor(_customNormalCursor);
            _customNormalCursor = IntPtr.Zero;
        }
        if (_customBeamCursor != IntPtr.Zero)
        {
            DestroyCursor(_customBeamCursor);
            _customBeamCursor = IntPtr.Zero;
        }
        if (_errorNormalCursor != IntPtr.Zero)
        {
            DestroyCursor(_errorNormalCursor);
            _errorNormalCursor = IntPtr.Zero;
        }
        if (_errorBeamCursor != IntPtr.Zero)
        {
            _errorBeamCursor = IntPtr.Zero;
        }
    }


    public static void Arrow()
    {
        var cusor = CopyIcon(LoadCursorFromFile(@"C:\Windows\Cursors\aero_arrow.cur"));
        SetSystemCursor(cusor, OCR_NORMAL);
    }
    public static void Busy()
    {
        var cusor = CopyIcon(LoadCursorFromFile(@"C:\Windows\Cursors\aero_busy.ani"));
        SetSystemCursor(cusor, OCR_NORMAL);
    }
}
