using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace GuyOllamaAI.Services;

public class WorkspaceService
{
    private readonly string _baseWorkspacePath;

    public string BaseWorkspacePath => _baseWorkspacePath;

    public WorkspaceService()
    {
        // Use platform-appropriate base path
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _baseWorkspacePath = @"C:\.guyollamacode";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _baseWorkspacePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".guyollamacode");
        }
        else // Linux
        {
            _baseWorkspacePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".guyollamacode");
        }
    }

    public string CreateWorkspace(Guid sessionId)
    {
        var workspacePath = GetWorkspacePath(sessionId);

        if (!Directory.Exists(workspacePath))
        {
            Directory.CreateDirectory(workspacePath);
        }

        return workspacePath;
    }

    public string GetWorkspacePath(Guid sessionId)
    {
        return Path.Combine(_baseWorkspacePath, sessionId.ToString("N"));
    }

    public bool WorkspaceExists(Guid sessionId)
    {
        return Directory.Exists(GetWorkspacePath(sessionId));
    }

    public void DeleteWorkspace(Guid sessionId)
    {
        var path = GetWorkspacePath(sessionId);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    public List<FileSystemItem> GetWorkspaceContents(string workspacePath, string? relativePath = null)
    {
        var items = new List<FileSystemItem>();
        var targetPath = string.IsNullOrEmpty(relativePath)
            ? workspacePath
            : Path.Combine(workspacePath, relativePath);

        if (!Directory.Exists(targetPath))
            return items;

        try
        {
            // Get directories
            foreach (var dir in Directory.GetDirectories(targetPath))
            {
                var dirInfo = new DirectoryInfo(dir);
                items.Add(new FileSystemItem
                {
                    Name = dirInfo.Name,
                    Path = Path.GetRelativePath(workspacePath, dir),
                    IsDirectory = true,
                    LastModified = dirInfo.LastWriteTime
                });
            }

            // Get files
            foreach (var file in Directory.GetFiles(targetPath))
            {
                var fileInfo = new FileInfo(file);
                items.Add(new FileSystemItem
                {
                    Name = fileInfo.Name,
                    Path = Path.GetRelativePath(workspacePath, file),
                    IsDirectory = false,
                    Size = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip items we can't access
        }

        return items.OrderByDescending(i => i.IsDirectory).ThenBy(i => i.Name).ToList();
    }

    public long GetWorkspaceSize(string workspacePath)
    {
        if (!Directory.Exists(workspacePath))
            return 0;

        return new DirectoryInfo(workspacePath)
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Sum(f => f.Length);
    }
}

public class FileSystemItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }

    public string SizeDisplay => IsDirectory ? "-" : FormatSize(Size);

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}
