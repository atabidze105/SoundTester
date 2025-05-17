using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Threading;
using DynamicData;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using SkiaSharp;
using SoundTesting;
using Splat;

namespace SoundTester.ViewModels;

public class VoiceTrackerViewModel : ViewModelBase
{
    private string _recButtonContent = "Начать проверку";
    private string _selectedDevice;
    private int _selectedDeviceIndex;
    private ObservableCollection<string> _devices;
    private float _volume;
    private bool _isMonitoring = false;
    private bool _isEnabled = false;
    private WasapiCapture _wasapiCapture;
    private WasapiOut _wasapiOut;
    private BufferedWaveProvider _bufferedWaveProvider;
    private DevicesEnumerator _devicesEnumerator;
    
    private IEnumerable<ISeries> _seriesMagnitudes;
    private IEnumerable<ISeries> _seriesFrequences;
    private ObservableCollection<float> _magnitudes = new ObservableCollection<float>();
    private ObservableCollection<float> _frequences = new ObservableCollection<float>();
    
    public ObservableCollection<ISeries> MySeries { get; } = new ObservableCollection<ISeries>();
    
    

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

    public IEnumerable<ISeries> SeriesMagnitudes
    {
        get => _seriesMagnitudes;
        set => this.RaiseAndSetIfChanged(ref _seriesMagnitudes, value);
    }
    
    public IEnumerable<ISeries> SeriesFrequences
    {
        get => _seriesFrequences;
        set => this.RaiseAndSetIfChanged(ref _seriesFrequences, value);
    }

    public ObservableCollection<float> Magnitudes
    {
        get => _magnitudes;
        set => this.RaiseAndSetIfChanged(ref _magnitudes, value);
    }

    public ObservableCollection<float> Frequences
    {
        get => _frequences;
        set => this.RaiseAndSetIfChanged(ref _frequences, value);
    }
    
    public Axis[] XAxisMagnitudes { get; set; } = new Axis[] { new Axis { Name = "Частота (Гц)" }  };
    public Axis[] YAxisMagnitudes { get; set; } = new Axis[] { new Axis { Name = "Амплитуда" }  };

    
    
    public VoiceTrackerViewModel(DevicesEnumerator? devicesEnumerator = null) //Конструктор
    {
        SeriesMagnitudes = new ISeries[]
        {
            new LineSeries<float>
            {
                Values = Magnitudes,
                Name = "Амплитуда",
                Stroke = new SolidColorPaint(SKColors.LightSkyBlue, 2),
                GeometryStroke = null,  
                GeometryFill = null,    
                GeometrySize = 0,
            }
        };

        SeriesFrequences = new ISeries[]
        {
            new LineSeries<float>
            {
                Values = Frequences,
                Name = "Частота",
                Stroke = new SolidColorPaint(SKColors.LightSkyBlue, 2),
                GeometryStroke = null,  
                GeometryFill = null,    
                GeometrySize = 0,
            }
        };
        
        MySeries.Add(new LineSeries<float>
        {
            Values = new ObservableCollection<float>(),
            GeometrySize = 0,
            LineSmoothness = 0
        });
        
        _devicesEnumerator = devicesEnumerator ?? Locator.Current.GetService<DevicesEnumerator>()!;

        Devices = _devicesEnumerator!.DevicesUpdater.InputDevices;
        SelectedDevice = Devices.FirstOrDefault();
        
        RecordCommand = ReactiveCommand.Create(() => Record());
        
        this.WhenAnyValue(x => x.Devices, x => x.SelectedDevice)
            .WhereNotNull()
            .Subscribe(x =>
            {
                Devices = _devicesEnumerator.DevicesUpdater.InputDevices;
                IsEnabled = SelectedDeviceIndex == -1 || Devices.Count == 0 ? false : true;
            });
        
        this.WhenAnyValue(x => x.SelectedDeviceIndex).Subscribe(x =>
        {
            if (_isMonitoring)
            {
                _isMonitoring = false;
                
                _wasapiCapture?.StopRecording();
                _wasapiOut?.Stop();
                _wasapiCapture?.Dispose();
                _wasapiOut?.Dispose();
                
                RecButtonContent = "Начать проверку";
                
                VoiceTrackerInit();
            }
        });
    }

    private void TimerOnTick(object? sender, EventArgs e)
    {
        
    }

    private void VoiceTrackerInit() //Инициаизация трекера
    {
        var en = new MMDeviceEnumerator(); //Костыль
        var inD = en.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).Where(x => x.FriendlyName == SelectedDevice).FirstOrDefault();
        var outD = en.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        
            
        _wasapiCapture = new WasapiCapture(inD, false, 100);
        _wasapiOut = new WasapiOut(outD, AudioClientShareMode.Shared, false, 100);
                
        _wasapiCapture.DataAvailable += OnDataAvailable;
        
        _bufferedWaveProvider = new BufferedWaveProvider(_wasapiCapture?.WaveFormat){ DiscardOnBufferOverflow = true };
        _wasapiOut.Init(_bufferedWaveProvider);
            
        Dispatcher.UIThread.InvokeAsync(new Action((() => RefreshProgressBar(0))));
        
