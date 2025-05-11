using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace SoundTester.Views;

public partial class PanningPickerView : ReactiveUserControl<PanningPickerView>
{
    public PanningPickerView()
    {
        InitializeComponent();
    }
}