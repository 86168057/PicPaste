using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PicPaste;

/// <summary>
/// 低级别键盘钩子，用于监听全局键盘事件
/// </summary>
public class KeyboardHook : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _proc;

    public event EventHandler<KeyPressedEventArgs>? KeyPressed;

    public void Hook()
    {
        _proc = HookCallback;
        _hookId = SetHook(_proc);
        
        if (_hookId == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            throw new System.ComponentModel.Win32Exception(error, "Failed to set keyboard hook");
        }
    }

    public void Unhook()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
            GetModuleHandle(curModule?.ModuleName), 0);
    }

    private bool _ctrlPressed = false;

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var key = (Keys)vkCode;

            // 检测 Ctrl 键状态
            if (key == Keys.LControlKey || key == Keys.RControlKey || key == Keys.ControlKey)
            {
                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    _ctrlPressed = true;
                }
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    _ctrlPressed = false;
                }
            }

            // 检测 V 键按下
            if ((wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN) && key == Keys.V && _ctrlPressed)
            {
                System.Diagnostics.Debug.WriteLine("KeyboardHook: Ctrl+V detected");
                KeyPressed?.Invoke(this, new KeyPressedEventArgs(key, true));
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        Unhook();
    }

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}

public class KeyPressedEventArgs : EventArgs
{
    public Keys Key { get; }
    public bool Control { get; }

    public KeyPressedEventArgs(Keys key, bool control)
    {
        Key = key;
        Control = control;
    }
}
