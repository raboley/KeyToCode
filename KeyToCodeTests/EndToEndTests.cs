using System.Diagnostics;
using System.IO;
using KeyToCode;
using KeyToCodeKeyboard;
using Xunit;

namespace KeyToCodeTests;
public class EndToEndTests : IAsyncLifetime
{
    private Process _notepadProcess;
    private IntPtr _notepadHandle;
    private PlaybackKeyboardCode _playbackKeyboard;
    private RecordKeyboard _recordKeyboard;
    private const string TestFileFullPathTypesKeysIntoNotepad = @"C:\Temp\hello.txt";

    public async Task InitializeAsync()
    {
        // Start Notepad
        _notepadProcess = new Process { StartInfo = { FileName = "notepad.exe" } };
        var psi = new ProcessStartInfo("Notepad.Exe", TestFileFullPathTypesKeysIntoNotepad);
        _notepadProcess.StartInfo = psi;
        var windowHelper = new WindowHelper();
        
        _playbackKeyboard = new PlaybackKeyboardCode(windowHelper);
        
        var keyActions = new Dictionary<VKey, Action>
        {
            [VKey.F2] = StopRecordingAction
        };

        _recordKeyboard = new RecordKeyboard(windowHelper, keyActions);
    }

    public Task DisposeAsync()
    {
        // Close Notepad
        if (!_notepadProcess.HasExited)
        {
            _notepadProcess.Kill();
        }
        return Task.CompletedTask;
    }

    [Fact]
    public void Test_StopRecordingWithF5()
    {
        // Start recording
        File.WriteAllText(TestFileFullPathTypesKeysIntoNotepad, string.Empty);
        var expected = "hello";
        _notepadProcess.Start();
        _notepadProcess.WaitForInputIdle();
        _playbackKeyboard.Connect(_notepadProcess.MainWindowHandle); 
        
        _recordKeyboard.StartRecording();

        // Simulate typing "hello"
         _playbackKeyboard.KeyDown(VKey.H);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyUp(VKey.H);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyDown(VKey.E);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyUp(VKey.E);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyDown(VKey.L);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyUp(VKey.L);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyDown(VKey.L);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyUp(VKey.L);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyDown(VKey.O);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyUp(VKey.O);
         _playbackKeyboard.Sleep(50);

        // Simulate pressing F5 to stop recording
         _playbackKeyboard.KeyDown(VKey.F2);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyUp(VKey.F2);
         _playbackKeyboard.Sleep(50);

        // Simulate typing "world"
         _playbackKeyboard.KeyDown(VKey.W);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyUp(VKey.W);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyDown(VKey.O);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyUp(VKey.O);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyDown(VKey.R);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyUp(VKey.R);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyDown(VKey.L);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyUp(VKey.L);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyDown(VKey.D);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyUp(VKey.D);
         _playbackKeyboard.Sleep(50);

        // Save the document
         _playbackKeyboard.KeyDown(VKey.Control);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyDown(VKey.S);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyUp(VKey.S);
         _playbackKeyboard.Sleep(50);
         _playbackKeyboard.KeyUp(VKey.Control);
         _playbackKeyboard.Sleep(50);

          
        // Verify the content of the saved file
        var actual = File.ReadAllText(TestFileFullPathTypesKeysIntoNotepad);
        Assert.Equal(expected, actual);
    }

    private void StopRecordingAction()
    {
        _recordKeyboard.StopRecording();
        // Save the document
        _playbackKeyboard.KeyDown(VKey.Control);
        _playbackKeyboard.Sleep(50);
        _playbackKeyboard.KeyDown(VKey.S);
        _playbackKeyboard.Sleep(50);
        _playbackKeyboard.KeyUp(VKey.S);
        _playbackKeyboard.Sleep(50);
        _playbackKeyboard.KeyUp(VKey.Control);
        _playbackKeyboard.Sleep(50);
        if (!_notepadProcess.HasExited)
        {
            _notepadProcess.Kill();
        }
    }
}