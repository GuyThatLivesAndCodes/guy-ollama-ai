using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GuyOllamaAI.Models;

namespace GuyOllamaAI.Services;

public class OllamaService : IDisposable
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = "http://localhost:11434";

    public string BaseUrl
    {
        get => _baseUrl;
        set
        {
            _baseUrl = value.TrimEnd('/');
        }
    }

    public OllamaService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        var models = new List<string>();

        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var json = JsonDocument.Parse(content);

                if (json.RootElement.TryGetProperty("models", out var modelsElement))
                {
                    foreach (var model in modelsElement.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var nameElement))
                        {
                            models.Add(nameElement.GetString() ?? "unknown");
                        }
                    }
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return models;
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        string model,
        List<ChatMessage> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = model,
            messages = messages.ConvertAll(m => new { role = m.Role.ToLower(), content = m.Content }),
            stream = true
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var responseJson = JsonDocument.Parse(line);

            if (responseJson.RootElement.TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out var contentElement))
            {
                var text = contentElement.GetString();
                if (!string.IsNullOrEmpty(text))
                {
                    yield return text;
                }
            }

            if (responseJson.RootElement.TryGetProperty("done", out var doneElement) &&
                doneElement.GetBoolean())
            {
                break;
            }
        }
    }

    public async Task<string> ChatAsync(
        string model,
        List<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = model,
            messages = messages.ConvertAll(m => new { role = m.Role.ToLower(), content = m.Content }),
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/chat", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseJson = JsonDocument.Parse(responseContent);

        if (responseJson.RootElement.TryGetProperty("message", out var messageElement) &&
            messageElement.TryGetProperty("content", out var contentElement))
        {
            return contentElement.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
