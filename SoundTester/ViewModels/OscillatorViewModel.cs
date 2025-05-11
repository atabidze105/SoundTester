using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;
using SoundTesting;
using Splat;

namespace SoundTester.ViewModels;

public class OscillatorViewModel : ViewModelBase
{
    private string _selectedDevice;
    private int _selectedDeviceIndex;
    private ObservableCollection<string> _devices;
    private string _playButtonText = "Начать прослушивание";
    private bool _isPlaying = false;
    private ObservableCollection<OscillatorItemViewModel> _items;
    private bool _isItemSelected = false;
    private bool _isEnabled = false;
    private bool _isEnabledByCount = false;
    private DevicesEnumerator _devicesEnumerator;
    private OscillatorItemViewModel _selectedItem;


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

    public ObservableCollection<string> Devices
    {
        get => _devices;
        set => this.RaiseAndSetIfChanged(ref _devices, value);
    }

    public string PlayButtonText
    {
        get => _playButtonText;
        set => this.RaiseAndSetIfChanged(ref _playButtonText, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }

    public ObservableCollection<OscillatorItemViewModel> Items
    {
        get => _items;
        set => this.RaiseAndSetIfChanged(ref _items, value);
    }

    public bool IsItemSelected
    {
        get => _isItemSelected;
        set => this.RaiseAndSetIfChanged(ref _isItemSelected, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public bool IsEnabledByCount
    {
        get => _isEnabledByCount;
        set => this.RaiseAndSetIfChanged(ref _isEnabledByCount, value);
    }

    public OscillatorItemViewModel SelectedItem
    {
        get => _selectedItem;
        set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
    }

    public ICommand PlayCommand { get; }

    public ICommand AddItemCommand { get; }

    public ICommand RemoveItemCommand { get; }

    public OscillatorViewModel(DevicesEnumerator? devicesEnumerator = null)
    {
        PlayCommand = ReactiveCommand.Create(() => Play());
        AddItemCommand = ReactiveCommand.Create(() => AddItem());
        RemoveItemCommand = ReactiveCommand.Create(() => RemoveItem());

        Items = new ObservableCollection<OscillatorItemViewModel>();
        _devicesEnumerator = devicesEnumerator ?? Locator.Current.GetService<DevicesEnumerator>()!;

        Devices = _devicesEnumerator!.DevicesUpdater.InputDevices;

        SelectedDevice = Devices.FirstOrDefault();


        this.WhenAnyValue(x => x.Devices)
            .WhereNotNull().Subscribe(x =>
            {
                SelectedDevice = null!;
                SelectedDevice = Devices?.FirstOrDefault();
                ReloadDevices();
            });

        this.WhenAnyValue(x => x.Items.Count).WhereNotNull().Subscribe(x =>
        {
            IsEnabledByCount = Items.Count > 0 ? true : false;
        });

        this.WhenAnyValue(x => x.Devices.Count).Subscribe(x =>
        {
            foreach (var item in Items)
            {
                item.WaveOut.Stop();
                item.IsPlaying = false;
            }

            Items.Clear();
        });
    }

    private void Play()
    {
        if (_isPlaying!)
        {
            PlayButtonText = "Начать прослушивание";
            foreach (var item in _items)
            {
                item.WaveOut.Stop();
                item.IsPlaying = false;
            }
        }
        else
        {
            PlayButtonText = "Остановить прослушивание";
            foreach (var item in _items)
            {
                item.WaveOut.Play();
                item.IsPlaying = true;
            }
        }

        _isPlaying = !_isPlaying;
    }

    private void AddItem()
    {
        foreach (var item in Items)
        {
            item.WaveOut.Stop();
            item.IsPlaying = false;
        }

        IsPlaying = false;
        PlayButtonText = "Начать прослушивание";

        OscillatorItemViewModel oscillatorItem = new OscillatorItemViewModel(SelectedDeviceIndex, SelectedDevice);
        Items.Add(oscillatorItem);

        foreach (var ite in Items)
        {
            ite.WhenAnyValue(x => x.Frequency).Subscribe(x =>
            {
                ite.WaveOut?.Stop();
                ite.IsPlaying = false;
                PlayButtonText = "Начать прослушивание";
            });
        }
    }

    private void RemoveItem()
    {
        if (SelectedItem != null)
        {
            SelectedItem.WaveOut.Stop();
            Items.Remove(SelectedItem);
        }
    }

    private void ReloadDevices() => this.WhenAnyValue(x => x.SelectedDevice)
        .Subscribe(_ =>
        {
            Devices = _devicesEnumerator.DevicesUpdater.OutputDevices;
            IsEnabled = SelectedDeviceIndex == -1 || Devices.Count == 0 ? false : true;
        });
}