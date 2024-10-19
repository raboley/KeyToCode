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
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace KeyScripter;

public partial class MainWindow : INotifyPropertyChanged
{
    private readonly PlaybackKeyboardCode _playbackKeyboard;
    private readonly RecordKeyboard _recordKeyboard;
    private bool _isRecording;
    private const string ConfigFilePath = "config.json";
    private string ConfigFileFullPath => Path.GetFullPath(ConfigFilePath);
    private Config _config;
    private string? _currentFilePath;
    private FileSystemWatcher _configWatcher;

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    public static MainWindow Instance { get; private set; }
    private static Dictionary<string, VKey> _keyActions;

    public string PlayKeyBinding { get; set; } = "";
    public string RecordKeyBinding { get; set; } = "";

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        var windowHelper = new WindowHelper();

        DataContext = this;
        PopulateProcessComboBox();
        LoadConfig();
        InitializeKeyActions();

        PlayKeyBinding = _keyActions["togglePlayStartStop"].ToString();
        RecordKeyBinding = _keyActions["toggleRecordingStartStop"].ToString();

        var keyActions = new Dictionary<VKey, Action>
        {
            [_keyActions["toggleRecordingStartStop"]] = () => { },
            [_keyActions["togglePlayStartStop"]] = () => { }
        };

        _recordKeyboard = new RecordKeyboard(windowHelper, keyActions);
        _playbackKeyboard = new PlaybackKeyboardCode(windowHelper);

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