        en.Dispose();
    }
    
    private void Record() //Запуск/остановка трекера
    {
        if (_isMonitoring)
        {
            _wasapiCapture?.StopRecording();
            _wasapiOut.Stop();
            Dispatcher.UIThread.InvokeAsync(new Action((() => RefreshProgressBar(0))));
            
            RecButtonContent = "Начать проверку";
        }
        else
        {
            VoiceTrackerInit();
            
            _wasapiCapture?.StartRecording();
            _wasapiOut.Play();
            
            RecButtonContent = "Остановить проверку";
        }

        _isMonitoring = !_isMonitoring;
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        VolumeGaining(sender, e);
        _bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded); //Добавление данных для мониторинга микрофона
        
        
        //fft
        FFTSeriesGaining(sender, e);
    }

    private void VolumeGaining(object sender, WaveInEventArgs e)
    {
        WasapiCapture capture = sender as WasapiCapture;
        float max = 0;
    
        if (capture.WaveFormat.BitsPerSample == 16)
        {
            // Обработка 16-битных сэмплов
            var buffer = new WaveBuffer(e.Buffer);
            int samples = e.BytesRecorded / 2;
        
            for (int i = 0; i < samples; i++)
            {
                var sample = buffer.ShortBuffer[i] / 32768f;
                if (sample < 0) sample = -sample;
                if (sample > max) max = sample;
            }
        }
        else if (capture.WaveFormat.BitsPerSample == 32 && 
                 capture.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            // Обработка 32-битных float сэмплов
            var buffer = new WaveBuffer(e.Buffer);
            int samples = e.BytesRecorded / 4;
        
            for (int i = 0; i < samples; i++)
            {
                var sample = buffer.FloatBuffer[i];
                if (sample < 0) sample = -sample;
                if (sample > max) max = sample;
            }
        }
        
        Dispatcher.UIThread.InvokeAsync(new Action((() => RefreshProgressBar(max*100)))); //Отображение значение дБ в прогрессбаре
        
    }

    private void FFTSeriesGaining(object sender, WaveInEventArgs e)
    { 
        WasapiCapture wasapiCapture = sender as WasapiCapture;
        // Конвертция сырых данных в float[]
        float[] audioData = new float[e.BytesRecorded / 2];
        for (int i = 0; i < audioData.Length; i++)
        {
            short sample = (short)(e.Buffer[i * 2 + 1] << 8 | e.Buffer[i * 2]);
            audioData[i] = sample / 32768f; // Нормализация в [-1, 1]
        }
        
        //Выбор размера FFT (степень двойки)
        int fftSize = 1024;
        if (audioData.Length < fftSize) return;
        
        //Обрезка или дополнение нулями 
        float[] fftInput = new float[fftSize];
        Array.Copy(audioData, fftInput, Math.Min(audioData.Length, fftSize));
        
        // Применение оконной функци Ханна
        ApplyHannWindow(fftInput, fftSize);
        
        // Подготовка Complex[] для FFT
        Complex[] fftBuffer = new Complex[fftSize];
        for (int i = 0; i < fftSize; i++)
        {
            fftBuffer[i].X = fftInput[i]; // Реальная часть
            fftBuffer[i].Y = 0;          // Мнимая часть = 0
        }
        
        // Выполнение FFT
        NAudio.Dsp.FastFourierTransform.FFT(true, (int)Math.Log(fftSize, 2), fftBuffer);
        
        // Получение амплитуды частот (для визуализации)
        float[] magnitudes = new float[fftSize / 2];
        for (int i = 0; i < fftSize / 2; i++)
        {
            magnitudes[i] = (float)Math.Sqrt(fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y);
        }
        
        // Получение децибел
        // Конвертация byte[] -> float[] (нормализация)
        var samples = new float[e.BytesRecorded / 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short sample = (short)(e.Buffer[i * 2 + 1] << 8 | e.Buffer[i * 2]);
            samples[i] = sample / 32768f; // [-1, 1]
        }

        // Усреднение амплитуд за 10 мс
        int samplesPerPoint = 44100 * 10 / 1000;
        var averaged = samples
            .Select((x, i) => new { Index = i / samplesPerPoint, Value = Math.Abs(x) })
            .GroupBy(x => x.Index)
            .Select(g => g.Average(x => x.Value))
            .ToArray();

        // Обновление данных на UI-потоке
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var lineSeries = (LineSeries<float>)MySeries[0];
            lineSeries.Values = new ObservableCollection<float>(averaged);
        });
        
        
        //Передача в график необходимых значений
        SeriesMagnitudes.First().Values = magnitudes;
        //SeriesFrequences.Last().Values = null;
    }
    
    private void ApplyHannWindow(float[] data, int frameSize)
    {
        for (int n = 0; n < frameSize; n++)
        {
            double multiplier = NAudio.Dsp.FastFourierTransform.HannWindow(n, frameSize);
            data[n] = (float)(data[n] * multiplier);
        }
    }

    private void RefreshProgressBar(float percentage)
    {
        Volume = percentage;
    }

    private void UpdateCartesianChart(ObservableCollection<float> data)
    {
        _magnitudes.Clear();
        _magnitudes = data;
    }
}