using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;

namespace SoundTester.ViewModels;

public class OscillatorItemViewModel : ViewModelBase //Осциллятор  (генератор волны)
{
    private WasapiOut _waveOut;
    private SignalGenerator _signalGenerator;
    private ISampleProvider _sampleProvider;
    private SignalGeneratorType _typeRef;
    private int _deviceIndex;
    private float _frequency = 20f;
    private bool _sin = true;
    private bool _square = false;
    private bool _sawtooth = false;
    private bool _triangle = false;
    private bool _noise = false;
    private bool _isPlaying = false;
    private string _itemName;
    private string _groupKey;

    public int DeviceIndex
    {
        get => _deviceIndex;
        set => this.RaiseAndSetIfChanged(ref _deviceIndex, value);
    }

    [Range(20, 20000)]
    public float Frequency
    {
        get => _frequency;
        set => this.RaiseAndSetIfChanged(ref _frequency, value);
    }

    public bool Sin
    {
        get => _sin;
        set => this.RaiseAndSetIfChanged(ref _sin, value);
    }

    public bool Square
    {
        get => _square;
        set => this.RaiseAndSetIfChanged(ref _square, value);
    }

    public bool Sawtooth
    {
        get => _sawtooth;
        set => this.RaiseAndSetIfChanged(ref _sawtooth, value);
    }

    public bool Triangle
    {
        get => _triangle;
        set => this.RaiseAndSetIfChanged(ref _triangle, value);
    }

    public bool Noise
    {
        get => _noise;
        set => this.RaiseAndSetIfChanged(ref _noise, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }

    public SignalGeneratorType SGType
    {
        get => SGTypeSelection();
        set => this.RaiseAndSetIfChanged(ref _typeRef, value);
    }

    public string ItemName
    {
        get => _itemName;
        set => this.RaiseAndSetIfChanged(ref _itemName, value);
    }

    private SignalGeneratorType SGTypeSelection() //Выбор типа волны в зависимости от булевых знаечний
    {
        if (Square)
            return SignalGeneratorType.Square;
        else if (Sawtooth)
            return SignalGeneratorType.SawTooth;
        else if (Triangle)
            return SignalGeneratorType.Triangle;
        else if (Noise)
            return SignalGeneratorType.White;

        return SignalGeneratorType.Sin;
    }

    public WasapiOut WaveOut
    {
        get => _waveOut;
        set => this.RaiseAndSetIfChanged(ref _waveOut, value);
    }

    public SignalGenerator SignalGenerator1
    {
        get => _signalGenerator;
        set => this.RaiseAndSetIfChanged(ref _signalGenerator, value);
    }

    public ISampleProvider SampleProvider
    {
        get => _sampleProvider;
        set => this.RaiseAndSetIfChanged(ref _sampleProvider, value);
    }

    public string GroupKey
    {
        get => _groupKey;
        set => this.RaiseAndSetIfChanged(ref _groupKey, value);
    }

    private void WaveOutInit(string audioDevice) //Инициализация осциллятора
    {
        var en = new MMDeviceEnumerator(); //костыль
        var outD = en.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).Where(x => x.FriendlyName == audioDevice).FirstOrDefault();
        
        WaveOut?.Stop();
        WaveOut?.Dispose();
        WaveOut = new WasapiOut(outD,AudioClientShareMode.Shared, false, 50);

        SignalGenerator1 = new SignalGenerator();
        SignalGenerator1.Frequency = Frequency;
        SignalGenerator1.Type = SGType;
        SignalGenerator1.Gain = 0.5;

        SampleProvider = SignalGenerator1.ToMono();
        WaveOut.Init(SampleProvider);
    }

    public OscillatorItemViewModel() //Конструктор
    {
    }

    public OscillatorItemViewModel(string audioDevice) //Конструктор
    {
        ItemName = audioDevice;
        GroupKey = KeyGen();

        this.WhenAnyValue(
                x => x.Frequency,
                x => x.Sin,
                x => x.Square,
                x => x.Sawtooth,
                x => x.Triangle,
                x => x.Noise)
            .Subscribe(x =>
            {
                IsPlaying = false;
                WaveOutInit(audioDevice);
            });
    }

    private string KeyGen() //Генерация уникального ключа для группы радиокнопок
    {
        return Guid.NewGuid().ToString();
    }
}