namespace GuyOllamaAI.Models;

public enum AIStatus
{
    Idle,
    Connecting,
    LoadingModels,
    SwitchingModel,
    Thinking,
    Generating,
    ExecutingCommand,
    ReadingFiles,
    WritingFiles,
    CreatingWorkspace,
    Error
}

public static class AIStatusExtensions
{
    public static string ToDisplayString(this AIStatus status) => status switch
    {
        AIStatus.Idle => "Ready",
        AIStatus.Connecting => "Connecting to Ollama...",
        AIStatus.LoadingModels => "Loading available models...",
        AIStatus.SwitchingModel => "Switching model...",
        AIStatus.Thinking => "Thinking...",
        AIStatus.Generating => "Generating response...",
        AIStatus.ExecutingCommand => "Executing command...",
        AIStatus.ReadingFiles => "Reading files...",
        AIStatus.WritingFiles => "Writing files...",
        AIStatus.CreatingWorkspace => "Creating workspace...",
        AIStatus.Error => "Error occurred",
        _ => "Unknown"
    };

    public static bool IsWorking(this AIStatus status) => status switch
    {
        AIStatus.Idle => false,
        AIStatus.Error => false,
        _ => true
    };
}
