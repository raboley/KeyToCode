using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace KeyToCodeKeyboard;

/// <summary>
/// Class responsible for playing back keyboard input.
/// </summary>
public class PlaybackKeyboardCode : IDisposable
{
    private readonly WindowHelper _windowHelper;
    private CancellationTokenSource _cancellationTokenSource;
    public CancellationToken CancellationToken { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackKeyboardCode"/> class.
    /// </summary>
    /// <param name="windowHelper">The window helper instance.</param>
    public PlaybackKeyboardCode(WindowHelper windowHelper)
    {
        _windowHelper = windowHelper ?? throw new ArgumentNullException(nameof(windowHelper));
    }

    /// <summary>
    /// Plays the specified input keys.
    /// </summary>
    /// <param name="inputKeys">The input keys to play.</param>
    public async Task Play(string inputKeys)
    {
        if (inputKeys == null) throw new ArgumentNullException(nameof(inputKeys));

        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;
        CancellationToken = cancellationToken;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !string.IsNullOrEmpty(a.Location));

        var scriptOptions = ScriptOptions.Default
            .AddReferences(assemblies)
            .AddImports("System", "System.Threading", "KeyToCode", "KeyToCodeKeyboard");

        var script = CSharpScript.Create(inputKeys, scriptOptions, typeof(Globals));
        var globals = new Globals { _keyboard = this };

        try
        {
            await script.RunAsync(globals, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Script playback was cancelled");
            // Ensure all keys that were pressed are released
            foreach (var key in Enum.GetValues(typeof(VKey)).Cast<VKey>())
            {
                KeyUp(key);
            }
        }
    }

    /// <summary>
    /// Stops the playback.
    /// </summary>
    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Connects to the specified process main window handle.
    /// </summary>
    /// <param name="processMainWindowHandle">The process main window handle.</param>
    public void Connect(IntPtr processMainWindowHandle)
    {
        _windowHelper.Connect(processMainWindowHandle);
    }

    /// <summary>
    /// Sleeps for the specified number of milliseconds, is cancellable.
    /// </summary>
    /// <param name="milliseconds">The number of milliseconds to sleep.</param>
    public async Task Sleep(int milliseconds)
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

    /// <summary>
    /// Simulates a key down event.
    /// </summary>
    /// <param name="key">The key to press down.</param>
    /// <returns>True if the key down event was successful; otherwise, false.</returns>
    public bool KeyDown(VKey key)
    {
        if (!_windowHelper.SetForegroundWindow())
            return false;

        ForegroundKeyDown(key);
        return _windowHelper.PostMessage(_windowHelper.ProcessMainWindowHandle, Message.KeyDown, key);
    }

    /// <summary>
    /// Sets the foreground window.
    /// </summary>
    /// <returns>True if the foreground window was set; otherwise, false.</returns>
    public bool SetForegroundWindow()
    {
        return _windowHelper.SetForegroundWindow();
    }

    /// <summary>
    /// Simulates a key up event.
    /// </summary>
    /// <param name="key">The key to release.</param>
    /// <returns>True if the key up event was successful; otherwise, false.</returns>
    public bool KeyUp(VKey key)
    {
        if (!_windowHelper.SetForegroundWindow())
            return false;

        var worked = _windowHelper.PostMessage(_windowHelper.ProcessMainWindowHandle, Message.KeyUp, key);
        ForegroundKeyUp(key);

        return worked;
    }

    /// <summary>
    /// Simulates a foreground key down event.
    /// </summary>
    /// <param name="key">The key to press down.</param>
    /// <returns>True if the key down event was successful; otherwise, false.</returns>
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

    /// <summary>
    /// Simulates a foreground key up event.
    /// </summary>
    /// <param name="key">The key to release.</param>
    /// <returns>True if the key up event was successful; otherwise, false.</returns>
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

    /// <summary>
    /// Disposes the resources used by the <see cref="PlaybackKeyboardCode"/> class.
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }
}