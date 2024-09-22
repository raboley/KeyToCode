using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using KeyToCode;
using System.Windows.Interop;

namespace KeyScripter;

public partial class MainWindow
{
    private readonly PlaybackKeyboardCode _playbackKeyboard = new();
    private readonly RecordKeyboard _recordKeyboard = new();
    private bool _isRecording;

    public MainWindow()
    {
        InitializeComponent();
        PopulateProcessComboBox();
    }

    private void PopulateProcessComboBox()
    {
        var processes = Process.GetProcesses()
            .Where(p => p.MainWindowHandle != IntPtr.Zero)
            .Select(p => new
            {
                p.ProcessName,
                p.Id,
                Icon = GetProcessIcon(p)
            })
            .ToList();

        ProcessComboBox.ItemsSource = processes;
        ProcessComboBox.SelectedValuePath = "Id";
    }

    private BitmapSource? GetProcessIcon(Process process)
    {
        IntPtr hIcon = IntPtr.Zero;
        try
        {
            hIcon = ExtractIcon(IntPtr.Zero, process.MainModule.FileName, 0);
            if (hIcon == IntPtr.Zero)
                return null;

            return Imaging.CreateBitmapSourceFromHIcon(
                hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        catch
        {
            return null;
        }
        finally
        {
            if (hIcon != IntPtr.Zero)
                DestroyIcon(hIcon);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private void ProcessComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProcessComboBox.SelectedItem is not null && ProcessComboBox.SelectedValue is int selectedProcessId)
        {
            var selectedProcess = Process.GetProcessById(selectedProcessId);
            _playbackKeyboard.Connect(selectedProcess.MainWindowHandle);
        }
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        _isRecording = !_isRecording;
        if (_isRecording)
        {
            RecordButton.Content = "■ Stop";
            StartRecording();
        }
        else
        {
            RecordButton.Content = "● Record";
            var output = StopRecording();
            OutputTextBox.Text = output;
        }
    }

    private async void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        var inputKeys = OutputTextBox.Text;
        await _playbackKeyboard.Play(inputKeys);
    }

    private void StartRecording()
    {
        _recordKeyboard.StartRecording();
    }

    private string StopRecording()
    {
        return _recordKeyboard.StopRecording();
    }

    private List<KeyEvent> ParseKeyEvents(string input)
    {
        var keyEvents = new List<KeyEvent>();
        var lines = input.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(' ');
            if (parts.Length < 4) continue;

            var eventType = (KeyEventType)Enum.Parse(typeof(KeyEventType), parts[0]);
            var key = (VKey)Enum.Parse(typeof(VKey), parts[1]);
            var timestamp = long.Parse(parts[3].Replace("ms", ""));

            keyEvents.Add(new KeyEvent
            {
                EventType = eventType,
                Key = key,
                Timestamp = timestamp
            });
        }

        return keyEvents;
    }
}