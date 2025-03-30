using Avalonia.Controls;
using Pv;
using System;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using static SoundTesting.AudioRecorder;
using static SoundTesting.CustomVoiceTracker;
using NAudio.Wave;

namespace SoundTesting
{
    public partial class MainWindow : Window
    {
        private AudioRecorder _Recorder = new AudioRecorder();
        private CustomVoiceTracker _Tracker = new CustomVoiceTracker(1);
        //private float _Volume = -96;
        private bool _isMonitoring = false;

        private string[] _Devices = PvRecorder.GetAvailableDevices();
        public MainWindow()
        {
            Decoder();
            
            InitializeComponent();

            cbox_microphones.ItemsSource = _Devices;

            progressbar_volume.DataContext = _Tracker;
        }
        private void ComboBox_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
        {
            if (cbox_microphones != null)
            {
                btn_rec.IsEnabled = cbox_microphones.SelectedIndex == -1 ? false : true;

                tblock_mic.Text = _Devices[cbox_microphones.SelectedIndex];
                _Tracker = new CustomVoiceTracker(cbox_microphones.SelectedIndex);
            }
        }

        private void Button_StartRec(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_isMonitoring)
            {
                _Tracker.Stop();
                btn_rec.Content = "Начать проверку";
            }
            else
            {
                _Tracker.Start();
                btn_rec.Content = "Остановить проверку";
            }
            _isMonitoring = !_isMonitoring;
        }


        private void Button_Start(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _Recorder.StartRecording(cbox_microphones.SelectedIndex);
            tblock_text.Text = "Запись начата...";
        }

        private void Button_Stop(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _Recorder.StopRecording();
            tblock_text.Text = "Запись остановлена.";
        }

        private void Button_Click_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _Recorder.PlayRecordedAudio();
            tblock_text.Text = "Воспроизведение аудио...";
        }

        private void Decoder() //Преобразование именований устройств в нужную кодировку
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding win1251 = Encoding.GetEncoding("windows-1251");

            Encoding utf8 = Encoding.GetEncoding("UTF-8");
            for (int i = 0; i < _Devices.Length; i++)
            {
                byte[] bytes = win1251.GetBytes(_Devices[i]);

                byte[] utf8Bytes = win1251.GetBytes(_Devices[i]);
                byte[] win1251Bytes = Encoding.Convert(utf8, win1251, utf8Bytes);

                _Devices[i] = (win1251.GetString(win1251Bytes));
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            File.Delete("RecordedAudio/recorded.wav");
            _Tracker?.Dispose();
            base.OnClosed(e);
        }
    }
}