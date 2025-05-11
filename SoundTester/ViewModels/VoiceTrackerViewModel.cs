using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using NAudio.Wave;
using ReactiveUI;
using SoundTesting;
using Splat;

namespace SoundTester.ViewModels;

public class VoiceTrackerViewModel : ViewModelBase
{
    private string _selectedDevice;
    private int _selectedDeviceIndex;
    private ObservableCollection<string> _devices;
    private float _volume = -96;
    private WaveInEvent _waveIn;
    private WaveOutEvent _waveOut;
    private bool _isMonitoring = false;
    private bool _isEnabled = false;
    private string _recButtonContent = "Начать проверку";
    private DevicesEnumerator _devicesEnumerator;

    public string SelectedDevice
    {
        get => _selectedDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedDevice, value);
    }

    public int SelectedDeviceIndex
    {
        get => _selectedDeviceIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedDeviceIndex, value);
    }

    public ObservableCollection<string>? Devices
    {
        get => _devices;
        set => this.RaiseAndSetIfChanged(ref _devices, value);
    }

    public float Volume
    {
        get => _volume;
        set => this.RaiseAndSetIfChanged(ref _volume, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public string RecButtonContent
    {
        get => _recButtonContent;
        set => this.RaiseAndSetIfChanged(ref _recButtonContent, value);
    }

    public ICommand RecordCommand { get; }
    

    public VoiceTrackerViewModel(DevicesEnumerator? devicesEnumerator = null) //Конструктор
    {
        _devicesEnumerator = devicesEnumerator ?? Locator.Current.GetService<DevicesEnumerator>()!;

        Devices = _devicesEnumerator!.DevicesUpdater.InputDevices;

        SelectedDevice = Devices.FirstOrDefault();

        RecordCommand = ReactiveCommand.Create(() => Record());
        
        this.WhenAnyValue(x => x.Devices)
            .WhereNotNull()
            .Subscribe(x =>
            {
                SelectedDevice = null!;
                SelectedDevice = Devices?.FirstOrDefault();
                ReloadDevices();
            });
    }
    
    private void ReloadDevices() => this.WhenAnyValue(x => x.SelectedDevice)
        .Subscribe(_ =>
        {
            Devices = _devicesEnumerator.DevicesUpdater.InputDevices;
            IsEnabled = SelectedDeviceIndex == -1 || Devices.Count == 0 ? false : true;

            _waveIn?.StopRecording();
            _waveOut?.Stop();
            _waveIn?.Dispose();
            _waveOut?.Dispose();
            _waveIn = new()
            {
                DeviceNumber = SelectedDeviceIndex,
                WaveFormat = new WaveFormat(44100, 16, 1),
                BufferMilliseconds = 50
            };
            _waveIn.DataAvailable += OnDataAvailable;
            
            Dispatcher.UIThread.InvokeAsync(new Action((() => RefreshProgressBar(-96))));
            RecButtonContent = "Начать проверку";
            _isMonitoring = false;
        });

    
    private void Record()
    {
        if (_isMonitoring)
        {
            _waveIn?.StopRecording();
            _waveOut.Stop();
            RecButtonContent = "Начать проверку";
            Dispatcher.UIThread.InvokeAsync(new Action((() => RefreshProgressBar(-96))));
        }
        else
        {
            var provider = new BufferedWaveProvider(_waveIn?.WaveFormat){ DiscardOnBufferOverflow = true };
            _waveOut = new WaveOutEvent();
            _waveOut.Init(provider);
            _waveIn.DataAvailable += (sender, e) =>
            {
                provider.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };
            _waveIn?.StartRecording();
            _waveOut.Play();

            RecButtonContent = "Остановить проверку";
        }

        _isMonitoring = !_isMonitoring;
    }


    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        float[] samples = new float[e.BytesRecorded / 2];
        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short sample = BitConverter.ToInt16(e.Buffer, i);
            samples[i / 2] = sample / 32768f;
        }

        float sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }

        float rms = (float)Math.Sqrt(sum / samples.Length);

        float res = rms > 0 ? 20 * (float)Math.Log10(rms) : -96;
        Dispatcher.UIThread.InvokeAsync(new Action((() => RefreshProgressBar(res))));
    }

    private void RefreshProgressBar(float percentage)
    {
        Volume = percentage;
    }

    // private int GetDeviceIndex(string? deviceName)
    // {
    //     
    //     for (int i = 0; i < WaveInEvent.DeviceCount; i++)
    //     {
    //         var caps = WaveInEvent.GetCapabilities(i);
    //         if (deviceName.Contains(caps.ProductName)) 
    //             return i;
    //     }
    //     return -1;
    // }
}