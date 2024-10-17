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
                       _keyboard.Sleep(100);
                       _keyboard.KeyDown(VKey.A);
                       _keyboard.Sleep(100);
                       _keyboard.KeyUp(VKey.A);
                       _keyboard.Sleep(100);
                       _keyboard.KeyDown(VKey.B);
                       _keyboard.Sleep(100);
                       _keyboard.KeyUp(VKey.B);
                       """;
        Assert.Equal(expected, result);
    }
    
[Fact]
public void TranslateInput_alloctes_sleep_correctly_ReturnsCorrectString()
{
    // Arrange
    var windowHelper = new WindowHelper();
    var recordKeyboard = new RecordKeyboard(windowHelper);
    var keyEvents = new List<KeyEvent>
    {
        new() { Key = VKey.F5, EventType = KeyEventType.KeyUp, Timestamp = 73 },
        new() { Key = VKey.F, EventType = KeyEventType.KeyDown, Timestamp = 905 },
        new() { Key = VKey.F, EventType = KeyEventType.KeyUp, Timestamp = 1049 },
        new() { Key = VKey.Return, EventType = KeyEventType.KeyDown, Timestamp = 3641 },
        new() { Key = VKey.Return, EventType = KeyEventType.KeyUp, Timestamp = 3721 }
    };

    // Act
    var result = recordKeyboard.TranslateToCSharp(keyEvents);

    // Assert
    var expected = """
                   _keyboard.Sleep(73);
                   _keyboard.KeyUp(VKey.F5);
                   _keyboard.Sleep(832);
                   _keyboard.KeyDown(VKey.F);
                   _keyboard.Sleep(144);
                   _keyboard.KeyUp(VKey.F);
                   _keyboard.Sleep(2592);
                   _keyboard.KeyDown(VKey.Return);
                   _keyboard.Sleep(80);
                   _keyboard.KeyUp(VKey.Return);
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
    
    // add a test that will esure that when we press F2 to stop recording that we trim the F2 and any subsequent key events or sleep times from the output
    [Fact]
    public void RemoveExtraKeyDownsForHeldKeys_RemovesExtraKeyDownsAfterF2()
    {
        // Arrange
        var f2StopRecordingAction = new Dictionary<VKey, Action>
        {
            [VKey.F2] = () => { }
        };
        var windowHelper = new WindowHelper();
        var recordKeyboard = new RecordKeyboard(windowHelper, f2StopRecordingAction);
        var keyEvents = new List<KeyEvent>
        {
            new() { Key = VKey.A, EventType = KeyEventType.KeyDown, Timestamp = 100 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyUp, Timestamp = 200 },
            new() { Key = VKey.F2, EventType = KeyEventType.KeyDown, Timestamp = 300 },
            new() { Key = VKey.F2, EventType = KeyEventType.KeyUp, Timestamp = 400 },
            new() { Key = VKey.B, EventType = KeyEventType.KeyDown, Timestamp = 500 },
            new() { Key = VKey.B, EventType = KeyEventType.KeyUp, Timestamp = 600 }
        };

        // Act
        var result = recordKeyboard.RemoveExtraKeyDownsForHeldKeys(keyEvents);

        // Assert
        var expected = new List<KeyEvent>
        {
            new() { Key = VKey.A, EventType = KeyEventType.KeyDown, Timestamp = 100 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyUp, Timestamp = 200 },
        };

        // assert each key event is the same
        Assert.Equal(expected.Count, result.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Key, result[i].Key);
            Assert.Equal(expected[i].EventType, result[i].EventType);
            Assert.Equal(expected[i].Timestamp, result[i].Timestamp);
        }
    }
    // add a test to ensure that F2 is trimmed from the top of the output and the bottom of the output
    [Fact]
    public void RemoveExtraKeyDownsForHeldKeys_RemovesExtraKeyDownsBeforeAndAfterF2()
    {
        // Arrange
        var f2StopRecordingAction = new Dictionary<VKey, Action>
        {
            [VKey.F2] = () => { }
        };
        var windowHelper = new WindowHelper();
        var recordKeyboard = new RecordKeyboard(windowHelper, f2StopRecordingAction);
        var keyEvents = new List<KeyEvent>
        {
            new() { Key = VKey.F2, EventType = KeyEventType.KeyUp, Timestamp = 200 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyDown, Timestamp = 300 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyUp, Timestamp = 400 },
            new() { Key = VKey.F2, EventType = KeyEventType.KeyDown, Timestamp = 500 },
            new() { Key = VKey.F2, EventType = KeyEventType.KeyUp, Timestamp = 600 }
        };

        // Act
        var result = recordKeyboard.RemoveExtraKeyDownsForHeldKeys(keyEvents);

        // Assert
        var expected = new List<KeyEvent>
        {
            new() { Key = VKey.A, EventType = KeyEventType.KeyDown, Timestamp = 300 },
            new() { Key = VKey.A, EventType = KeyEventType.KeyUp, Timestamp = 400 },
        };

        // assert each key event is the same
        Assert.Equal(expected.Count, result.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Key, result[i].Key);
            Assert.Equal(expected[i].EventType, result[i].EventType);
            Assert.Equal(expected[i].Timestamp, result[i].Timestamp);
        }
    }
}