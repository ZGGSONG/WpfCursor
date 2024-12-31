using Microsoft.Win32;
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
    private const uint IMAGE_CURSOR = 2;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const uint LR_DEFAULTSIZE = 0x00000040;
    private const uint LR_SHARED = 0x00008000;

    private static bool _isExecuting;
    private static IntPtr _originalNormalCursor;
    private static IntPtr _originalBeamCursor;
    private static IntPtr _customNormalCursor;
    private static IntPtr _customBeamCursor;
    private static IntPtr _errorNormalCursor;
    private static IntPtr _errorBeamCursor;

    /// <summary>
    ///     <see href="https://learn.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-loadcursora"/>
    ///     * 此函数已被 LoadImage 函数取代（设置了 LR_DEFAULTSIZE 和 LR_SHARED 标志）。
    /// </summary>
    /// <param name="hInstance"></param>
    /// <param name="lpCursorName"></param>
    /// <returns></returns>
    [DllImport("user32.dll")]
    private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    /// <summary>
    ///     <see href="https://learn.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-loadimagea"/>
    /// </summary>
    /// <param name="hInst"></param>
    /// <param name="lpszName"></param>
    /// <param name="uType"></param>
    /// <param name="cxDesired"></param>
    /// <param name="cyDesired"></param>
    /// <param name="fuLoad"></param>
    /// <returns></returns>
    [DllImport("user32.dll")]
    private static extern IntPtr LoadImage(IntPtr hInst, IntPtr lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr LoadCursorFromFile(string fileName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool SetSystemCursor(IntPtr hCursor, uint id);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CopyIcon(IntPtr hIcon);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyCursor(IntPtr hCursor);

    // 定义 MAKEINTRESOURCE 宏的等效功能
    public static IntPtr MAKEINTRESOURCE(int value)
    {
        return new IntPtr(value);
    }

    /// <summary>
    ///     <see href="https://learn.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-systemparametersinfoa"/>
    /// </summary>
    /// <param name="uiAction"></param>
    /// <param name="uiParam"></param>
    /// <param name="pvParam"></param>
    /// <param name="fWinIni"></param>
    /// <returns></returns>
    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

    /// <summary>
    ///     重置系统光标。将ulParam参数设为0并且pvParam参数设为NULL。
    /// </summary>
    private const uint SPI_SETCURSORS = 0x0057;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDCHANGE = 0x02;

    /// <summary>
    ///     获取鼠标指针大小
    /// </summary>
    private static int GetCursorSize()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors");
        var cursorBaseSize = key?.GetValue("CursorBaseSize");
        return cursorBaseSize is int intVal ? intVal : 32;
    }

    /// <summary>
    ///     执行光标替换操作
    /// </summary>
    public static void Execute()
    {
        if (_isExecuting) return;

        _isExecuting = true;

        try
        {
            // https://stackoverflow.com/a/65213697/18478256
            // 保存原始光标
            var normalCursor = LoadImage(IntPtr.Zero, MAKEINTRESOURCE(OCR_NORMAL), IMAGE_CURSOR, 0, 0, LR_SHARED);
            var beamCursor = LoadImage(IntPtr.Zero, MAKEINTRESOURCE(OCR_IBEAM), IMAGE_CURSOR, 0, 0, LR_SHARED);

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

                //int cursorSize = GetCursorSize();

                _customNormalCursor = LoadCursorFromFile(tempFilePath);
                //_customNormalCursor = LoadImage(IntPtr.Zero, tempFilePath, IMAGE_CURSOR, cursorSize, cursorSize, LR_LOADFROMFILE);
                if (_customNormalCursor == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to load custom normal cursor");

                _customBeamCursor = LoadCursorFromFile(tempFilePath);
                //_customBeamCursor = LoadImage(IntPtr.Zero, tempFilePath, IMAGE_CURSOR, cursorSize, cursorSize, LR_LOADFROMFILE);
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

            // 通知系统更新所有光标
            SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

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
}