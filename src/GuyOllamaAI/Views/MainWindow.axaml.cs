using Avalonia.Controls;
using Avalonia.Input;
using GuyOllamaAI.ViewModels;

namespace GuyOllamaAI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InputTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (DataContext is MainViewModel viewModel && viewModel.CanSendMessage)
            {
                viewModel.SendMessageCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
