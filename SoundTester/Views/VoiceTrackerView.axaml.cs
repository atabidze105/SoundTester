using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.SKCharts;
using ReactiveUI;
using SoundTester.Messages;
using SoundTester.ViewModels;

namespace SoundTester.Views;


public partial class VoiceTrackerView : ReactiveUserControl<VoiceTrackerViewModel>
{
    public VoiceTrackerView()
    {
        DataContext = new VoiceTrackerViewModel();
        
        InitializeComponent();
        
        MessageBus.Current.Listen<SaveSpectrogramMessage>()
            .Subscribe(new Action<object>(async _ => await SaveChart("SpectrogramChart")));
        
        MessageBus.Current.Listen<SaveOscillogramMessage>()
            .Subscribe(new Action<object>(async _ => await SaveChart("OscillogramChart")));
        
    }
    
    private async Task SaveChart( string chartName)
    {
        var chart = this.FindControl<CartesianChart>(chartName);
        if (chart == null) return;

        await SaveImage(chart);
    }

    private async Task SaveImage(CartesianChart chart)
    {
        var topLevel = TopLevel.GetTopLevel(chart);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"{(chart.Name == "SpectrogramChart" ? "Сохранить спектрограмму" : "Сохранить осцилограмму" )}",
            SuggestedFileName = $"{(chart.Name == "SpectrogramChart" ? "spectrogram" : "oscillogram" )}-{DateTime.Now:yyyy-M-d-H-mm-ss}.png",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PNG") { Patterns = new[] { "*.png" } }
            }
        });

        if (file != null)
        {
            var skChart = new SKCartesianChart(chart);
            skChart.SaveImage(file.Path.AbsolutePath);
        }
    }
}