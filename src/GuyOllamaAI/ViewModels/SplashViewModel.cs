using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuyOllamaAI.Views;

namespace GuyOllamaAI.ViewModels;

public partial class SplashViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _logoOpacity = 0;

    [ObservableProperty]
    private double _logoScale = 0.5;

    [ObservableProperty]
    private double _titleOpacity = 0;

    [ObservableProperty]
    private double _titleOffset = 20;

    [ObservableProperty]
    private double _subtitleOpacity = 0;

    [ObservableProperty]
    private double _subtitleOffset = 20;

    [ObservableProperty]
    private double _infoOpacity = 0;

    [ObservableProperty]
    private double _infoOffset = 30;

    [ObservableProperty]
    private double _buttonOpacity = 0;

    [ObservableProperty]
    private double _buttonOffset = 20;

    public SplashViewModel()
    {
        StartAnimationAsync();
    }

    private async void StartAnimationAsync()
    {
        await Task.Delay(200);

        // Animate logo
        await AnimatePropertyAsync(
            value => { LogoOpacity = value; LogoScale = 0.5 + (value * 0.5); },
            300);

        await Task.Delay(100);

        // Animate title
        await AnimatePropertyAsync(
            value => { TitleOpacity = value; TitleOffset = 20 * (1 - value); },
            250);

        await Task.Delay(50);

        // Animate subtitle
        await AnimatePropertyAsync(
            value => { SubtitleOpacity = value; SubtitleOffset = 20 * (1 - value); },
            250);

        await Task.Delay(100);

        // Animate info box
        await AnimatePropertyAsync(
            value => { InfoOpacity = value; InfoOffset = 30 * (1 - value); },
            300);

        await Task.Delay(150);

        // Animate button
        await AnimatePropertyAsync(
            value => { ButtonOpacity = value; ButtonOffset = 20 * (1 - value); },
            250);
    }

    private async Task AnimatePropertyAsync(Action<double> setter, int durationMs)
    {
        const int steps = 30;
        var stepDuration = durationMs / steps;

        for (int i = 0; i <= steps; i++)
        {
            var t = (double)i / steps;
            var eased = EaseOutCubic(t);

            await Dispatcher.UIThread.InvokeAsync(() => setter(eased));
            await Task.Delay(stepDuration);
        }
    }

    private static double EaseOutCubic(double t)
    {
        return 1 - Math.Pow(1 - t, 3);
    }

    [RelayCommand]
    private void Continue()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            var oldWindow = desktop.MainWindow;
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            oldWindow?.Close();
        }
    }
}
