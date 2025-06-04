using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using Avalonia.Threading;
using DynamicData;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Painting;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using SkiaSharp;
using SoundTester.Messages;
using SoundTester.Views;
using SoundTesting;
using Splat;

namespace SoundTester.ViewModels;

public class VoiceTrackerViewModel : ViewModelBase
{
    private string _recButtonContent = "Начать проверку";
    private string _progressbarColor = "RoyalBlue";
    private string _selectedDevice;
    private int _selectedDeviceIndex;
    private ObservableCollection<string> _devices;
    private float _volume;
    private float _dbfs;
    private bool _isMonitoring = false;
    private bool _isEnabled = false;
    private bool _isCheckedCeiling = false;
    private bool _isCheckedFloor = false;
    private WasapiCapture _wasapiCapture;
    private WasapiOut _wasapiOut;
    private BufferedWaveProvider _bufferedWaveProvider;
    private DevicesEnumerator _devicesEnumerator;
    
    private IEnumerable<ISeries> _seriesMagnitudes;
    private ObservableCollection<float> _magnitudes = new ObservableCollection<float>();
    
    private readonly List<AudioPoint> _tempBuffer = new(); // Временный буфер
    private readonly object _syncLock = new(); //чтобы одновременно не происходили чтение и чистка/запись в _tempBuffer
    


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

    public float DBFS
    {
        get => _dbfs;
        set => this.RaiseAndSetIfChanged(ref _dbfs, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public bool IsCheckedCeiling
    {
        get => _isCheckedCeiling;
        set => this.RaiseAndSetIfChanged(ref _isCheckedCeiling, value);
    }

    public bool IsCheckedFloor
    {
        get => _isCheckedFloor;
        set => this.RaiseAndSetIfChanged(ref _isCheckedFloor, value);
    }

    public string RecButtonContent
    {
        get => _recButtonContent;
        set => this.RaiseAndSetIfChanged(ref _recButtonContent, value);
    }

    public string ProgressbarColor
    {
        get => _progressbarColor;
        set => this.RaiseAndSetIfChanged(ref _progressbarColor, value);
    }

    public ICommand RecordCommand { get; }
    
    public ReactiveCommand<Unit, Unit> SaveSpectrogramCommand { get; }
    
    public ReactiveCommand<Unit, Unit> SaveOscillogramCommand { get; }

    public IEnumerable<ISeries> SeriesMagnitudes
    {
        get => _seriesMagnitudes;
        set => this.RaiseAndSetIfChanged(ref _seriesMagnitudes, value);
    }
    
    public ObservableCollection<float> Magnitudes
    {
        get => _magnitudes;
        set => this.RaiseAndSetIfChanged(ref _magnitudes, value);
    }
    
    public Axis[] XAxisMagnitudes { get; set; } = new Axis[] { new Axis { Name = "Частота (Гц)", 
        ShowSeparatorLines = true, SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1), SeparatorsAtCenter = true }   };
    public Axis[] YAxisMagnitudes { get; set; } = new Axis[] { new Axis { Name = "Амплитуда", MinLimit = 0, MaxLimit = 0.01, 
        ShowSeparatorLines = true, SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1), SeparatorsAtCenter = true  }  };

    public ISeries[] SeriesPower { get; set; }
    
    public Axis[] XAxesFrequencies { get; set; }
    
    public Axis[] YAxesFrequencies { get; set; }
    
