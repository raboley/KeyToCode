using System.Runtime.InteropServices;

namespace KeyToCodeKeyboard;

[StructLayout(LayoutKind.Sequential)]
internal struct Mouseinput
{
    public int dx;
    public int dy;
    public uint mouseData;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}