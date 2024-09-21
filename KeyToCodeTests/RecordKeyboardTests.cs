using System.Windows.Input;
using Xunit;

namespace KeyScripter.Tests
{
    public class RecordKeyboardTests
    {
        [Fact]
        public void TranslateInput_ReturnsCorrectString()
        {
            // Arrange
            var recordKeyboard = new RecordKeyboard();
            var keyEvents = new List<KeyEvent>
            {
                new KeyEvent { Key = Key.A, EventType = KeyEventType.KeyDown, Timestamp = 100 },
                new KeyEvent { Key = Key.A, EventType = KeyEventType.KeyUp, Timestamp = 200 },
                new KeyEvent { Key = Key.B, EventType = KeyEventType.KeyDown, Timestamp = 300 },
                new KeyEvent { Key = Key.B, EventType = KeyEventType.KeyUp, Timestamp = 400 }
            };

            // Act
            var result = recordKeyboard.TranslateToCSharp(keyEvents);

            // Assert
            var expected = "KeyDown A at 100ms\nKeyUp A at 200ms\nKeyDown B at 300ms\nKeyUp B at 400ms";
            Assert.Equal(expected, result);
        }
    }
}