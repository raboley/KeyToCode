using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace KeyToCode
{
    public class RecordKeyboard
    {
        private List<KeyEvent> _keyEvents;
        private Stopwatch _stopwatch;
        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;

        public RecordKeyboard()
        {
            _keyEvents = new List<KeyEvent>();
            _stopwatch = new Stopwatch();
            _proc = HookCallback;
        }

        public void StartRecording()
        {
            _keyEvents.Clear();
            _stopwatch.Restart();
            _hookID = SetHook(_proc);
        }

        public string StopRecording()
        {
            _stopwatch.Stop();
            UnhookWindowsHookEx(_hookID);
            return TranslateInput();
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                // turn the virtual key code into a VKeys
                var key = (VKey)vkCode;
                
                KeyEventType eventType = wParam == (IntPtr)WM_KEYDOWN ? KeyEventType.KeyDown : KeyEventType.KeyUp;
                _keyEvents.Add(new KeyEvent
                {
                    Key = key,
                    EventType = eventType,
                    Timestamp = _stopwatch.ElapsedMilliseconds
                });
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private string TranslateInput()
        {
            return TranslateToCSharp(_keyEvents);
            return string.Join("\n", _keyEvents);
        }

        public string TranslateToCSharp(List<KeyEvent> keyEvents, string keyboardName = "_keyboard")
        {
            var dedupedKeyEvents = RemoveExtraKeyDownsForHeldKeys(keyEvents);
            
            
            long previousTimestamp = 0;
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var keyEvent in dedupedKeyEvents)
            {
                stringBuilder.AppendLine(TranslateKeyToString(keyEvent.Key, keyEvent.EventType, keyboardName));
                stringBuilder.AppendLine(CalculateSleepTime(previousTimestamp, keyEvent.Timestamp, keyboardName));
                previousTimestamp = keyEvent.Timestamp;
            }
            
            // trim the last new line
            stringBuilder.Length -= 2;
            return stringBuilder.ToString();
        }
        
        public string TranslateKeyToString(VKey key, KeyEventType eventType, string keyboardName)
        {
            return $"{keyboardName}.{eventType}(VKeys.{key});";
        }
        
        public string CalculateSleepTime(long previousTimestamp, long currentTimestamp, string keyboardName)
        {
            return $"{keyboardName}.Sleep({currentTimestamp - previousTimestamp});";
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

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
            {
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
            }
            return result;
        }
    }

    public class KeyEvent
    {
        public VKey Key { get; set; }
        public KeyEventType EventType { get; set; }
        public long Timestamp { get; set; }

        public override string ToString()
        {
            return $"{EventType} {Key} at {Timestamp}ms";
        }
    }

    public enum KeyEventType
    {
        KeyDown,
        KeyUp
    }
}