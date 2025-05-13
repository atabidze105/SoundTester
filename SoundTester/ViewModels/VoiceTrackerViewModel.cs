using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using ReactiveUI;
using SoundTesting;
using Splat;

namespace SoundTester.ViewModels;

public class VoiceTrackerViewModel : ViewModelBase
{
    private string _recButtonContent = "Начать проверку";
    private string _selectedDevice;
    private int _selectedDeviceIndex;
    private ObservableCollection<string> _devices;
    private float _volume = -96;
    private bool _isMonitoring = false;
    private bool _isEnabled = false;
    private WasapiCapture _wasapiCapture;
    private WasapiOut _wasapiOut;
    private BufferedWaveProvider _bufferedWaveProvider;
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

    private void VoiceTrackerInit() //Инициаизация трекера
    {
        var en = new MMDeviceEnumerator(); //Костыль
        var inD = en.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).Where(x => x.FriendlyName == SelectedDevice).FirstOrDefault();
        var outD = en.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        
            
        _wasapiCapture = new WasapiCapture(inD, false, 50);
        _wasapiOut = new WasapiOut(outD, AudioClientShareMode.Shared, false, 50);
                
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
        Dispatcher.UIThread.InvokeAsync(new Action((() => RefreshProgressBar(res)))); //Отображение значение дБ в прогрессбаре
        
        _bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded); //Добавление данных для мониторинга микрофона
    }

    private void RefreshProgressBar(float percentage)
    {
        Volume = percentage;
    }
}