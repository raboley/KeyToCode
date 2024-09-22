using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace KeyToCode;

public class PlaybackKeyboardCode
{
    /// <summary>Maps a virtual key to a key code with specified keyboard.</summary>
    private const uint MapvkVkToVscEx = 0x04;

    /// <summary>Keyboard input type.</summary>
    private const int InputKeyboard = 1;

    /// <summary>Code for keyup event.</summary>
    private const uint KeyeventfKeyup = 0x0002;

    private IntPtr _processMainWindowHandle;

    public void Connect(IntPtr processMainWindowHandle)
    {
        if (processMainWindowHandle == IntPtr.Zero)
            throw new ArgumentException("Invalid handle", nameof(processMainWindowHandle));
        _processMainWindowHandle = processMainWindowHandle;
    }

    public void Sleep(int milliseconds)
    {
        Thread.Sleep(milliseconds);
    }

    public bool KeyDown(VKey key)
    {
        if (_processMainWindowHandle == IntPtr.Zero)
            return false;

        if (GetForegroundWindow() != _processMainWindowHandle)
            if (!SetForegroundWindow(_processMainWindowHandle))
                return false;

        ForegroundKeyDown(key);
        var worked = PostMessage(_processMainWindowHandle, Message.KeyDown, key);
        return worked;
    }

    public bool KeyUp(VKey key)
    {
        if (_processMainWindowHandle == IntPtr.Zero)
            return false;

        if (GetForegroundWindow() != _processMainWindowHandle)
            if (!SetForegroundWindow(_processMainWindowHandle))
                return false;

        var worked = PostMessage(_processMainWindowHandle, Message.KeyUp, key);
        ForegroundKeyUp(key);

        return worked;
    }

    public static bool ForegroundKeyDown(VKey key)
    {
        uint intReturn;
        Input structInput;
        structInput = new Input();
        structInput.Type = InputKeyboard;

        // Key down shift, ctrl, and/or alt
        structInput.U.ki.wScan = 0;
        structInput.U.ki.time = 0;
        structInput.U.ki.dwFlags = 0;
        // Key down the actual key-code
        structInput.U.ki.wVk = (ushort)key;
        intReturn = SendInput(1, new[] { structInput }, Marshal.SizeOf(new Input()));

        return intReturn == 0;
    }

    public static bool ForegroundKeyUp(VKey key)
    {
        uint intReturn;
        Input structInput;
        structInput = new Input();
        structInput.Type = InputKeyboard;

        // Key down shift, ctrl, and/or alt
        structInput.U.ki.wScan = 0;
        structInput.U.ki.time = 0;
        structInput.U.ki.dwFlags = 0;
        // Key down the actual key-code
        structInput.U.ki.wVk = (ushort)key;

        // Key up the actual key-code
        structInput.U.ki.dwFlags = KeyeventfKeyup;
        intReturn = SendInput(1, new[] { structInput }, Marshal.SizeOf(typeof(Input)));
        return intReturn == 0;
    }

    private bool PostMessage(IntPtr hWnd, Message message, VKey key, int delay = 0)
    {
        var virtualKey = (uint)key;
        if (PostMessage(hWnd, (int)message, virtualKey,
                GetLParam(1, key, 0, 0, 0, 0)))
            return false;

        Sleep(delay);
        return true;
    }

    private static uint GetLParam(short repeatCount,
        VKey key,
        byte extended,
        byte contextCode,
        byte previousState,
        byte transitionState
    )
    {
        var lParam = (uint)repeatCount;
        //uint scanCode = MapVirtualKey((uint)key, MAPVK_VK_TO_CHAR);
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

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, uint wParam, uint lParam);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    /// <summary>Allows for foreground hardware keyboard key presses</summary>
    /// <param name="nInputs">The number of inputs in pInputs</param>
    /// <param name="pInputs">A Input structure for what is to be pressed.</param>
    /// <param name="cbSize">The size of the structure.</param>
    /// <returns>A message.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    /// <summary>
    ///     The GetForegroundWindow function returns a handle to the foreground window.
    /// </summary>
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    public async Task Play(string inputKeys)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !string.IsNullOrEmpty(a.Location));

        var scriptOptions = ScriptOptions.Default
            .AddReferences(assemblies)
            .AddImports("System", "System.Threading", "KeyToCode");

        var script = CSharpScript.Create(inputKeys, scriptOptions, typeof(Globals));
        var globals = new Globals { _keyboard = this };
        await script.RunAsync(globals);
    }
}

public class Globals
{
    public PlaybackKeyboardCode _keyboard;
}