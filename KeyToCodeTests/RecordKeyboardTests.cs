using KeyToCode;
using Xunit;

namespace KeyScripterTests;

public class RecordKeyboardTests
{
    [Fact]
    public void TranslateInput_ReturnsCorrectString()
    {
        // Arrange
        var windowHelper = new WindowHelper();
        var recordKeyboard = new RecordKeyboard(windowHelper);
        var keyEvents = new List<KeyEvent>
        {
            new() { Key = VKey.A, EventType = KeyEventType.KeyDown, Timestamp = 100 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyUp, Timestamp = 200 },
            new() { Key = VKey.B, EventType = KeyEventType.KeyDown, Timestamp = 300 },
            new() { Key = VKey.B, EventType = KeyEventType.KeyUp, Timestamp = 400 }
        };

        // Act
        var result = recordKeyboard.TranslateToCSharp(keyEvents);

        // Assert
        var expected = """
                       _keyboard.KeyDown(VKey.A);
                       _keyboard.Sleep(100);
                       _keyboard.KeyUp(VKey.A);
                       _keyboard.Sleep(100);
                       _keyboard.KeyDown(VKey.B);
                       _keyboard.Sleep(100);
                       _keyboard.KeyUp(VKey.B);
                       _keyboard.Sleep(100);
                       """;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TranslateKeyToString_TranslatesKeyToCSharpCode()
    {
        // Arrange
        var windowHelper = new WindowHelper();
        var recordKeyboard = new RecordKeyboard(windowHelper);

        // Act
        var result = recordKeyboard.TranslateKeyToString(VKey.A, KeyEventType.KeyDown, "_keyboard");

        // Assert
        Assert.Equal("_keyboard.KeyDown(VKey.A);", result);
    }

    [Fact]
    public void CalculateSleepTime_ReturnsCorrectSleepTime()
    {
        // Arrange
        var windowHelper = new WindowHelper();
        var recordKeyboard = new RecordKeyboard(windowHelper);
        // Act
        var result = recordKeyboard.CalculateSleepTime(100, 300, "_keyboard");

        // Assert
        Assert.Equal("_keyboard.Sleep(200);", result);
    }

    [Fact]
    public void RemoveExtraKeyDownsForHeldKeys_RemovesExtraKeyDowns()
    {
        // Arrange
        var windowHelper = new WindowHelper();
        var recordKeyboard = new RecordKeyboard(windowHelper);
        var keyEvents = new List<KeyEvent>
        {
            new() { Key = VKey.A, EventType = KeyEventType.KeyDown, Timestamp = 100 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyDown, Timestamp = 200 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyUp, Timestamp = 300 },
            new() { Key = VKey.B, EventType = KeyEventType.KeyDown, Timestamp = 400 },
            new() { Key = VKey.B, EventType = KeyEventType.KeyUp, Timestamp = 500 }
        };

        // Act
        var result = recordKeyboard.RemoveExtraKeyDownsForHeldKeys(keyEvents);

        // Assert
        var expected = new List<KeyEvent>
        {
            new() { Key = VKey.A, EventType = KeyEventType.KeyDown, Timestamp = 100 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyUp, Timestamp = 300 },
            new() { Key = VKey.B, EventType = KeyEventType.KeyDown, Timestamp = 400 },
            new() { Key = VKey.B, EventType = KeyEventType.KeyUp, Timestamp = 500 }
        };

        // assert each key event is the same
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Key, result[i].Key);
            Assert.Equal(expected[i].EventType, result[i].EventType);
            Assert.Equal(expected[i].Timestamp, result[i].Timestamp);
        }
    }

    [Fact]
    public void RemoveExtraKeyDownsForHeldKeys_OnlyRemovesSequentialDuplicateKeyDowns()
    {
        // Arrange
        var windowHelper = new WindowHelper();
        var recordKeyboard = new RecordKeyboard(windowHelper);
        var keyEvents = new List<KeyEvent>
        {
            new() { Key = VKey.A, EventType = KeyEventType.KeyDown, Timestamp = 100 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyDown, Timestamp = 200 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyUp, Timestamp = 300 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyDown, Timestamp = 400 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyUp, Timestamp = 500 }
        };

        // Act
        var result = recordKeyboard.RemoveExtraKeyDownsForHeldKeys(keyEvents);

        // Assert
        var expected = new List<KeyEvent>
        {
            new() { Key = VKey.A, EventType = KeyEventType.KeyDown, Timestamp = 100 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyUp, Timestamp = 300 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyDown, Timestamp = 400 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyUp, Timestamp = 500 }
        };

        // assert each key event is the same
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Key, result[i].Key);
            Assert.Equal(expected[i].EventType, result[i].EventType);
            Assert.Equal(expected[i].Timestamp, result[i].Timestamp);
        }
    }
}