using System.Runtime.InteropServices;

namespace KeyToCodeKeyboard;

[StructLayout(LayoutKind.Sequential)]
internal struct Hardwareinput
{
    public uint uMsg;
    public ushort wParamL;
    public ushort wParamH;
}