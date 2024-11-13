using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace WpfCursor;

/// <summary>
///     光标管理类，用于设置和恢复系统光标
/// </summary>
public class CursorManager
{
    private const int OCR_NORMAL = 32512;
    private const int OCR_IBEAM = 32513;

    private static bool _isExecuting;
    private static IntPtr _originalNormalCursor;
    private static IntPtr _originalBeamCursor;
    private static IntPtr _customNormalCursor;
    private static IntPtr _customBeamCursor;
    private static IntPtr _errorNormalCursor;
    private static IntPtr _errorBeamCursor;

    [DllImport("user32.dll")]
    private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr LoadCursorFromFile(string fileName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool SetSystemCursor(IntPtr hCursor, uint id);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CopyIcon(IntPtr hIcon);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyCursor(IntPtr hCursor);

    /// <summary>
    ///     执行光标替换操作
    /// </summary>
    public static void Execute()
    {
        if (_isExecuting) return;

        _isExecuting = true;

        try
        {
            // 保存原始光标
            var normalCursor = LoadCursor(IntPtr.Zero, OCR_NORMAL);
            var beamCursor = LoadCursor(IntPtr.Zero, OCR_IBEAM);

            if (normalCursor != IntPtr.Zero)
                _originalNormalCursor = CopyIcon(normalCursor);

            if (beamCursor != IntPtr.Zero)
                _originalBeamCursor = CopyIcon(beamCursor);

            // 检查是否成功保存原始光标
            if (_originalNormalCursor == IntPtr.Zero || _originalBeamCursor == IntPtr.Zero)
                throw new InvalidOperationException("Failed to save original cursors");

            // 设置自定义光标
            var tempFilePath = Path.GetTempFileName();
            try
            {
                using (var resourceStream =
                       Application.GetResourceStream(new Uri(
                           "Working_32x.ani",
                           UriKind.Relative))?.Stream)
                {
                    if (resourceStream == null)
                        throw new InvalidOperationException("Could not load cursor resource");

                    using FileStream fileStream = new(tempFilePath, FileMode.Create);
                    resourceStream.CopyTo(fileStream);
                }

                _customNormalCursor = LoadCursorFromFile(tempFilePath);
                if (_customNormalCursor == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to load custom normal cursor");

                _customBeamCursor = LoadCursorFromFile(tempFilePath);
                if (_customBeamCursor == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to load custom beam cursor");

                SetSystemCursor(_customNormalCursor, OCR_NORMAL);
                SetSystemCursor(_customBeamCursor, OCR_IBEAM);
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }
        catch (Exception)
        {
            Restore(); // 出错时恢复原始状态
            throw;
        }
    }

    /// <summary>
    ///     设置错误光标
    /// </summary>
    public static void Error()
    {
        try
        {
            const string errorCursorPath = @"C:\Windows\Cursors\aero_unavail_l.cur";

            _errorNormalCursor = LoadCursorFromFile(errorCursorPath);
            if (_errorNormalCursor == IntPtr.Zero)
                throw new InvalidOperationException("Failed to load error normal cursor");

            _errorBeamCursor = LoadCursorFromFile(errorCursorPath);
            if (_errorBeamCursor == IntPtr.Zero)
                throw new InvalidOperationException("Failed to load error beam cursor");

            SetSystemCursor(_errorNormalCursor, OCR_NORMAL);
            SetSystemCursor(_errorBeamCursor, OCR_IBEAM);
        }
        catch (Exception)
        {
            Restore();
            throw;
        }
    }

    /// <summary>
    ///     恢复原始光标
    /// </summary>
    public static void Restore()
    {
        if (!_isExecuting) return;

        try
        {
            // 恢复原始光标
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

            // 清理自定义光标
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

            // 清理错误光标
            if (_errorNormalCursor != IntPtr.Zero)
            {
                DestroyCursor(_errorNormalCursor);
                _errorNormalCursor = IntPtr.Zero;
            }

            if (_errorBeamCursor != IntPtr.Zero)
            {
                DestroyCursor(_errorBeamCursor);
                _errorBeamCursor = IntPtr.Zero;
            }
        }
        finally
        {
            _isExecuting = false;
        }
    }

    /// <summary>
    ///     设置箭头光标
    /// </summary>
    public static void Arrow()
    {
        try
        {
            var cursor = LoadCursorFromFile(@"C:\Windows\Cursors\aero_arrow.cur");
            if (cursor != IntPtr.Zero)
            {
                var copiedCursor = CopyIcon(cursor);
                if (copiedCursor != IntPtr.Zero) SetSystemCursor(copiedCursor, OCR_NORMAL);

                DestroyCursor(cursor);
            }
        }
        catch (Exception)
        {
            Restore();
            throw;
        }
    }

    /// <summary>
    ///     设置忙碌光标
    /// </summary>
    public static void Busy()
    {
        try
        {
            var cursor = LoadCursorFromFile(@"C:\Windows\Cursors\aero_busy.ani");
            if (cursor != IntPtr.Zero)
            {
                var copiedCursor = CopyIcon(cursor);
                if (copiedCursor != IntPtr.Zero) SetSystemCursor(copiedCursor, OCR_NORMAL);

                DestroyCursor(cursor);
            }
        }
        catch (Exception)
        {
            Restore();
            throw;
        }
    }
}