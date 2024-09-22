namespace KeyToCode;

public enum Message
{
    Nchittest = 0x0084,
    KeyDown = 0x0100, //Key down
    KeyUp = 0x0101, //Key Up
    VmChar = 0x0102, //The character being pressed
    Syskeydown = 0x0104, //An Alt/ctrl/shift + key down message
    Syskeyup = 0x0105, //An Alt/Ctrl/Shift + Key up Message
    Syschar = 0x0106, //An Alt/Ctrl/Shift + Key character Message
    Lbuttondown = 0x201, //Left mousebutton down 
    Lbuttonup = 0x202, //Left mousebutton up 
    Lbuttondblclk = 0x203, //Left mousebutton doubleclick 
    Rbuttondown = 0x204, //Right mousebutton down 
    Rbuttonup = 0x205, //Right mousebutton up 
    Rbuttondblclk = 0x206, //Right mousebutton doubleclick

    /// <summary>Middle mouse button down</summary>
    Mbuttondown = 0x207,

    /// <summary>Middle mouse button up</summary>
    Mbuttonup = 0x208
}