    public VoiceTrackerViewModel(DevicesEnumerator? devicesEnumerator = null) //Конструктор
    {
        GraphsInit();
        
        _devicesEnumerator = devicesEnumerator ?? Locator.Current.GetService<DevicesEnumerator>()!;

        Devices = _devicesEnumerator!.DevicesUpdater.InputDevices;
        SelectedDevice = Devices.FirstOrDefault();
        
        RecordCommand = ReactiveCommand.Create(() => Record());
        
        SaveSpectrogramCommand = ReactiveCommand.Create(() =>
        {
            MessageBus.Current.SendMessage(new SaveSpectrogramMessage());
        });
        
        SaveOscillogramCommand = ReactiveCommand.Create(() =>
        {
            MessageBus.Current.SendMessage(new SaveOscillogramMessage());
        });
        
        Dispatcher.UIThread.InvokeAsync(new Action((() => RefreshProgressBar(-96))));
        
        this.WhenAnyValue(x => x.Devices, x => x.SelectedDevice)
            .WhereNotNull()
            .Subscribe(x =>
            {
                Devices = _devicesEnumerator.DevicesUpdater.InputDevices;
                IsEnabled = SelectedDeviceIndex == -1 || Devices.Count == 0 || _devicesEnumerator.DevicesUpdater.OutputDevices.Count == 0 ? false : true;
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

        this.WhenAnyValue(x => x.IsCheckedCeiling).Subscribe(x =>
        {
            SeriesMagnitudes.ElementAt(1).IsVisible = IsCheckedCeiling;
        });
        
        this.WhenAnyValue(x => x.IsCheckedFloor).Subscribe(x =>
        {
            SeriesMagnitudes.ElementAt(2).IsVisible = IsCheckedFloor;
        });
    }

    private void GraphsInit()
    {
        //настройка спектрограммы
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
            },
            new LineSeries<float>
            {
                Name = "Верхний порог шумов",
                Stroke = new SolidColorPaint(SKColors.IndianRed, 2),
                GeometryStroke = null,  
                GeometryFill = null,    
                GeometrySize = 0,
                Fill = null
            },
            new LineSeries<float>
            {
                Name = "Нижний порог шумов",
                Stroke = new SolidColorPaint(SKColors.LightGreen, 2),
                GeometryStroke = null,  
                GeometryFill = null,    
                GeometrySize = 0,
                Fill = null
            }
        };
        
        // Настройка осциллограммы
        LiveCharts.Configure(config =>
            config
                .HasMap<AudioPoint>((point, index) => 
                    new(point.TimeMs, point.Amplitude)
                ));

        SeriesPower = new ISeries[]
        {
            new LineSeries<AudioPoint>
            {
                Values = new ObservableCollection<AudioPoint>(),
                Stroke = new SolidColorPaint(SKColors.LightSkyBlue, 2),
                GeometryStroke = null,  
                GeometryFill = null,    
                GeometrySize = 0,
                Fill = null,
                XToolTipLabelFormatter = point => $"{point.Model.TimeMs:F2} ms: {point.Model.Amplitude:F2}",
                DataPadding = new LvcPoint(0, 0)
            }
        };
        
        XAxesFrequencies = new[]
        {
            new Axis
            {
                Name = "Время (мс)",
                MinLimit = 0,
                MaxLimit = 10,
                ShowSeparatorLines = true, 
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
                SeparatorsAtCenter = true
            }
        };

        YAxesFrequencies = new[]
        {
            new Axis
            {
                Name = "Амплитуда",
                MinLimit = -1,
                MaxLimit = 1,
                ShowSeparatorLines = true,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(100), 1),
                SeparatorsAtCenter = true
            }
        };
    }

    private void VoiceTrackerInit() //Инициаизация трекера
    {
        var en = new MMDeviceEnumerator(); //Костыль
        var inD = en.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).Where(x => x.FriendlyName == SelectedDevice).FirstOrDefault();
        var outD = en.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        
            
        _wasapiCapture = new WasapiCapture(inD, false, 100);
        _wasapiCapture.WaveFormat = new WaveFormat(44100, 16, 1);
        _wasapiOut = new WasapiOut(outD, AudioClientShareMode.Shared, false, 100);
                
        _wasapiCapture.DataAvailable += OnDataAvailable;
        
        _bufferedWaveProvider = new BufferedWaveProvider(_wasapiCapture?.WaveFormat){ DiscardOnBufferOverflow = true };
        _wasapiOut.Init(_bufferedWaveProvider);
            
        Dispatcher.UIThread.InvokeAsync(new Action((() => RefreshProgressBar(-96))));
        
        en.Dispose();
    }
    
    private void Record() //Запуск/остановка трекера
    {
        if (_isMonitoring)
        {
            _wasapiCapture?.StopRecording();
            _wasapiOut.Stop();
            Dispatcher.UIThread.InvokeAsync(new Action((() => RefreshProgressBar(-96))));
            
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
        
        //осциллограмма
        OscillogrammGaining(sender, e);
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
    
        // Конвертация амплитуды в dBFS
        float dBFS;
        if (max > 0)
        {
            dBFS = 20 * MathF.Log10(max); // Формула перевода амплитуды в dBFS
        }
        else
        {
            dBFS = -96; // Минимальное значение для тишины (или другое очень низкое значение)
        }

        DBFS = dBFS;
        
        Dispatcher.UIThread.InvokeAsync(() => RefreshProgressBar(dBFS));
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

        //Нахождение верхнего и нижнего порогов шума
        
        float[] magnitudesChanged = new float[magnitudes.Length]; //Массив для сортировки всех значений
        
        Array.Copy(magnitudes, magnitudesChanged, magnitudes.Length);//Перенос и сортировка значений
        Array.Sort(magnitudesChanged);
        
        float noiseCeiling = magnitudesChanged[(int)(magnitudesChanged.Length * 0.93f)]; //93 перцентиль от всех сортированных значений, 7% наивысших частот не попадают
        float noiseFloor = magnitudesChanged.Skip(magnitudesChanged.Length * 3 / 4).Min(); //Поиск минимального значения в последних 25% сортированных значений
        
        //Передача в график необходимых значений
        SeriesMagnitudes.ElementAt(0).Values = magnitudes;
        SeriesMagnitudes.ElementAt(1).Values = Enumerable.Repeat(noiseCeiling, magnitudes.Length);
        SeriesMagnitudes.ElementAt(2).Values = Enumerable.Repeat(noiseFloor, magnitudes.Length);
    }
    
    private void OscillogrammGaining(object sender, WaveInEventArgs e)
    {
        lock (_syncLock)
        {
            _tempBuffer.Clear();
            int samplesPerWindow = 44100 * 10 / 1000; // 441 сэмпл для 44.1 кГц (произведение частоты дискретизации и размера окна (10 мс))

            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                float amplitude = sample / 32768f;
                double timeMs = _tempBuffer.Count * (10 / (double)samplesPerWindow);

                _tempBuffer.Add(new AudioPoint { TimeMs = timeMs, Amplitude = amplitude });

                if (_tempBuffer.Count >= samplesPerWindow) break; // Ограничиваем буфер
            }

            // Полная перезапись данных в UI-потоке
            Dispatcher.UIThread.Post(() =>
            {
                var series = (ObservableCollection<AudioPoint>)SeriesPower[0].Values!;
                series.Clear();
                foreach (var point in _tempBuffer)
                    series.Add(point);
            });
        }
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
        if ( percentage <= 0 && percentage > -6) 
            ProgressbarColor = "Tomato";
        else if (percentage <= -6 && percentage > -20)
            ProgressbarColor = "Gold";
        else if (percentage <= -20 && percentage > -40)
            ProgressbarColor = "LightGreen";
        else
            ProgressbarColor = "RoyalBlue";
            
    }
}