using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using SoundTester.ViewModels;

namespace SoundTester.Views;

public partial class OscillatorView : ReactiveUserControl<OscillatorViewModel>
{
    public OscillatorView()
    {
        DataContext = new OscillatorViewModel();
        InitializeComponent();
    }
}