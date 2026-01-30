using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuyOllamaAI.Models;

namespace GuyOllamaAI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private CancellationTokenSource? _currentCancellation;
    private ChatSession _currentSession = new();

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    [ObservableProperty]
    private ObservableCollection<ChatSession> _chatHistory = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableModels = new();

    [ObservableProperty]
    private string _selectedModel = string.Empty;

    [ObservableProperty]
    private string _inputMessage = string.Empty;

    [ObservableProperty]
    private string _currentChatTitle = "New Chat";

    [ObservableProperty]
    private string _serverUrl = "http://localhost:11434";

    [ObservableProperty]
    private string _connectionStatus = "Checking...";

    [ObservableProperty]
    private IBrush _connectionStatusColor = new SolidColorBrush(Color.Parse("#94A3B8"));

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isInfoVisible;

    [ObservableProperty]
    private bool _isSettingsVisible;

    [ObservableProperty]
    private bool _isConnected;

    public bool CanSendMessage => !string.IsNullOrWhiteSpace(InputMessage) &&
                                   !IsLoading &&
                                   !string.IsNullOrEmpty(SelectedModel) &&
                                   IsConnected;

    public MainViewModel()
    {
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        await CheckConnectionAsync();
        await RefreshModelsAsync();
    }

    private async Task CheckConnectionAsync()
    {
        ConnectionStatus = "Checking...";
        ConnectionStatusColor = new SolidColorBrush(Color.Parse("#94A3B8"));

        App.OllamaService.BaseUrl = ServerUrl;
        var connected = await App.OllamaService.TestConnectionAsync();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsConnected = connected;
            if (connected)
            {
                ConnectionStatus = "Connected";
                ConnectionStatusColor = new SolidColorBrush(Color.Parse("#22C55E"));
            }
            else
            {
                ConnectionStatus = "Disconnected";
                ConnectionStatusColor = new SolidColorBrush(Color.Parse("#EF4444"));
            }
            OnPropertyChanged(nameof(CanSendMessage));
        });
    }

    [RelayCommand]
    private async Task RefreshModelsAsync()
    {
        var models = await App.OllamaService.GetModelsAsync();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            AvailableModels.Clear();
            foreach (var model in models)
            {
                AvailableModels.Add(model);
            }

            if (AvailableModels.Count > 0 && string.IsNullOrEmpty(SelectedModel))
            {
                SelectedModel = AvailableModels[0];
            }
            OnPropertyChanged(nameof(CanSendMessage));
        });
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputMessage) || IsLoading || string.IsNullOrEmpty(SelectedModel))
            return;

        var userMessage = InputMessage.Trim();
        InputMessage = string.Empty;

        // Add user message
        var userChatMessage = new ChatMessage("User", userMessage);
        Messages.Add(userChatMessage);
        _currentSession.Messages.Add(userChatMessage);

        // Update session title if this is the first message
        if (_currentSession.Messages.Count == 1)
        {
            _currentSession.UpdateTitle();
            CurrentChatTitle = _currentSession.Title;

            if (!ChatHistory.Contains(_currentSession))
            {
                ChatHistory.Insert(0, _currentSession);
            }
        }

        IsLoading = true;
        OnPropertyChanged(nameof(CanSendMessage));

        try
        {
            _currentCancellation = new CancellationTokenSource();

            // Add assistant message placeholder
            var assistantMessage = new ChatMessage("Assistant", "");
            Messages.Add(assistantMessage);

            // Stream the response
            var messagesForApi = new List<ChatMessage>(_currentSession.Messages);
            messagesForApi.RemoveAt(messagesForApi.Count - 1); // Remove the empty assistant message

            await foreach (var chunk in App.OllamaService.ChatStreamAsync(
                SelectedModel,
                messagesForApi,
                _currentCancellation.Token))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    assistantMessage.Content += chunk;
                    // Force UI update
                    var index = Messages.IndexOf(assistantMessage);
                    if (index >= 0)
                    {
                        Messages[index] = assistantMessage;
                    }
                });
            }

            // Add final message to session
            _currentSession.Messages.Add(new ChatMessage("Assistant", assistantMessage.Content));
            _currentSession.UpdatedAt = DateTime.Now;
        }
        catch (OperationCanceledException)
        {
            // Cancelled by user
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Messages.Add(new ChatMessage("System", $"Error: {ex.Message}"));
            });
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanSendMessage));
            _currentCancellation?.Dispose();
            _currentCancellation = null;
        }
    }

    [RelayCommand]
    private void NewChat()
    {
        _currentSession = new ChatSession();
        Messages.Clear();
        CurrentChatTitle = "New Chat";
        InputMessage = string.Empty;
    }

    [RelayCommand]
    private void SelectChat(ChatSession? session)
    {
        if (session == null) return;

        _currentSession = session;
        Messages.Clear();
        foreach (var message in session.Messages)
        {
            Messages.Add(message);
        }
        CurrentChatTitle = session.Title;
    }

    [RelayCommand]
    private void ShowInfo()
    {
        IsInfoVisible = true;
        IsSettingsVisible = false;
    }

    [RelayCommand]
    private void CloseInfo()
    {
        IsInfoVisible = false;
    }

    [RelayCommand]
    private void ShowSettings()
    {
        IsSettingsVisible = true;
        IsInfoVisible = false;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsVisible = false;
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        await CheckConnectionAsync();
        if (IsConnected)
        {
            await RefreshModelsAsync();
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        App.OllamaService.BaseUrl = ServerUrl;
        await CheckConnectionAsync();
        if (IsConnected)
        {
            await RefreshModelsAsync();
        }
        IsSettingsVisible = false;
    }

    partial void OnInputMessageChanged(string value)
    {
        OnPropertyChanged(nameof(CanSendMessage));
    }

    partial void OnSelectedModelChanged(string value)
    {
        OnPropertyChanged(nameof(CanSendMessage));
    }
}
