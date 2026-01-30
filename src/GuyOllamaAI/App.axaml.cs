using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GuyOllamaAI.Services;
using GuyOllamaAI.ViewModels;
using GuyOllamaAI.Views;

namespace GuyOllamaAI;

public partial class App : Application
{
    public static OllamaService OllamaService { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        OllamaService = new OllamaService();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new SplashWindow
            {
                DataContext = new SplashViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
