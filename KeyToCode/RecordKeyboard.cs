using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

// RecordKeyboard.cs
namespace KeyToCode;

public class RecordKeyboard
{
    private const int WhKeyboardLl = 13;
    private const int WmKeydown = 0x0100;
    private const int WmKeyup = 0x0101;
    private IntPtr _hookId = IntPtr.Zero;
    private readonly List<KeyEvent> _keyEvents;
    private readonly LowLevelKeyboardProc _proc;
    private readonly Stopwatch _stopwatch;
    private readonly WindowHelper _windowHelper;
    private readonly Dictionary<VKey,Action> _keyActions;

    public RecordKeyboard(WindowHelper windowHelper, Dictionary<VKey, Action> configKeyActions = null)
    {
        _keyEvents = new List<KeyEvent>();
        _stopwatch = new Stopwatch();
        _proc = HookCallback;
        _windowHelper = windowHelper;
        _keyActions = configKeyActions;
    }

    public void StartRecording()
    {
        _keyEvents.Clear();
        _stopwatch.Restart();
        _hookId = SetHook(_proc);
    }

    public string StopRecording()
    {
        _stopwatch.Stop();
        UnhookWindowsHookEx(_hookId);
        return TranslateInput();
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            if (curModule == null)
                return IntPtr.Zero;

            return SetWindowsHookEx(WhKeyboardLl, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WmKeydown || wParam == WmKeyup))
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var key = (VKey)vkCode;

            
            if (wParam == WmKeyup && _keyActions.TryGetValue(key, out var action))
            {
                action.Invoke();
            }
            
            var eventType = wParam == WmKeydown ? KeyEventType.KeyDown : KeyEventType.KeyUp;
            _keyEvents.Add(new KeyEvent
            {
                Key = key,
                EventType = eventType,
                Timestamp = _stopwatch.ElapsedMilliseconds
            });
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private string TranslateInput()
    {
        return TranslateToCSharp(_keyEvents);
    }

    public string TranslateToCSharp(List<KeyEvent> keyEvents, string keyboardName = "_keyboard")
    {
        if (keyEvents.Count == 0)
            return "";

        var dedupedKeyEvents = RemoveExtraKeyDownsForHeldKeys(keyEvents);

        long previousTimestamp = 0;
        var stringBuilder = new StringBuilder();
        foreach (var keyEvent in dedupedKeyEvents)
        {
            stringBuilder.AppendLine(TranslateKeyToString(keyEvent.Key, keyEvent.EventType, keyboardName));
            stringBuilder.AppendLine(CalculateSleepTime(previousTimestamp, keyEvent.Timestamp, keyboardName));
            previousTimestamp = keyEvent.Timestamp;
        }

        stringBuilder.Length -= 2;
        return stringBuilder.ToString();
    }

    public string TranslateKeyToString(VKey key, KeyEventType eventType, string keyboardName)
    {
        return $"{keyboardName}.{eventType}(VKey.{key});";
    }

    public string CalculateSleepTime(long previousTimestamp, long currentTimestamp, string keyboardName)
    {
        return $"{keyboardName}.Sleep({currentTimestamp - previousTimestamp});";
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public List<KeyEvent> RemoveExtraKeyDownsForHeldKeys(List<KeyEvent> keyEvents)
    {
        var result = new List<KeyEvent>();
        var heldKeys = new List<VKey>();
        foreach (var keyEvent in keyEvents)
            if (keyEvent.EventType == KeyEventType.KeyDown)
            {
                if (!heldKeys.Contains(keyEvent.Key))
                {
                    heldKeys.Add(keyEvent.Key);
                    result.Add(keyEvent);
                }
            }
            else if (keyEvent.EventType == KeyEventType.KeyUp)
            {
                heldKeys.Remove(keyEvent.Key);
                result.Add(keyEvent);
            }

        return result;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
}