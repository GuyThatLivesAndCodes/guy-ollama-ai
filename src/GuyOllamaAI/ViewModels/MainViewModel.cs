using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuyOllamaAI.Models;
using GuyOllamaAI.Services;

namespace GuyOllamaAI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private CancellationTokenSource? _currentCancellation;
    private ChatSession _currentSession = new();
    private readonly WorkspaceService _workspaceService = new();
    private readonly FileOperationService _fileService = new();
    private readonly CommandExecutionService _commandService = new();
    private readonly CodeActionParser _actionParser = new();
    private readonly ChatPersistenceService _persistenceService = new();
    private bool _isInitialized;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    [ObservableProperty]
    private ObservableCollection<ChatSession> _chatHistory = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableModels = new();

    [ObservableProperty]
    private ObservableCollection<FileSystemItem> _workspaceFiles = new();

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

    [ObservableProperty]
    private ChatMode _currentChatMode = ChatMode.Regular;

    [ObservableProperty]
    private AIStatus _currentAIStatus = AIStatus.Idle;

    [ObservableProperty]
    private string _aiStatusText = "Ready";

    [ObservableProperty]
    private string _workspacePath = string.Empty;

    [ObservableProperty]
    private bool _isNewChatDialogVisible;

    public bool CanSendMessage => !string.IsNullOrWhiteSpace(InputMessage) &&
                                   !CurrentAIStatus.IsWorking() &&
                                   !string.IsNullOrEmpty(SelectedModel) &&
                                   IsConnected;

    public bool IsCodeChat => _currentSession.Mode == ChatMode.Code;

    public MainViewModel()
    {
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        // Load saved settings first
        await LoadSettingsAsync();

        SetStatus(AIStatus.Connecting);
        await CheckConnectionAsync();
        SetStatus(AIStatus.LoadingModels);
        await RefreshModelsAsync();

        // Load saved sessions
        await LoadSessionsAsync();

        SetStatus(AIStatus.Idle);
        _isInitialized = true;
    }

    private void SetStatus(AIStatus status)
    {
        CurrentAIStatus = status;
        AiStatusText = status.ToDisplayString();
        IsLoading = status.IsWorking();
        OnPropertyChanged(nameof(CanSendMessage));
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
        SetStatus(AIStatus.LoadingModels);
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
        SetStatus(AIStatus.Idle);
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputMessage) || CurrentAIStatus.IsWorking() || string.IsNullOrEmpty(SelectedModel))
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

        SetStatus(AIStatus.Thinking);

        try
        {
            _currentCancellation = new CancellationTokenSource();

            // Add assistant message placeholder
            var assistantMessage = new ChatMessage("Assistant", "");
            Messages.Add(assistantMessage);

            // Build messages for API with system prompt for code chat
            var messagesForApi = BuildMessagesForApi();

            SetStatus(AIStatus.Generating);

            // Stream the response with batched UI updates to prevent freezing
            var contentBuilder = new System.Text.StringBuilder();
            var lastUpdateTime = DateTime.UtcNow;
            const int updateIntervalMs = 50; // Update UI every 50ms max

            await foreach (var chunk in App.OllamaService.ChatStreamAsync(
                SelectedModel,
                messagesForApi,
                _currentCancellation.Token))
            {
                contentBuilder.Append(chunk);

                // Batch updates to reduce UI thread pressure
                var now = DateTime.UtcNow;
                if ((now - lastUpdateTime).TotalMilliseconds >= updateIntervalMs)
                {
                    var currentContent = contentBuilder.ToString();
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        assistantMessage.Content = currentContent;
                        var index = Messages.IndexOf(assistantMessage);
                        if (index >= 0)
                        {
                            Messages[index] = assistantMessage;
                        }
                    });
                    lastUpdateTime = now;
                }
            }

            // Final update with complete content
            var finalContent = contentBuilder.ToString();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                assistantMessage.Content = finalContent;
                var index = Messages.IndexOf(assistantMessage);
                if (index >= 0)
                {
                    Messages[index] = assistantMessage;
                }
            });

            // Process code actions if in code chat mode
            if (_currentSession.Mode == ChatMode.Code && !string.IsNullOrEmpty(_currentSession.WorkspacePath))
            {
                await ProcessCodeActionsAsync(assistantMessage.Content);
            }

            // Add final message to session
            _currentSession.Messages.Add(new ChatMessage("Assistant", assistantMessage.Content));
            _currentSession.UpdatedAt = DateTime.Now;

            // Save session after successful message
            await SaveCurrentSessionAsync();
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
            SetStatus(AIStatus.Error);
            await Task.Delay(2000);
        }
        finally
        {
            SetStatus(AIStatus.Idle);
            _currentCancellation?.Dispose();
            _currentCancellation = null;
        }
    }

    private List<ChatMessage> BuildMessagesForApi()
    {
        var messages = new List<ChatMessage>();

        // Add system prompt for code chat
        if (_currentSession.Mode == ChatMode.Code && !string.IsNullOrEmpty(_currentSession.WorkspacePath))
        {
            var systemPrompt = GetCodeChatSystemPrompt();
            messages.Add(new ChatMessage("system", systemPrompt));
        }

        // Add conversation history
        messages.AddRange(_currentSession.Messages);

        return messages;
    }

    private string GetCodeChatSystemPrompt()
    {
        return $@"You are an AI coding assistant with access to a workspace directory. You can perform file and command operations using special action blocks.

WORKSPACE: {_currentSession.WorkspacePath}

ACTION SYNTAX - Use these exact formats in your responses:

WRITE FILE (put the ACTUAL content the user wants, not placeholders):
```action:write
file: filename.txt
The actual file content here - use what the user requested!
```

READ FILE:
```action:read
file: filename.txt
```

DELETE:
```action:delete
file: path/to/delete
```

RENAME/MOVE:
```action:rename
from: oldname.txt
to: newname.txt
```

CREATE DIRECTORY:
```action:mkdir
file: foldername
```

RUN COMMAND:
```action:run
the command here
```

RUN SCRIPT:
```action:script
extension: .bat
script content here
```

CRITICAL RULES:
1. When writing files, use the EXACT content the user requests - never use placeholder text like 'content here'
2. All paths are relative to the workspace
3. Explain what you're doing before each action
4. You can use multiple action blocks in one response
5. For file content, write exactly what the user asks for";
    }

    private async Task ProcessCodeActionsAsync(string response)
    {
        var actions = _actionParser.ParseActions(response);

        foreach (var action in actions)
        {
            try
            {
                await ExecuteCodeActionAsync(action);
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Messages.Add(new ChatMessage("System", $"Action failed: {action.Description}\nError: {ex.Message}"));
                });
            }
        }

        // Refresh workspace files after actions
        if (actions.Count > 0)
        {
            RefreshWorkspaceFiles();
        }
    }

    private async Task ExecuteCodeActionAsync(CodeAction action)
    {
        var workspace = _currentSession.WorkspacePath!;

        switch (action.Type)
        {
            case CodeActionType.WriteFile:
                SetStatus(AIStatus.WritingFiles);
                await _fileService.WriteFileAsync(workspace, action.FilePath, action.Content);
                await AddSystemMessageAsync($"Created/Updated file: {action.FilePath}");
                break;

            case CodeActionType.ReadFile:
                SetStatus(AIStatus.ReadingFiles);
                var content = await _fileService.ReadFileAsync(workspace, action.FilePath);
                await AddSystemMessageAsync($"Read file: {action.FilePath}\n```\n{content}\n```");
                break;

            case CodeActionType.Delete:
                SetStatus(AIStatus.WritingFiles);
                var info = _fileService.GetInfo(workspace, action.FilePath);
                if (info is System.IO.DirectoryInfo)
                    _fileService.DeleteDirectory(workspace, action.FilePath);
                else
                    _fileService.DeleteFile(workspace, action.FilePath);
                await AddSystemMessageAsync($"Deleted: {action.FilePath}");
                break;

            case CodeActionType.Rename:
                SetStatus(AIStatus.WritingFiles);
                _fileService.Rename(workspace, action.FilePath, action.DestinationPath);
                await AddSystemMessageAsync($"Renamed: {action.FilePath} -> {action.DestinationPath}");
                break;

            case CodeActionType.CreateDirectory:
                SetStatus(AIStatus.WritingFiles);
                _fileService.CreateDirectory(workspace, action.FilePath);
                await AddSystemMessageAsync($"Created directory: {action.FilePath}");
                break;

            case CodeActionType.RunCommand:
                SetStatus(AIStatus.ExecutingCommand);
                var cmdResult = await _commandService.ExecuteAsync(action.Command, workspace);
                await AddSystemMessageAsync($"Command: {action.Command}\nExit code: {cmdResult.ExitCode}\n```\n{cmdResult.FullOutput}\n```");
                break;

            case CodeActionType.RunScript:
                SetStatus(AIStatus.ExecutingCommand);
                var scriptResult = await _commandService.ExecuteScriptAsync(
                    action.Content, workspace, action.ScriptExtension);
                await AddSystemMessageAsync($"Script executed\nExit code: {scriptResult.ExitCode}\n```\n{scriptResult.FullOutput}\n```");
                break;
        }
    }

    private async Task AddSystemMessageAsync(string message)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Messages.Add(new ChatMessage("System", message));
        });
    }

    [RelayCommand]
    private void ShowNewChatDialog()
    {
        IsNewChatDialogVisible = true;
    }

    [RelayCommand]
    private void CloseNewChatDialog()
    {
        IsNewChatDialogVisible = false;
    }

    [RelayCommand]
    private void NewRegularChat()
    {
        _currentSession = new ChatSession { Mode = ChatMode.Regular };
        Messages.Clear();
        CurrentChatTitle = "New Chat";
        CurrentChatMode = ChatMode.Regular;
        WorkspacePath = string.Empty;
        WorkspaceFiles.Clear();
        InputMessage = string.Empty;
        IsNewChatDialogVisible = false;
        OnPropertyChanged(nameof(IsCodeChat));
    }

    [RelayCommand]
    private void NewCodeChat()
    {
        SetStatus(AIStatus.CreatingWorkspace);

        _currentSession = new ChatSession { Mode = ChatMode.Code };
        _currentSession.WorkspacePath = _workspaceService.CreateWorkspace(_currentSession.Id);

        Messages.Clear();
        CurrentChatTitle = "[Code] New Chat";
        CurrentChatMode = ChatMode.Code;
        WorkspacePath = _currentSession.WorkspacePath;
        RefreshWorkspaceFiles();
        InputMessage = string.Empty;
        IsNewChatDialogVisible = false;

        // Add welcome message
        Messages.Add(new ChatMessage("System",
            $"Code Chat initialized!\n\nWorkspace: {WorkspacePath}\n\nThe AI can now execute commands and manage files in this workspace."));

        OnPropertyChanged(nameof(IsCodeChat));
        SetStatus(AIStatus.Idle);
    }

    [RelayCommand]
    private void NewChat()
    {
        // Show dialog to choose chat type
        ShowNewChatDialog();
    }

    [RelayCommand]
    private void RefreshWorkspaceFiles()
    {
        if (string.IsNullOrEmpty(_currentSession.WorkspacePath))
            return;

        var files = _workspaceService.GetWorkspaceContents(_currentSession.WorkspacePath);

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            WorkspaceFiles.Clear();
            foreach (var file in files)
            {
                WorkspaceFiles.Add(file);
            }
        });
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
        CurrentChatMode = session.Mode;
        WorkspacePath = session.WorkspacePath ?? string.Empty;

        if (session.Mode == ChatMode.Code && !string.IsNullOrEmpty(session.WorkspacePath))
        {
            RefreshWorkspaceFiles();
        }
        else
        {
            WorkspaceFiles.Clear();
        }

        OnPropertyChanged(nameof(IsCodeChat));
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
        SetStatus(AIStatus.Connecting);
        await CheckConnectionAsync();
        if (IsConnected)
        {
            await RefreshModelsAsync();
        }
        SetStatus(AIStatus.Idle);
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        App.OllamaService.BaseUrl = ServerUrl;
        SetStatus(AIStatus.Connecting);
        await CheckConnectionAsync();
        if (IsConnected)
        {
            await RefreshModelsAsync();
        }
        IsSettingsVisible = false;
        SetStatus(AIStatus.Idle);

        // Persist settings to disk
        await PersistSettingsAsync();
    }

    [RelayCommand]
    private async Task OpenWorkspaceFolderAsync()
    {
        if (string.IsNullOrEmpty(WorkspacePath))
            return;

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = WorkspacePath,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            await AddSystemMessageAsync($"Could not open folder: {ex.Message}");
        }
    }

    partial void OnInputMessageChanged(string value)
    {
        OnPropertyChanged(nameof(CanSendMessage));
    }

    partial void OnSelectedModelChanged(string value)
    {
        OnPropertyChanged(nameof(CanSendMessage));
    }

    #region Persistence

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await Task.Run(() => _persistenceService.LoadSettingsAsync());
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!string.IsNullOrEmpty(settings.ServerUrl))
                {
                    ServerUrl = settings.ServerUrl;
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }

    private async Task LoadSessionsAsync()
    {
        try
        {
            var sessions = await Task.Run(() => _persistenceService.LoadSessionsAsync());
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ChatHistory.Clear();
                foreach (var session in sessions.OrderByDescending(s => s.UpdatedAt))
                {
                    ChatHistory.Add(session);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load sessions: {ex.Message}");
        }
    }

    private async Task SaveCurrentSessionAsync()
    {
        if (!_isInitialized) return;

        try
        {
            await Task.Run(() => _persistenceService.SaveSessionAsync(_currentSession));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save session: {ex.Message}");
        }
    }

    private async Task PersistSettingsAsync()
    {
        if (!_isInitialized) return;

        try
        {
            var settings = new AppSettings
            {
                ServerUrl = ServerUrl,
                LastSelectedModel = SelectedModel,
                LastSessionId = _currentSession?.Id
            };
            await Task.Run(() => _persistenceService.SaveSettingsAsync(settings));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    #endregion
}
