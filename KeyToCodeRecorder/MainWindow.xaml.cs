using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using KeyToCode;
using System.Windows.Interop;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace KeyScripter;

public partial class MainWindow : INotifyPropertyChanged
{
    private readonly PlaybackKeyboardCode _playbackKeyboard = new();
    private readonly RecordKeyboard _recordKeyboard = new();
    private bool _isRecording;
    private const string ConfigFilePath = "config.json";
    private Config _config;
    private string? _currentFilePath;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        PopulateProcessComboBox();
        LoadConfig();
        if (_config == null)
        {
            _config = new Config();
            WriteConfig();
            return;
        }
        
        if (_config.AutomaticallySelectLastProcess)
            LoadSelectedProcess();

        if (_config.AutomaticallyOpenLastSavedFile)
        {
            _currentFilePath = _config.LastSavedFilePath;
            if (File.Exists(_currentFilePath))
            {
                OutputTextBox.Text = File.ReadAllText(_currentFilePath);
            }
        }
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
            if (_config.AutomaticallySelectLastProcess)
            {
                SaveSelectedProcess(selectedProcess.ProcessName);
            }
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
            if (_config.AutomaticallyCopyOutputToClipboard)
            {
                Clipboard.SetText(output);
            }
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(OutputTextBox.Text);
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

    private void SaveSelectedProcess(string processName)
    {
        _config.LastSelectedProcessName = processName;
        WriteConfig();
    }

    private void LoadSelectedProcess()
    {
        var process = Process.GetProcesses().FirstOrDefault(p =>
            p.ProcessName == _config.LastSelectedProcessName && p.MainWindowHandle != IntPtr.Zero);
        if (process != null)
        {
            ProcessComboBox.SelectedValue = process.Id;
        }
    }

    private void LoadConfig()
    {
        _config = GetConfig() ?? new Config();
        OnPropertyChanged(nameof(AutomaticallySelectLastProcess));
        OnPropertyChanged(nameof(AutomaticallyCopyOutputToClipboard));
        OnPropertyChanged(nameof(AutomaticallyOpenLastSavedFile));
    }

    private void WriteConfig()
    {
        var json = JsonSerializer.Serialize(_config);
        File.WriteAllText(ConfigFilePath, json);
    }

    private static Config? GetConfig()
    {
        if (!File.Exists(ConfigFilePath))
            return null;

        var json = File.ReadAllText(ConfigFilePath);
        var config = JsonSerializer.Deserialize<Config>(json);
        return config;
    }

    public bool AutomaticallySelectLastProcess
    {
        get => _config.AutomaticallySelectLastProcess;
        set
        {
            if (_config.AutomaticallySelectLastProcess != value)
            {
                _config.AutomaticallySelectLastProcess = value;
                OnPropertyChanged();
                WriteConfig();
            }
        }
    }

    public bool AutomaticallyCopyOutputToClipboard
    {
        get => _config.AutomaticallyCopyOutputToClipboard;
        set
        {
            if (_config.AutomaticallyCopyOutputToClipboard != value)
            {
                _config.AutomaticallyCopyOutputToClipboard = value;
                OnPropertyChanged();
                WriteConfig();
            }
        }
    }

    public bool AutomaticallyOpenLastSavedFile
    {
        get => _config.AutomaticallyOpenLastSavedFile;
        set
        {
            if (_config.AutomaticallyOpenLastSavedFile != value)
            {
                _config.AutomaticallyOpenLastSavedFile = value;
                OnPropertyChanged();
                WriteConfig();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private class Config
    {
        public string? LastSelectedProcessName { get; set; }
        public string? LastSavedFilePath { get; set; }
        public bool AutomaticallySelectLastProcess { get; set; } = true;
        public bool AutomaticallyCopyOutputToClipboard { get; set; } = true;

        public bool AutomaticallyOpenLastSavedFile { get; set; } = false;
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _currentFilePath = openFileDialog.FileName;
            _config.LastSavedFilePath = _currentFilePath;
            OutputTextBox.Text = File.ReadAllText(_currentFilePath);
        }
    }

    private void SaveFile_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            SaveAsFile_Click(sender, e);
        }
        else
        {
            File.WriteAllText(_currentFilePath, OutputTextBox.Text);
            _config.LastSavedFilePath = _currentFilePath;
            WriteConfig();
        }
    }

    private void SaveAsFile_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            _currentFilePath = saveFileDialog.FileName;
            _config.LastSavedFilePath = _currentFilePath;
            WriteConfig();
            File.WriteAllText(_currentFilePath, OutputTextBox.Text);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        if (PreventLosingWork()) 
            return;
        
        Application.Current.Shutdown();
    }
    
    private void NewFile_Click(object sender, RoutedEventArgs e)
    {
        if (PreventLosingWork()) 
            return;

        OutputTextBox.Clear();
        _currentFilePath = null;
    }

    private bool PreventLosingWork()
    {
        if (IsUnsavedWork())
        {
            var result = MessageBox.Show("You have unsaved work. Do you want to discard it?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsUnsavedWork()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            return !string.IsNullOrEmpty(OutputTextBox.Text);
        }

        var currentContent = File.ReadAllText(_currentFilePath);
        return currentContent != OutputTextBox.Text;
    }
}