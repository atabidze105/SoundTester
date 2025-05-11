using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SoundTester.ViewModels;

namespace SoundTester.Views;

public partial class VoiceTrackerView : ReactiveUserControl<VoiceTrackerViewModel>
{
    public VoiceTrackerView()
    {
        DataContext = new VoiceTrackerViewModel();
        InitializeComponent();
    }
}