        // Set the global keyboard hook
        _hookID = SetHook(_proc);
        InitializeConfigWatcher();
    }

    private void InitializeConfigWatcher()
    {
        _configWatcher = new FileSystemWatcher
        {
            Path = Path.GetDirectoryName(ConfigFileFullPath),
            Filter = Path.GetFileName(ConfigFileFullPath),
            NotifyFilter = NotifyFilters.LastWrite
        };

        _configWatcher.Changed += OnConfigFileChanged;
        _configWatcher.EnableRaisingEvents = true;
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LoadConfig();
            InitializeKeyActions();

            PlayKeyBinding = _keyActions["togglePlayStartStop"].ToString();
            RecordKeyBinding = _keyActions["toggleRecordingStartStop"].ToString();

            OnPropertyChanged(nameof(PlayKeyBinding));
            OnPropertyChanged(nameof(RecordKeyBinding));
        });
    }

    private void InitializeKeyActions()
    {
        if (_config.KeyActions == null)
        {
            _config.KeyActions = new Dictionary<string, string>();
        }

        AddDefaultKeyAction("toggleRecordingStartStop", VKey.F4);
        AddDefaultKeyAction("togglePlayStartStop", VKey.F5);

        SetKeyActions();

        WriteConfig();
    }

    private void AddDefaultKeyAction(string actionName, VKey defaultKey)
    {
        if (!_config.KeyActions.ContainsKey(actionName))
        {
            _config.KeyActions[actionName] = defaultKey.ToString();
        }
    }

    private void LoadConfig()
    {
        _config = GetConfig() ?? new Config();
        if (_config.KeyActions != null)
        {
            SetKeyActions();
        }

        OnPropertyChanged(nameof(AutomaticallySelectLastProcess));
        OnPropertyChanged(nameof(AutomaticallyCopyOutputToClipboard));
        OnPropertyChanged(nameof(AutomaticallyOpenLastSavedFile));
        OnPropertyChanged(nameof(LoopPlayback));
    }

    private void SetKeyActions()
    {
        _keyActions = _config.KeyActions.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var keyEnum = Enum.TryParse(typeof(VKey), kvp.Value, out var key);
                if (!keyEnum)
                    return VKey.Null;

                return (VKey)key;
            });
    }

    private void WriteConfig()
    {
        _config.KeyActions = _keyActions.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToString()
        );

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(_config, options);
        File.WriteAllText(ConfigFilePath, json);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            var toggleRecordingStartStopKey = _keyActions["toggleRecordingStartStop"];
            var togglePlayStartStopKey = _keyActions["togglePlayStartStop"];

            if (vkCode == (int)toggleRecordingStartStopKey)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow.Instance.RecordButton_Click(MainWindow.Instance.RecordButton, new RoutedEventArgs());
                });
            }
            else if (vkCode == (int)togglePlayStartStopKey)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow.Instance.PlayButton_Click(MainWindow.Instance.PlayButton, new RoutedEventArgs());
                });
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    protected override void OnClosed(EventArgs e)
    {
        UnhookWindowsHookEx(_hookID);
        base.OnClosed(e);
    }

    private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = IsUnsavedWork();
    }

    private void SaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = !string.IsNullOrEmpty(OutputTextBox.Text);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // open the directory where the settings file is stored 
        Process.Start("explorer.exe", "/select," + ConfigFilePath);
    }

    private void OutputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        CommandManager.InvalidateRequerySuggested();
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

    private void ProcessComboBox_DropDownOpened(object sender, EventArgs e)
    {
        // get current selected process
        var selectedProcess = ProcessComboBox.SelectedItem as dynamic;
        PopulateProcessComboBox();
        ProcessComboBox.SelectedValue = selectedProcess?.Id;
    }

    private void StopRecordingAction()
    {
        RecordButton_Click(this, new RoutedEventArgs());
    }

    private RecordingOverlay _recordingOverlay;

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        _isRecording = !_isRecording;
        if (_isRecording)
        {
            RecordButton.Content = "■ Stop";
            RecordButton.Background = new SolidColorBrush(Colors.Tomato); // Lighter hue for stop mode
            StartRecording();

            // Show the recording overlay
            _recordingOverlay = new RecordingOverlay();
            _recordingOverlay.Show();
        }
        else
        {
            RecordButton.Content = "● Record";
            RecordButton.Background = new SolidColorBrush(Colors.Red); // Normal color
            // Stop recording logic
            var output = StopRecording();
            OutputTextBox.Text = output;
            if (_config.AutomaticallyCopyOutputToClipboard)
            {
                Clipboard.SetText(output);
            }

            // Hide the recording overlay
            if (_recordingOverlay != null)
            {
                _recordingOverlay.Close();
                _recordingOverlay = null;
            }
        }
    }

    private void StartRecording()
    {
        _playbackKeyboard.SetForegroundWindow();
        _recordKeyboard.StartRecording();
    }

    private string StopRecording()
    {
        return _recordKeyboard.StopRecording();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(OutputTextBox.Text);
    }

    private bool _isPlaying = false;

    private async void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        _isPlaying = !_isPlaying;
        if (_isPlaying)
        {
            PlayButton.Content = "■ Stop";
            PlayButton.Background = new SolidColorBrush(Colors.MediumSeaGreen); // Lighter hue for stop mode
            var inputKeys = OutputTextBox.Text;

            do
            {
                await Task.Run(() => _playbackKeyboard.Play(inputKeys));
            } while (_config.LoopPlayback && _isPlaying);

            _isPlaying = false;
            PlayButton.Content = "▶ Play";
            PlayButton.Background = new SolidColorBrush(Colors.Green); // Normal color
        }
        else
        {
            _playbackKeyboard.Stop();
            PlayButton.Content = "▶ Play";
            PlayButton.Background = new SolidColorBrush(Colors.Green); // Normal color
        }
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

    private static Config? GetConfig()
    {
        return RetryOnIOException(() =>
        {
            if (!File.Exists(ConfigFilePath))
                return null;

            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<Config>(json);
            return config;
        });
    }

    private static T RetryOnIOException<T>(Func<T> func, int maxRetries = 20, int delayMilliseconds = 1000)
    {
        int retries = 0;
        while (true)
        {
            try
            {
                return func();
            }
            catch (Exception e) when (retries < maxRetries)
            {
                retries++;
                Task.Delay(delayMilliseconds).Wait();
                Console.WriteLine("exception when reading config file, retrying...");
                Console.WriteLine("exception is: " + e.Message);
            }
        }
    }

    private void ToggleLoopButton_Click(object sender, RoutedEventArgs e)
    {
        LoopPlayback = !LoopPlayback;

        if (LoopPlayback)
        {
            ToggleLoopButton.Background = Brushes.Green;
            ToggleLoopButton.Foreground = Brushes.White;
        }
        else
        {
            ToggleLoopButton.Background = Brushes.LightGray;
            ToggleLoopButton.Foreground = Brushes.Black;
        }
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

    public bool LoopPlayback
    {
        get => _config.LoopPlayback;
        set
        {
            if (_config.LoopPlayback != value)
            {
                _config.LoopPlayback = value;
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
            var result = MessageBox.Show("You have unsaved work. Do you want to discard it?", "Warning",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
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