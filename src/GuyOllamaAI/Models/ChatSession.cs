using System;
using System.Collections.Generic;

namespace GuyOllamaAI.Models;

public class ChatSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "New Chat";
    public ChatMode Mode { get; set; } = ChatMode.Regular;
    public string? WorkspacePath { get; set; }
    public List<ChatMessage> Messages { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public void UpdateTitle()
    {
        if (Messages.Count > 0)
        {
            var prefix = Mode == ChatMode.Code ? "[Code] " : "";
            var firstMessage = Messages[0].Content;
            var maxLen = 30 - prefix.Length;
            Title = prefix + (firstMessage.Length > maxLen
                ? firstMessage.Substring(0, maxLen) + "..."
                : firstMessage);
        }
    }
}
