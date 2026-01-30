using System;
using System.Collections.Generic;

namespace GuyOllamaAI.Models;

public class ChatSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "New Chat";
    public List<ChatMessage> Messages { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public void UpdateTitle()
    {
        if (Messages.Count > 0)
        {
            var firstMessage = Messages[0].Content;
            Title = firstMessage.Length > 30
                ? firstMessage.Substring(0, 30) + "..."
                : firstMessage;
        }
    }
}
