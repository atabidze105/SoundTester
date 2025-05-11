using System;
using System.ComponentModel.Design;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using SoundTester.ViewModels;
using SoundTester.Views;
using SoundTesting;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace SoundTester.Helpers;

public static class Bootstrapper
{
    public static void Configure()
    {
        var services = new ServiceCollection();

        services.AddSingleton<DevicesEnumerator>();
        services.AddSingleton<MainWindowViewModel>();
        
        services.UseMicrosoftDependencyResolver();
        
        var provider = services.BuildServiceProvider();
        
        Locator.CurrentMutable.InitializeSplat();
        Locator.CurrentMutable.InitializeReactiveUI();
        
        Locator.CurrentMutable.RegisterConstant<IServiceProvider>(provider);
        
        Locator.CurrentMutable.Register<IViewFor<VoiceTrackerViewModel>>(() => new VoiceTrackerView());
        
        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
    }
}