using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NAudio.Wave;
using Pv;
using System;
using System.Text;

namespace SoundTesting.Views;

public partial class VoiceTrackerControl : UserControl
{
    private bool _isMonitoring = false;
    private string[] _Devices = PvRecorder.GetAvailableDevices();

    private WaveInEvent _waveIn;
    private float _minDb = -96; // Минимальный уровень дб
    private float _currentDb;

    public float CurrentDbValue
    {
        get;
        set;
    }

    public VoiceTrackerControl()
    {
        Decoder();

        InitializeComponent();
        cbox_microphones.ItemsSource = _Devices;
        btn_rec.IsEnabled = cbox_microphones.SelectedIndex == -1 ? false : true;
    }


    private void ComboBox_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (cbox_microphones != null)
        {
            btn_rec.IsEnabled = cbox_microphones.SelectedIndex == -1 ? false : true;

            tblock_mic.Text = _Devices[cbox_microphones.SelectedIndex];
            
            _waveIn?.StopRecording();
            btn_rec.Content = "Начать проверку";
            _waveIn?.Dispose();
            _waveIn = new()
            {
                DeviceNumber = cbox_microphones.SelectedIndex,
                WaveFormat = new WaveFormat(44100, 1), // 44.1 кГц, моно
                BufferMilliseconds = 50 // Буфер 50 мс
            };
            _waveIn.DataAvailable += OnDataAvailable;
        }
    }
    private void Button_StartRec(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isMonitoring)
        {
            _waveIn.StopRecording();
            btn_rec.Content = "Начать проверку";
        }
        else
        {
            _waveIn.StartRecording();
            btn_rec.Content = "Остановить проверку";
        }
        _isMonitoring = !_isMonitoring;
    }


    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        // Преобразуем байты в массив float (от -1.0 до 1.0)
        float[] samples = new float[e.BytesRecorded / 2];
        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short sample = BitConverter.ToInt16(e.Buffer, i);
            samples[i / 2] = sample / 32768f; // Нормализация до [-1, 1]
        }

        // Вычисляем RMS (среднеквадратичное значение)
        float sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        float rms = (float)Math.Sqrt(sum / samples.Length);

        // Переводим RMS в децибелы (дБ)
        _currentDb = rms > 0 ? 20 * (float)Math.Log10(rms) : _minDb;

        progressbar_volume.Value = _currentDb; //Здесь ошибка "Call from invalid thread"
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
}