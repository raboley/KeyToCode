using KeyToCodeKeyboard;

namespace KeyToCode;

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