using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using GuyOllamaAI.ViewModels;

namespace GuyOllamaAI.Views;

public partial class MainWindow : Window
{
    private ScrollViewer? _messagesScrollViewer;
    private bool _scrollPending;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _messagesScrollViewer = this.FindControl<ScrollViewer>("MessagesScrollViewer");

        if (_messagesScrollViewer != null)
        {
            // Subscribe to layout updates for more reliable scrolling
            _messagesScrollViewer.LayoutUpdated += OnScrollViewerLayoutUpdated;
        }

        if (DataContext is MainViewModel viewModel)
        {
            // Subscribe to collection changes
            viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;

            // Subscribe to scroll requests from ViewModel
            viewModel.ScrollToBottomRequested += OnScrollToBottomRequested;
        }
    }

    private void OnScrollViewerLayoutUpdated(object? sender, EventArgs e)
    {
        if (_scrollPending && _messagesScrollViewer != null)
        {
            _scrollPending = false;
            ScrollToBottomImmediate();
        }
    }

    private void OnScrollToBottomRequested(object? sender, EventArgs e)
    {
        ScheduleScrollToBottom();
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Scroll to bottom when messages are added or changed
        ScheduleScrollToBottom();
    }

    private void ScheduleScrollToBottom()
    {
        _scrollPending = true;

        // Also do an immediate scroll attempt with a small delay
        Dispatcher.UIThread.Post(async () =>
        {
            // Small delay to let layout complete
            await Task.Delay(50);
            ScrollToBottomImmediate();
        }, DispatcherPriority.Render);
    }

    private void ScrollToBottomImmediate()
    {
        if (_messagesScrollViewer != null)
        {
            // Get the extent (total scrollable height) and set offset to it
            var extent = _messagesScrollViewer.Extent;
            var viewport = _messagesScrollViewer.Viewport;

            if (extent.Height > viewport.Height)
            {
                _messagesScrollViewer.Offset = new Vector(0, extent.Height - viewport.Height + 100);
            }
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
