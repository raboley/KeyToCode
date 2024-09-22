using System.Diagnostics;
using System.Windows.Input;
using KeyScripter;
using KeyToCode;
using Xunit;

namespace KeyScripterTests;

public class PlaybackKeyboardCodeTests
{
    private readonly Process _notepadProcess;
    private string _testFileFullPath_TypesKeysIntoNotepad = @"C:\Temp\TypesKeysIntoNotepad.txt";
    private readonly PlaybackKeyboardCode _keyboard;

    public PlaybackKeyboardCodeTests()
    {
        
        _notepadProcess = new Process {StartInfo = {FileName = "notepad.exe"}};
        ProcessStartInfo psi = new ProcessStartInfo("Notepad.Exe", _testFileFullPath_TypesKeysIntoNotepad);
        _notepadProcess.StartInfo = psi;
         _keyboard = new PlaybackKeyboardCode();

    }
    
    [Fact]
    public void PlaybackKeyboardCode_Play_TypesKeysIntoNotepad()
    {
        // Arrange
        // create test file with no content
        File.WriteAllText(_testFileFullPath_TypesKeysIntoNotepad, string.Empty);
        var expected = "ab";
        _notepadProcess.Start();
        _notepadProcess.WaitForInputIdle();
        _keyboard.Connect(_notepadProcess.MainWindowHandle);
        
        // Act
        _keyboard.KeyDown(VKeys.KEY_A);
        _keyboard.Sleep(50);
        _keyboard.KeyUp(VKeys.KEY_A);
        _keyboard.Sleep(50);
        _keyboard.KeyDown(VKeys.KEY_B);
        _keyboard.Sleep(50);
        _keyboard.KeyUp(VKeys.KEY_B);
        _keyboard.Sleep(50);
        _keyboard.KeyDown(VKeys.KEY_LCONTROL);
        _keyboard.Sleep(50);
        _keyboard.KeyDown(VKeys.KEY_S);
        _keyboard.Sleep(50);
        _keyboard.KeyUp(VKeys.KEY_S);
        _keyboard.Sleep(50);
        _keyboard.KeyUp(VKeys.KEY_LCONTROL);
        _keyboard.Sleep(50);

        // Assert
        // Save the file
        
        
        _notepadProcess.CloseMainWindow();
        _notepadProcess.Kill();
        _notepadProcess.WaitForExit();
        
        var actual = File.ReadAllText(_testFileFullPath_TypesKeysIntoNotepad);
        // reset the file
        Assert.Equal(expected, actual);
    }
    
}