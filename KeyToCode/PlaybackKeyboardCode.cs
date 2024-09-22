using System.Runtime.InteropServices;

namespace KeyToCode;

public class PlaybackKeyboardCode
{
    private IntPtr _processMainWindowHandle;

    public void Connect(IntPtr processMainWindowHandle)
    {
        if (processMainWindowHandle == IntPtr.Zero)
            throw new ArgumentException("Invalid handle", nameof(processMainWindowHandle));
        _processMainWindowHandle = processMainWindowHandle;
    }

    public void Sleep(int milliseconds)
    {
        Thread.Sleep(milliseconds);
    }

    public bool KeyDown(VKey key)
    {
        if (_processMainWindowHandle == IntPtr.Zero)
            return false;
        
        if (GetForegroundWindow() != _processMainWindowHandle)
        {
            if (!SetForegroundWindow(_processMainWindowHandle))
                return false;
        }
        
        ForegroundKeyDown(key);
        var worked = PostMessage(_processMainWindowHandle, Message.KEY_DOWN, key);
        return worked;
    }

    public bool KeyUp(VKey key)
    {
        if (_processMainWindowHandle == IntPtr.Zero)
            return false;
        
        if (GetForegroundWindow() != _processMainWindowHandle)
        {
            if (!SetForegroundWindow(_processMainWindowHandle))
                return false;
        }
        
        var worked = PostMessage(_processMainWindowHandle, Message.KEY_UP, key);
        ForegroundKeyUp(key);
        
        return worked;
    }
    
    public static bool ForegroundKeyDown(VKey key)
    {
        uint intReturn;
        INPUT structInput;
        structInput = new INPUT();
        structInput.type = INPUT_KEYBOARD;

        // Key down shift, ctrl, and/or alt
        structInput.u.ki.wScan = 0;
        structInput.u.ki.time = 0;
        structInput.u.ki.dwFlags = 0;
        // Key down the actual key-code
        structInput.u.ki.wVk = (ushort) key;
        intReturn = SendInput(1, new []{structInput}, Marshal.SizeOf(new INPUT()));

        // Key up shift, ctrl, and/or alt
        //keybd_event((int)key.VK, GetScanCode(key.VK) + 0x80, KEYEVENTF_NONE, 0);
        //keybd_event((int)key.VK, GetScanCode(key.VK) + 0x80, KEYEVENTF_KEYUP, 0);
        return true;
    }

    public static bool ForegroundKeyUp(VKey key)
    {
        uint intReturn;
        INPUT structInput;
        structInput = new INPUT();
        structInput.type = INPUT_KEYBOARD;

        // Key down shift, ctrl, and/or alt
        structInput.u.ki.wScan = 0;
        structInput.u.ki.time = 0;
        structInput.u.ki.dwFlags = 0;
        // Key down the actual key-code
        structInput.u.ki.wVk = (ushort)key;

        // Key up the actual key-code
        structInput.u.ki.dwFlags = KEYEVENTF_KEYUP;
        intReturn = SendInput(1, new[] { structInput }, Marshal.SizeOf(typeof(INPUT)));
        return true;
    }

    private bool PostMessage(IntPtr hWnd, Message message, VKey key, int delay = 0)
    {
        var virtualKey = (uint) key;
        if (PostMessage(hWnd, (int)message, virtualKey,
                GetLParam(1, key, 0, 0, 0, 0)))
            return false;
        
        Sleep(delay);
        return true;
    }

