using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using SoundTester.ViewModels;

namespace SoundTester.Views;

public partial class OscillatorItemView : ReactiveUserControl<OscillatorItemViewModel>
{
    public OscillatorItemView()
    {
        
        InitializeComponent();
    }
}