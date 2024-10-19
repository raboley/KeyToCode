using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace KeyToCode;

public class PlaybackKeyboardCode
{
    private readonly WindowHelper _windowHelper;
    private CancellationTokenSource _cancellationTokenSource;
    public CancellationToken CancellationToken { get; set; }

    public PlaybackKeyboardCode(WindowHelper windowHelper)
    {
        _windowHelper = windowHelper;
    }

    public async Task Play(string inputKeys)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;
        CancellationToken = cancellationToken;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !string.IsNullOrEmpty(a.Location));

        var scriptOptions = ScriptOptions.Default
            .AddReferences(assemblies)
            .AddImports("System", "System.Threading", "KeyToCode");

        var script = CSharpScript.Create(inputKeys, scriptOptions, typeof(Globals));
        var globals = new Globals { _keyboard = this};

        try
        {
            await script.RunAsync(globals, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Script playback was cancelled");
            // ensure all keys that were pressed are released
            foreach (var key in Enum.GetValues(typeof(VKey)).Cast<VKey>())
            {
                KeyUp(key);
            }
        }
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
    }

    public void Connect(IntPtr processMainWindowHandle)
    {
        _windowHelper.Connect(processMainWindowHandle);
    }

    public void Sleep(int milliseconds)
    {
        var delayTask = Task.Delay(milliseconds, CancellationToken);
        try
        {
            delayTask.Wait(CancellationToken);
        }
        catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
        {
            Console.WriteLine("Cancelling sleep, operation was cancelled");
            CancellationToken.ThrowIfCancellationRequested();
        }
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

    private const int InputKeyboard = 1;
    private const uint KeyeventfKeyup = 0x0002;
}