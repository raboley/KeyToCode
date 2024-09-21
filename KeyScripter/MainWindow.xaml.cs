using System;
using System.Windows;

namespace KeyScripter
{
    public partial class MainWindow : Window
    {
        private bool isRecording = false;
        private RecordKeyboard _recordKeyboard = new RecordKeyboard();

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

        private void StartRecording(bool start = true)
        {
            _recordKeyboard.StartRecording();
        }
        
        private string StopRecording()
        {
            return _recordKeyboard.StopRecording();
        }
    }
}