using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;

namespace SoundTesting.Views;

public partial class PanningControl : UserControl //Панорамирование
{
    private bool _isPlaying = false;
    private List<DirectSoundDeviceInfo> _devices = new List<DirectSoundDeviceInfo>();
    private WaveOutEvent? _waveOut;
    private SignalGenerator _signalGenerator = new SignalGenerator
    {
        Gain = 0.5,
        Frequency = 440,
        Type = SignalGeneratorType.Sin
    };
    ISampleProvider _sampleProvider;


    public float Panning
    {
        get;
        set;
    }

    public PanningControl()
    {
        InitializeComponent();
        DataContext = this;

        foreach (DirectSoundDeviceInfo device in DirectSoundOut.Devices)
        {
            if (device.Description != "Первичный звуковой драйвер")
                _devices.Add(device);
        }
        cbox_devices.ItemsSource = _devices;
    }

    private void PanningAudioTest(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (cbox_devices.SelectedIndex != -1 && cbox_devices.SelectedItem != null)
            if (_isPlaying == false)
            {
                _isPlaying = true;
                btn_panTest.Content = "Остановить проверку";

                _waveOut = new WaveOutEvent()
                {
                    DeviceNumber = cbox_devices.SelectedIndex
                };

                _sampleProvider = _signalGenerator.ToMono();
                PanningSampleProvider panning = new PanningSampleProvider(_sampleProvider);
                panning.Pan = Panning;
                _waveOut.Init(panning);
                _waveOut.Play();
            }
            else
            {
                _waveOut?.Stop();
                _isPlaying = false;
                btn_panTest.Content = "Начать проверку";
            }
    }

    private void Slider_PanningChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isPlaying)
        {
            _waveOut?.Stop();
            _waveOut = new WaveOutEvent()
            {
                DeviceNumber = cbox_devices.SelectedIndex
            };

            _sampleProvider = _signalGenerator.ToMono();
            PanningSampleProvider panning = new PanningSampleProvider(_sampleProvider);
            panning.Pan = Panning;
            _waveOut.Init(panning);
            _waveOut.Play();
        }
    }
}