    private static uint GetLParam(int x, int y)
    {
        return (uint)((y << 16) | (x & 0xFFFF));
    }
    private static uint GetLParam(Int16 repeatCount, VKey key, byte extended, byte contextCode, byte previousState,
        byte transitionState)
    {
        var lParam = (uint) repeatCount;
        //uint scanCode = MapVirtualKey((uint)key, MAPVK_VK_TO_CHAR);
        uint scanCode = GetScanCode(key);
        lParam += scanCode*0x10000;
        lParam += (uint) ((extended)*0x1000000);
        lParam += (uint) ((contextCode*2)*0x10000000);
        lParam += (uint) ((previousState*4)*0x10000000);
        lParam += (uint) ((transitionState*8)*0x10000000);
        return lParam;
    }
    private static uint GetScanCode(VKey key)
    {
        return MapVirtualKey((uint) key, MAPVK_VK_TO_VSC_EX);
    }
    
    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, int Msg, uint wParam, uint lParam);
    
    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);
    
    /// <summary>Maps a virtual key to a key code with specified keyboard.</summary>
    private const uint MAPVK_VK_TO_VSC_EX = 0x04;
        
    /// <summary>Mouse input type.</summary>
    private const int INPUT_MOUSE = 0;

    /// <summary>Keyboard input type.</summary>
    private const int INPUT_KEYBOARD = 1;

    /// <summary>Hardware input type.</summary>
    private const int INPUT_HARDWARE = 2;
    
    /// <summary>Code for keyup event.</summary>
    private const uint KEYEVENTF_KEYUP = 0x0002;
    
    /// <summary>Allows for foreground hardware keyboard key presses</summary>
    /// <param name="nInputs">The number of inputs in pInputs</param>
    /// <param name="pInputs">A Input structure for what is to be pressed.</param>
    /// <param name="cbSize">The size of the structure.</param>
    /// <returns>A message.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    
    /// <summary>
    ///     The GetForegroundWindow function returns a handle to the foreground window.
    /// </summary>
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    public void Play(List<KeyEvent> keyEvents)
    {
        throw new NotImplementedException();
    }
}

struct INPUT
{
    public int type;
    public InputUnion u;
};

[StructLayout(LayoutKind.Explicit)]
struct InputUnion
{
    [FieldOffset(0)]
    public MOUSEINPUT mi;
    [FieldOffset(0)]
    public KEYBDINPUT ki;
    [FieldOffset(0)]
    public HARDWAREINPUT hi;
}

[StructLayout(LayoutKind.Sequential)]
struct MOUSEINPUT
{
    public int dx;
    public int dy;
    public uint mouseData;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
};

[StructLayout(LayoutKind.Sequential)]
struct KEYBDINPUT
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
};

[StructLayout(LayoutKind.Sequential)]
struct HARDWAREINPUT
{
    public uint uMsg;
    public ushort wParamL;
    public ushort wParamH;
};

public enum Message
{
    NCHITTEST = (0x0084),
    KEY_DOWN = (0x0100), //Key down
    KEY_UP = (0x0101), //Key Up
    VM_CHAR = (0x0102), //The character being pressed
    SYSKEYDOWN = (0x0104), //An Alt/ctrl/shift + key down message
    SYSKEYUP = (0x0105), //An Alt/Ctrl/Shift + Key up Message
    SYSCHAR = (0x0106), //An Alt/Ctrl/Shift + Key character Message
    LBUTTONDOWN = (0x201), //Left mousebutton down 
    LBUTTONUP = (0x202), //Left mousebutton up 
    LBUTTONDBLCLK = (0x203), //Left mousebutton doubleclick 
    RBUTTONDOWN = (0x204), //Right mousebutton down 
    RBUTTONUP = (0x205), //Right mousebutton up 
    RBUTTONDBLCLK = (0x206), //Right mousebutton doubleclick

    /// <summary>Middle mouse button down</summary>
    MBUTTONDOWN = (0x207),

    /// <summary>Middle mouse button up</summary>
    MBUTTONUP = (0x208)
}

