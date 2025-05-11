namespace SoundTester.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public VoiceTrackerViewModel VoiceTracker { get; }
    public OscillatorViewModel Oscillator { get; }
    public PanningPickerViewModel PanningPicker { get; }
    public MainWindowViewModel()
    {
        VoiceTracker = new VoiceTrackerViewModel();
        Oscillator = new OscillatorViewModel();
        PanningPicker = new PanningPickerViewModel();
    }
}