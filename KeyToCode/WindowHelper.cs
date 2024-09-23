// WindowHelper.cs
using System.Runtime.InteropServices;

namespace KeyToCode;

public class WindowHelper
{
    public IntPtr ProcessMainWindowHandle;

    public void Connect(IntPtr processMainWindowHandle)
    {
        if (processMainWindowHandle == IntPtr.Zero)
            throw new ArgumentException("Invalid handle", nameof(processMainWindowHandle));
        ProcessMainWindowHandle = processMainWindowHandle;
    }

    public bool SetForegroundWindow()
    {
        if (ProcessMainWindowHandle == IntPtr.Zero)
            return false;

        if (GetForegroundWindow() != ProcessMainWindowHandle)
            return SetForegroundWindow(ProcessMainWindowHandle);

        return true;
    }

    public bool PostMessage(IntPtr hWnd, Message message, VKey key, int delay = 0)
    {
        var virtualKey = (uint)key;
        if (!PostMessage(hWnd, (int)message, virtualKey, GetLParam(1, key, 0, 0, 0, 0)))
            return false;

        Thread.Sleep(delay);
        return true;
    }

    private static uint GetLParam(short repeatCount, VKey key, byte extended, byte contextCode, byte previousState, byte transitionState)
    {
        var lParam = (uint)repeatCount;
        var scanCode = GetScanCode(key);
        lParam += scanCode * 0x10000;
        lParam += (uint)(extended * 0x1000000);
        lParam += (uint)(contextCode * 2 * 0x10000000);
        lParam += (uint)(previousState * 4 * 0x10000000);
        lParam += (uint)(transitionState * 8 * 0x10000000);
        return lParam;
    }

    private static uint GetScanCode(VKey key)
    {
        return MapVirtualKey((uint)key, MapvkVkToVscEx);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, uint wParam, uint lParam);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    
    private const uint MapvkVkToVscEx = 0x04;
}