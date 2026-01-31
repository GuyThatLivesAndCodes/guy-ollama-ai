using System;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using GuyOllamaAI.ViewModels;

namespace GuyOllamaAI.Views;

public partial class MainWindow : Window
{
    private ScrollViewer? _messagesScrollViewer;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _messagesScrollViewer = this.FindControl<ScrollViewer>("MessagesScrollViewer");

        if (DataContext is MainViewModel viewModel)
        {
            // Subscribe to collection changes
            viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;

            // Subscribe to scroll requests from ViewModel
            viewModel.ScrollToBottomRequested += OnScrollToBottomRequested;
        }
    }

    private void OnScrollToBottomRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ScrollToBottom();
        }, DispatcherPriority.Background);
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Scroll to bottom when messages are added or changed
        Dispatcher.UIThread.Post(() =>
        {
            ScrollToBottom();
        }, DispatcherPriority.Background);
    }

    private void ScrollToBottom()
    {
        if (_messagesScrollViewer != null)
        {
            _messagesScrollViewer.ScrollToEnd();
        }
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
