using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

// PlaybackKeyboardCode.cs
namespace KeyToCode;

public class PlaybackKeyboardCode
{
    private readonly WindowHelper _windowHelper;

    public PlaybackKeyboardCode(WindowHelper windowHelper)
    {
        _windowHelper = windowHelper;
    }
    
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

    public void Connect(IntPtr processMainWindowHandle)
    {
        _windowHelper.Connect(processMainWindowHandle);
    }

    public void Sleep(int milliseconds)
    {
        Thread.Sleep(milliseconds);
    }

    public bool KeyDown(VKey key)
    {
        if (!_windowHelper.SetForegroundWindow())
            return false;

        ForegroundKeyDown(key);
        return _windowHelper.PostMessage(_windowHelper.ProcessMainWindowHandle, Message.KeyDown, key);
    }
    
    public bool SetForegroundWindow()
    {
        return _windowHelper.SetForegroundWindow();
    }

    public bool KeyUp(VKey key)
    {
        if (!_windowHelper.SetForegroundWindow())
            return false;

        var worked = _windowHelper.PostMessage(_windowHelper.ProcessMainWindowHandle, Message.KeyUp, key);
        ForegroundKeyUp(key);

        return worked;
    }

    public static bool ForegroundKeyDown(VKey key)
    {
        uint intReturn;
        Input structInput = new Input
        {
            Type = InputKeyboard,
            U = { ki = { wVk = (ushort)key, dwFlags = 0 } }
        };

        intReturn = SendInput(1, new[] { structInput }, Marshal.SizeOf(new Input()));
        return intReturn == 0;
    }

    public static bool ForegroundKeyUp(VKey key)
    {
        uint intReturn;
        Input structInput = new Input
        {
            Type = InputKeyboard,
            U = { ki = { wVk = (ushort)key, dwFlags = KeyeventfKeyup } }
        };

        intReturn = SendInput(1, new[] { structInput }, Marshal.SizeOf(typeof(Input)));
        return intReturn == 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);
    
    /// <summary>Keyboard input type.</summary>
    private const int InputKeyboard = 1;

    /// <summary>Code for keyup event.</summary>
    private const uint KeyeventfKeyup = 0x0002;
}