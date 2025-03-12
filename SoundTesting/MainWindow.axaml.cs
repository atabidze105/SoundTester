using Avalonia.Controls;
using Pv;
using System;
using System.IO;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using static SoundTesting.AudioRecorder;
using NAudio.Wave;

namespace SoundTesting
{
    public partial class MainWindow : Window
    {
        private AudioRecorder _Recorder = new AudioRecorder();

        //private PvRecorder _Recorder = PvRecorder.Create(frameLength: 512);
        private string[] _Devices = PvRecorder.GetAvailableDevices();
        public MainWindow()
        {
            Decoder();
            InitializeComponent();
            cbox_microphones.ItemsSource = _Devices;


        }

        private void Button_Start(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            //_Recorder.Start();
            _Recorder.StartRecording(cbox_microphones.SelectedIndex);
            tblock_text.Text = "Запись начата...";
        }

        private void Button_Stop(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            //_Recorder.Stop();
            _Recorder.StopRecording();
            tblock_text.Text = "Запись остановлена.";
        }

        private void Button_Click_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            //short[] frame = _Recorder.Read();
            _Recorder.PlayRecordedAudio();
            tblock_text.Text = "Воспроизведение аудио...";
        }

        private void Decoder()
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


    }
}