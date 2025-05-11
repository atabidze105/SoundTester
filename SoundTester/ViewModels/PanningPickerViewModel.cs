using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using SoundTesting;
using Splat;

namespace SoundTester.ViewModels;

public class PanningPickerViewModel : ViewModelBase
{
    private string _playButtonContent = "Начать проверку";
    private float _panningValue = 0f;
    private int _selectedIndex;
    private string _selectedDevice;
    private ObservableCollection<string> _devices;
    private bool _isPlaying;
    private bool _isEnabled = false;
    
    private WaveOutEvent _waveOut;
    private SignalGenerator _signalGenerator = new SignalGenerator
    {
        Gain = 0.5,
        Frequency = 440,
        Type = SignalGeneratorType.Sin
    };
    private ISampleProvider _sampleProvider;
    
    private DevicesEnumerator _devicesEnumerator;

    public string PlayButtonContent
    {
        get => _playButtonContent;
        set => this.RaiseAndSetIfChanged(ref _playButtonContent, value);
    }

    public float PanningValue
    {
        get => _panningValue;
        set => this.RaiseAndSetIfChanged(ref _panningValue, value);
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedIndex, value);
    }
    
    public string SelectedDevice
    {
        get => _selectedDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedDevice, value);
    }

    public ObservableCollection<string> Devices
    {
        get => _devices;
        set => this.RaiseAndSetIfChanged(ref _devices, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    // public WaveOutEvent WaveOut
    // {
    //     get => _waveOut;
    //     set => this.RaiseAndSetIfChanged(ref _waveOut, value);
    // }
    
    public ICommand PlayCommand { get; }

    public PanningPickerViewModel(DevicesEnumerator? devicesEnumerator = null)
    {
        _devicesEnumerator = devicesEnumerator ?? Locator.Current.GetService<DevicesEnumerator>()!;

        Devices = _devicesEnumerator!.DevicesUpdater.OutputDevices;
        IsEnabled = SelectedIndex == -1 || Devices.Count == 0 ? false : true;

        SelectedDevice = Devices.FirstOrDefault();
        
        PlayCommand = ReactiveCommand.Create( () => Play());
        
        this.WhenAnyValue(x => x.Devices)
            .WhereNotNull()
            .Subscribe(x =>
            {
                SelectedDevice = null!;
                SelectedDevice = Devices?.FirstOrDefault();
                ReloadDevices();
            });

        this.WhenAnyValue(x => x.PanningValue, x => x.SelectedIndex).Subscribe(x =>
        {
            if (IsPlaying)
            {
                IsPlaying = false;
                _waveOut?.Stop();
                PlayButtonContent = "Начать проверку";
                PanningAudioInit();
            }
        });
    }

    private void ReloadDevices() => this.WhenAnyValue(x => x.SelectedDevice).Subscribe(_ =>
    {
        Devices = _devicesEnumerator.DevicesUpdater.OutputDevices;
        IsEnabled = SelectedIndex == -1 || Devices.Count == 0 ? false : true;
        
        
    });

    
    
    private void Play()
    {
        if (_isPlaying)
        {
            _waveOut?.Stop();
            PlayButtonContent = "Начать проверку";
        }
        else
        {
            PanningAudioInit();
            _waveOut.Play();
            PlayButtonContent = "Остановить проверку";
        }
        _isPlaying = !_isPlaying;
    }

    private void PanningAudioInit()
    {
        _waveOut = new WaveOutEvent()
        {
            DeviceNumber = SelectedIndex
        };

        _sampleProvider = _signalGenerator.ToMono();
        PanningSampleProvider panning = new PanningSampleProvider(_sampleProvider);
        panning.Pan = PanningValue;
        _waveOut.Init(panning);
    }
}