[Serializable]
public enum VKey
{
    D0 = 0x30, //0 key 
    D1 = 0x31, //1 key 
    D2 = 0x32, //2 key 
    D3 = 0x33, //3 key 
    D4 = 0x34, //4 key 
    D5 = 0x35, //5 key 
    D6 = 0x36, //6 key 
    D7 = 0x37, //7 key 
    D8 = 0x38, //8 key 
    D9 = 0x39, //9 key
    MINUS = 0xBD, // - key
    PLUS = 0xBB, // + key
    A = 0x41, //A key 
    B = 0x42, //B key 
    C = 0x43, //C key 
    D = 0x44, //D key 
    E = 0x45, //E key 
    F = 0x46, //F key 
    G = 0x47, //G key 
    H = 0x48, //H key 
    I = 0x49, //I key 
    J = 0x4A, //J key 
    K = 0x4B, //K key 
    L = 0x4C, //L key 
    M = 0x4D, //M key 
    N = 0x4E, //N key 
    O = 0x4F, //O key 
    P = 0x50, //P key 
    Q = 0x51, //Q key 
    R = 0x52, //R key 
    S = 0x53, //S key 
    T = 0x54, //T key 
    U = 0x55, //U key 
    V = 0x56, //V key 
    W = 0x57, //W key 
    X = 0x58, //X key 
    Y = 0x59, //Y key 
    Z = 0x5A, //Z key 
    LBUTTON = 0x01, //Left mouse button 
    RBUTTON = 0x02, //Right mouse button 
    CANCEL = 0x03, //Control-break processing 
    MBUTTON = 0x04, //Middle mouse button (three-button mouse) 
    BACK = 0x08, //BACKSPACE key 
    TAB = 0x09, //TAB key 
    CLEAR = 0x0C, //CLEAR key 
    RETURN = 0x0D, //ENTER key 
    SHIFT = 0x10, //SHIFT key 
    CONTROL = 0x11, //CTRL key 
    MENU = 0x12, //ALT key 
    PAUSE = 0x13, //PAUSE key 
    CAPITAL = 0x14, //CAPS LOCK key 
    ESCAPE = 0x1B, //ESC key 
    SPACE = 0x20, //SPACEBAR 
    PRIOR = 0x21, //PAGE UP key 
    NEXT = 0x22, //PAGE DOWN key 
    END = 0x23, //END key 
    HOME = 0x24, //HOME key 
    LEFT = 0x25, //LEFT ARROW key 
    UP = 0x26, //UP ARROW key 
    RIGHT = 0x27, //RIGHT ARROW key 
    DOWN = 0x28, //DOWN ARROW key 
    SELECT = 0x29, //SELECT key 
    PRINT = 0x2A, //PRINT key 
    EXECUTE = 0x2B, //EXECUTE key 
    SNAPSHOT = 0x2C, //PRINT SCREEN key 
    INSERT = 0x2D, //INS key 
    DELETE = 0x2E, //DEL key 
    HELP = 0x2F, //HELP key 
    NUMPAD0 = 0x60, //Numeric keypad 0 key 
    NUMPAD1 = 0x61, //Numeric keypad 1 key 
    NUMPAD2 = 0x62, //Numeric keypad 2 key 
    NUMPAD3 = 0x63, //Numeric keypad 3 key 
    NUMPAD4 = 0x64, //Numeric keypad 4 key 
    NUMPAD5 = 0x65, //Numeric keypad 5 key 
    NUMPAD6 = 0x66, //Numeric keypad 6 key 
    NUMPAD7 = 0x67, //Numeric keypad 7 key 
    NUMPAD8 = 0x68, //Numeric keypad 8 key 
    NUMPAD9 = 0x69, //Numeric keypad 9 key 
    SEPARATOR = 0x6C, //Separator key 
    SUBTRACT = 0x6D, //Subtract key 
    DECIMAL = 0x6E, //Decimal key 
    DIVIDE = 0x6F, //Divide key 
    F1 = 0x70, //F1 key 
    F2 = 0x71, //F2 key 
    F3 = 0x72, //F3 key 
    F4 = 0x73, //F4 key 
    F5 = 0x74, //F5 key 
    F6 = 0x75, //F6 key 
    F7 = 0x76, //F7 key 
    F8 = 0x77, //F8 key 
    F9 = 0x78, //F9 key 
    F10 = 0x79, //F10 key 
    F11 = 0x7A, //F11 key 
    F12 = 0x7B, //F12 key 
    SCROLL = 0x91, //SCROLL LOCK key 
    LSHIFT = 0xA0, //Left SHIFT key 
    RSHIFT = 0xA1, //Right SHIFT key 
    LCONTROL = 0xA2, //Left CONTROL key 
    RCONTROL = 0xA3, //Right CONTROL key 
    LMENU = 0xA4, //Left MENU key 
    RMENU = 0xA5, //Right MENU key 
    COMMA = 0xBC, //, key
    PERIOD = 0xBE, //. key
    PLAY = 0xFA, //Play key 
    ZOOM = 0xFB, //Zoom key 
    NULL = 0x0,
}