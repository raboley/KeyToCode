using System.Windows.Input;
using KeyScripter;
using Xunit;

namespace KeyScripterTests
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
            var expected = """
                           KeyDown(Keys.A);
                           Sleep(100);
                           KeyUp(Keys.A);
                           Sleep(100);
                           KeyDown(Keys.B);
                           Sleep(100);
                           KeyUp(Keys.B);
                           Sleep(100);
                           """;
            Assert.Equal(expected, result);
        }
        
        [Fact]
        public void TranslateKeyToString_TranslatesKeyToCSharpCode()
        {
            // Arrange
            var recordKeyboard = new RecordKeyboard();

            // Act
            var result = recordKeyboard.TranslateKeyToString(Key.A, KeyEventType.KeyDown);

            // Assert
            Assert.Equal("KeyDown(Keys.A);", result);
        }
        
        [Fact]
        public void CalculateSleepTime_ReturnsCorrectSleepTime()
        {
            // Arrange
            var recordKeyboard = new RecordKeyboard();
            // Act
            var result = recordKeyboard.CalculateSleepTime(100, 300);

            // Assert
            Assert.Equal("Sleep(200);", result);
        }
    }
}