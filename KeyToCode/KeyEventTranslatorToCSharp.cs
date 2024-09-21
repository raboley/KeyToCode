using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace KeyToCode;

public class KeyEventTranslatorToCSharp
{
    public string TranslateInput(List<KeyEvent> keyEvents)
    {
        return string.Join("\n", keyEvents.Select(TranslateKeyEvent));
    }

    private string TranslateKeyEvent(KeyEvent keyEvent)
    {
        return $"SendKeys.SendWait(\"{keyEvent.Key}\");";
    }
}

public class KeyEvent
{
    public Key Key { get; set; }
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