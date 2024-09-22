using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using KeyToCode;

namespace KeyScripter
{
    public partial class MainWindow : Window
    {
        private bool isRecording = false;
        private RecordKeyboard _recordKeyboard = new RecordKeyboard();
        private PlaybackKeyboardCode _playbackKeyboard = new PlaybackKeyboardCode();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            isRecording = !isRecording;
            if (isRecording)
            {
                RecordButton.Content = "■ Stop";
                StartRecording();
            }
            else
            {
                RecordButton.Content = "● Record";
                string output = StopRecording();
                OutputTextBox.Text = output;
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            var keyEvents = ParseKeyEvents(OutputTextBox.Text);
            _playbackKeyboard.Play(keyEvents);
        }

        private void StartRecording(bool start = true)
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
                // var key = (Key)Enum.Parse(typeof(Key), parts[1]);
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
}