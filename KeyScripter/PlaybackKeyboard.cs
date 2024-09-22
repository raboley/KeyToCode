using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;

namespace KeyScripter
{
    public class PlaybackKeyboard
    {
        public void Play(List<KeyEvent> keyEvents)
        {
            foreach (var keyEvent in keyEvents)
            {
                if (keyEvent.EventType == KeyEventType.KeyDown)
                {
                    KeyDown(keyEvent.Key);
                }
                else if (keyEvent.EventType == KeyEventType.KeyUp)
                {
                    KeyUp(keyEvent.Key);
                }

                if (keyEvent.Timestamp > 0)
                {
                    Thread.Sleep((int)keyEvent.Timestamp);
                }
            }
        }

        private void KeyDown(Key key)
        {
            var keyCode = KeyInterop.VirtualKeyFromKey(key);
            keybd_event((byte)keyCode, 0, 0, 0);
        }

        private void KeyUp(Key key)
        {
            var keyCode = KeyInterop.VirtualKeyFromKey(key);
            keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, 0);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        private const uint KEYEVENTF_KEYUP = 0x0002;
    }
}