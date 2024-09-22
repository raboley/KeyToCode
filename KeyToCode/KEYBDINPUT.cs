using System.Runtime.InteropServices;

namespace KeyToCode;

[StructLayout(LayoutKind.Sequential)]
internal struct Keybdinput
{
    /*Virtual Key code.  Must be from 1-254.  If the dwFlags member specifies KEYEVENTF_UNICODE, wVk must be 0.*/
    public ushort wVk;

    /*A hardware scan code for the key. If dwFlags specifies KEYEVENTF_UNICODE, wScan specifies a Unicode character which is to be sent to the foreground application.*/
    public ushort wScan;

    /*Specifies various aspects of a keystroke.  See the KEYEVENTF_ constants for more information.*/
    public uint dwFlags;

    /*The time stamp for the event, in milliseconds. If this parameter is zero, the system will provide its own time stamp.*/
    public uint time;

    /*An additional value associated with the keystroke. Use the GetMessageExtraInfo function to obtain this information.*/
    public IntPtr dwExtraInfo;
}