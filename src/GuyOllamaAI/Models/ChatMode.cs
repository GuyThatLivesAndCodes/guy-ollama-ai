namespace GuyOllamaAI.Models;

public enum ChatMode
{
    Regular,
    Code
}

public static class ChatModeExtensions
{
    public static string ToDisplayString(this ChatMode mode) => mode switch
    {
        ChatMode.Regular => "Regular Chat",
        ChatMode.Code => "Code Chat",
        _ => "Unknown"
    };

    public static string GetDescription(this ChatMode mode) => mode switch
    {
        ChatMode.Regular => "Have a normal conversation with the AI",
        ChatMode.Code => "AI can execute commands and manage files in a workspace",
        _ => ""
    };
}
