using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using SoundTesting;
using Splat;

namespace SoundTester.ViewModels;

public class PanningPickerViewModel : ViewModelBase //Тестер панорамирования
{
    private string _playButtonContent = "Начать проверку";
    private string _selectedDevice;
    private ObservableCollection<string> _devices;
    private float _panningValue = 0f;
    private int _selectedIndex;
    private bool _isPlaying;
    private bool _isEnabled = false;
    private WasapiOut _wasapiOut;
    
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
    
    public ICommand PlayCommand { get; }

    public PanningPickerViewModel(DevicesEnumerator? devicesEnumerator = null) //конструктор
    {
        _devicesEnumerator = devicesEnumerator ?? Locator.Current.GetService<DevicesEnumerator>()!;

        Devices = _devicesEnumerator!.DevicesUpdater.OutputDevices;
        IsEnabled = SelectedIndex == -1 || Devices.Count == 0 ? false : true;

        SelectedDevice = Devices.FirstOrDefault();
        
        PlayCommand = ReactiveCommand.Create( () => Play());
        
        this.WhenAnyValue(x => x.Devices, x => x.SelectedDevice)
            .WhereNotNull()
            .Subscribe(x =>
            {
                Devices = _devicesEnumerator.DevicesUpdater.OutputDevices;
                IsEnabled = SelectedIndex == -1 || Devices.Count == 0 ? false : true;
            });

        this.WhenAnyValue(x => x.PanningValue, x => x.SelectedIndex).Subscribe(x =>
        {
            if (IsPlaying)
            {
                IsPlaying = false;
                
                _wasapiOut?.Stop();
                
                PlayButtonContent = "Начать проверку";
                
                PanningAudioInit();
            }
        });
    }
    
    private void Play() //Запуск/остановка тестера
    {
        if (_isPlaying)
        {
            _wasapiOut?.Stop();
            
            PlayButtonContent = "Начать проверку";
        }
        else
        {
            PanningAudioInit();
            
            _wasapiOut.Play();
            
            PlayButtonContent = "Остановить проверку";
        }
        _isPlaying = !_isPlaying;
    }
    
    private void PanningAudioInit() //Инициализация тестера
    {
        _wasapiOut = null;
        
        var en = new MMDeviceEnumerator(); //костыль
        var outD = en.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).Where(x => x.FriendlyName == SelectedDevice).FirstOrDefault();
        
        _wasapiOut = new WasapiOut(outD,AudioClientShareMode.Shared, false, 50); //Инициализация WasapiOut с помощью SelectedDevice не работает т.к. не удается корректно привести объект к интерфейсу IMMDevice

        _sampleProvider = _signalGenerator.ToMono();
        PanningSampleProvider panning = new PanningSampleProvider(_sampleProvider);
        panning.Pan = PanningValue;
        _wasapiOut.Init(panning);
        
        en.Dispose();
    }
}