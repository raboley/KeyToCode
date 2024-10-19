using System.Runtime.InteropServices;

namespace KeyToCodeKeyboard;

[StructLayout(LayoutKind.Explicit)]
internal struct InputUnion
{
    [FieldOffset(0)] public Mouseinput mi;
    [FieldOffset(0)] public Keybdinput ki;
    [FieldOffset(0)] public Hardwareinput hi;
}