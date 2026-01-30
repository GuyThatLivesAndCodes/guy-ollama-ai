using Avalonia.Layout;

namespace GuyOllamaAI.Models;

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsUser => Role.Equals("user", System.StringComparison.OrdinalIgnoreCase);

    public HorizontalAlignment HorizontalAlignment =>
        IsUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;

    public ChatMessage() { }

    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}
