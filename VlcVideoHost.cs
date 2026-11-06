using System.Runtime.InteropServices;
using System.Windows.Interop;
using LibVLCSharp.Shared;

namespace CameraViewer;

public sealed class VlcVideoHost : HwndHost
{
    private const string SurfaceClass = "CameraViewer.VideoSurface";
    private const uint WsChild = 0x40000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsClipSiblings = 0x04000000;
    private const uint WsClipChildren = 0x02000000;
    private static readonly WindowProcedure Procedure = DefWindowProc;
    private static readonly IntPtr BackgroundBrush = CreateSolidBrush(0x00181917);
    private static readonly object RegistrationLock = new();
    private static bool _classRegistered;
    private MediaPlayer? _mediaPlayer;
    private IntPtr _handle;

    public MediaPlayer? MediaPlayer
    {
        get => _mediaPlayer;
        set
        {
            _mediaPlayer = value;
            if (_handle != IntPtr.Zero && value is not null) value.Hwnd = _handle;
        }
    }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        EnsureWindowClass();
        _handle = CreateWindowEx(0, SurfaceClass, string.Empty,
            WsChild | WsVisible | WsClipSiblings | WsClipChildren,
            0, 0, 0, 0, hwndParent.Handle, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);
        if (_handle == IntPtr.Zero) throw new InvalidOperationException("Não foi possível criar a superfície de vídeo.");
        if (_mediaPlayer is not null) _mediaPlayer.Hwnd = _handle;
        return new HandleRef(this, _handle);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        if (_mediaPlayer is not null) _mediaPlayer.Hwnd = IntPtr.Zero;
        DestroyWindow(hwnd.Handle);
        _handle = IntPtr.Zero;
    }

    private static void EnsureWindowClass()
    {
        lock (RegistrationLock)
        {
            if (_classRegistered) return;
            var windowClass = new WindowClass
            {
                Size = (uint)Marshal.SizeOf<WindowClass>(),
                Procedure = Marshal.GetFunctionPointerForDelegate(Procedure),
                Instance = GetModuleHandle(null),
                Background = BackgroundBrush,
                ClassName = SurfaceClass
            };
            if (RegisterClassEx(ref windowClass) == 0)
                throw new InvalidOperationException("Não foi possível registrar a superfície de vídeo.");
            _classRegistered = true;
        }
    }

    private delegate IntPtr WindowProcedure(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {
        public uint Size;
        public uint Style;
        public IntPtr Procedure;
        public int ClassExtra;
        public int WindowExtra;
        public IntPtr Instance;
        public IntPtr Icon;
        public IntPtr Cursor;
        public IntPtr Background;
        public string? MenuName;
        public string? ClassName;
        public IntPtr SmallIcon;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern ushort RegisterClassEx(ref WindowClass windowClass);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateWindowEx(uint extendedStyle, string className, string windowName, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(IntPtr hwnd);
    [DllImport("user32.dll", EntryPoint = "DefWindowProcW")] private static extern IntPtr DefWindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? moduleName);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateSolidBrush(uint colorRef);
}
