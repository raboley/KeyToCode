using System.Runtime.InteropServices;

namespace KeyToCode;

[StructLayout(LayoutKind.Sequential)]
internal struct Hardwareinput
{
    public uint uMsg;
    public ushort wParamL;
    public ushort wParamH;
}