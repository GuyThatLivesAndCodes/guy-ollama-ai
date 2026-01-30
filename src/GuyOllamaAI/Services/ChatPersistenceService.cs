using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GuyOllamaAI.Models;

namespace GuyOllamaAI.Services;

public class ChatPersistenceService
{
    private readonly string _dataDirectory;
    private readonly string _sessionsFile;
    private readonly string _settingsFile;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public ChatPersistenceService()
    {
        // Use appropriate data directory for each platform
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GuyOllamaAI");
        }
        else
        {
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".guyollamaai");
        }

        _sessionsFile = Path.Combine(_dataDirectory, "sessions.json");
        _settingsFile = Path.Combine(_dataDirectory, "settings.json");

        EnsureDataDirectoryExists();
    }

    private void EnsureDataDirectoryExists()
    {
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
    }

    public async Task SaveSessionsAsync(List<ChatSession> sessions)
    {
        try
        {
            var json = JsonSerializer.Serialize(sessions, JsonOptions);
            await File.WriteAllTextAsync(_sessionsFile, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save sessions: {ex.Message}");
        }
    }

    public async Task<List<ChatSession>> LoadSessionsAsync()
    {
        try
        {
            if (!File.Exists(_sessionsFile))
                return new List<ChatSession>();

            var json = await File.ReadAllTextAsync(_sessionsFile).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<ChatSession>>(json, JsonOptions) ?? new List<ChatSession>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load sessions: {ex.Message}");
            return new List<ChatSession>();
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsFile, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsFile))
                return new AppSettings();

            var json = await File.ReadAllTextAsync(_settingsFile).ConfigureAwait(false);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load settings: {ex.Message}");
            return new AppSettings();
        }
    }

    public async Task SaveSessionAsync(ChatSession session)
    {
        var sessions = await LoadSessionsAsync().ConfigureAwait(false);
        var existingIndex = sessions.FindIndex(s => s.Id == session.Id);

        if (existingIndex >= 0)
        {
            sessions[existingIndex] = session;
        }
        else
        {
            sessions.Insert(0, session);
        }

        await SaveSessionsAsync(sessions).ConfigureAwait(false);
    }

    public async Task DeleteSessionAsync(Guid sessionId)
    {
        var sessions = await LoadSessionsAsync().ConfigureAwait(false);
        sessions.RemoveAll(s => s.Id == sessionId);
        await SaveSessionsAsync(sessions).ConfigureAwait(false);
    }
}

public class AppSettings
{
    public string ServerUrl { get; set; } = "http://localhost:11434";
    public string? LastSelectedModel { get; set; }
    public Guid? LastSessionId { get; set; }
}
