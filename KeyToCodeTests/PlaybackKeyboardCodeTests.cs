using System.Diagnostics;
using KeyToCode;
using Xunit;

namespace KeyScripterTests;

public class PlaybackKeyboardCodeTests
{
    private const string TestFileFullPathTypesKeysIntoNotepad = @"C:\Temp\TypesKeysIntoNotepad.txt";
    private readonly PlaybackKeyboardCode _keyboard;
    private readonly Process _notepadProcess;

    public PlaybackKeyboardCodeTests()
    {
        _notepadProcess = new Process { StartInfo = { FileName = "notepad.exe" } };
        var psi = new ProcessStartInfo("Notepad.Exe", TestFileFullPathTypesKeysIntoNotepad);
        _notepadProcess.StartInfo = psi;
        _keyboard = new PlaybackKeyboardCode();
    }

    [Fact]
    public void PlaybackKeyboardCode_Play_TypesKeysIntoNotepad()
    {
        // Arrange
        // create test file with no content
        File.WriteAllText(TestFileFullPathTypesKeysIntoNotepad, string.Empty);
        var expected = "ab";
        _notepadProcess.Start();
        _notepadProcess.WaitForInputIdle();
        _keyboard.Connect(_notepadProcess.MainWindowHandle);

        // Act
        //// Type "ab" into notepad
        _keyboard.KeyDown(VKey.A);
        _keyboard.Sleep(50);
        _keyboard.KeyUp(VKey.A);
        _keyboard.Sleep(50);
        _keyboard.KeyDown(VKey.B);
        _keyboard.Sleep(50);
        _keyboard.KeyUp(VKey.B);
        _keyboard.Sleep(50);

        //// Save the file
        _keyboard.KeyDown(VKey.Lcontrol);
        _keyboard.Sleep(50);
        _keyboard.KeyDown(VKey.S);
        _keyboard.Sleep(50);
        _keyboard.KeyUp(VKey.S);
        _keyboard.Sleep(50);
        _keyboard.KeyUp(VKey.Lcontrol);
        _keyboard.Sleep(50);

        // Assert
        _notepadProcess.CloseMainWindow();
        _notepadProcess.Kill();
        _notepadProcess.WaitForExit();

        var actual = File.ReadAllText(TestFileFullPathTypesKeysIntoNotepad);
        Assert.Equal(expected, actual);
    }
}