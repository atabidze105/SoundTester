using Avalonia.ReactiveUI;
using SoundTester.ViewModels;

namespace SoundTester.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}