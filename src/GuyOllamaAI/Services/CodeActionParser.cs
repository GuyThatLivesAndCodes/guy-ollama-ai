using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GuyOllamaAI.Services;

public class CodeActionParser
{
    // Pattern to match action blocks: ```action:type ... ```
    private static readonly Regex ActionBlockPattern = new(
        @"```action:(\w+)\s*\n([\s\S]*?)```",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // Pattern to match file path in action (first line after action type)
    private static readonly Regex FilePathPattern = new(
        @"^file:\s*(.+)$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public List<CodeAction> ParseActions(string response)
    {
        var actions = new List<CodeAction>();
        var matches = ActionBlockPattern.Matches(response);

        foreach (Match match in matches)
        {
            var actionType = match.Groups[1].Value.ToLower();
            var content = match.Groups[2].Value;

            var action = ParseAction(actionType, content);
            if (action != null)
            {
                action.RawBlock = match.Value;
                actions.Add(action);
            }
        }

        return actions;
    }

    private CodeAction? ParseAction(string type, string content)
    {
        return type switch
        {
            "write" or "create" => ParseWriteAction(content),
            "read" => ParseReadAction(content),
            "delete" => ParseDeleteAction(content),
            "rename" or "move" => ParseRenameAction(content),
            "mkdir" or "createdir" => ParseMkdirAction(content),
            "run" or "execute" or "cmd" or "command" => ParseCommandAction(content),
            "script" => ParseScriptAction(content),
            _ => null
        };
    }

    private CodeAction ParseWriteAction(string content)
    {
        var lines = content.Split('\n', 2);
        var fileMatch = FilePathPattern.Match(lines[0]);

        return new CodeAction
        {
            Type = CodeActionType.WriteFile,
            FilePath = fileMatch.Success ? fileMatch.Groups[1].Value.Trim() : lines[0].Trim(),
            Content = lines.Length > 1 ? lines[1].TrimStart('\n') : string.Empty
        };
    }

    private CodeAction ParseReadAction(string content)
    {
        var fileMatch = FilePathPattern.Match(content);
        var filePath = fileMatch.Success ? fileMatch.Groups[1].Value.Trim() : content.Trim().Split('\n')[0];

        return new CodeAction
        {
            Type = CodeActionType.ReadFile,
            FilePath = filePath
        };
    }

    private CodeAction ParseDeleteAction(string content)
    {
        var fileMatch = FilePathPattern.Match(content);
        var filePath = fileMatch.Success ? fileMatch.Groups[1].Value.Trim() : content.Trim().Split('\n')[0];

        return new CodeAction
        {
            Type = CodeActionType.Delete,
            FilePath = filePath
        };
    }

    private CodeAction ParseRenameAction(string content)
    {
        var lines = content.Trim().Split('\n');
        var fromPath = "";
        var toPath = "";

        foreach (var line in lines)
        {
            if (line.StartsWith("from:", StringComparison.OrdinalIgnoreCase))
                fromPath = line.Substring(5).Trim();
            else if (line.StartsWith("to:", StringComparison.OrdinalIgnoreCase))
                toPath = line.Substring(3).Trim();
        }

        // Fallback: if no from:/to: prefix, use first two lines
        if (string.IsNullOrEmpty(fromPath) && lines.Length >= 2)
        {
            fromPath = lines[0].Trim();
            toPath = lines[1].Trim();
        }

        return new CodeAction
        {
            Type = CodeActionType.Rename,
            FilePath = fromPath,
            DestinationPath = toPath
        };
    }

    private CodeAction ParseMkdirAction(string content)
    {
        var fileMatch = FilePathPattern.Match(content);
        var dirPath = fileMatch.Success ? fileMatch.Groups[1].Value.Trim() : content.Trim().Split('\n')[0];

        return new CodeAction
        {
            Type = CodeActionType.CreateDirectory,
            FilePath = dirPath
        };
    }

    private CodeAction ParseCommandAction(string content)
    {
        return new CodeAction
        {
            Type = CodeActionType.RunCommand,
            Command = content.Trim()
        };
    }

    private CodeAction ParseScriptAction(string content)
    {
        var lines = content.Split('\n', 2);
        var extension = ".sh";

        // Check for extension hint on first line
        var firstLine = lines[0].Trim().ToLower();
        if (firstLine.StartsWith("extension:"))
        {
            extension = firstLine.Substring(10).Trim();
            if (!extension.StartsWith(".")) extension = "." + extension;
            content = lines.Length > 1 ? lines[1] : "";
        }

        return new CodeAction
        {
            Type = CodeActionType.RunScript,
            Content = content.Trim(),
            ScriptExtension = extension
        };
    }

    public string RemoveActionBlocks(string response)
    {
        return ActionBlockPattern.Replace(response, "").Trim();
    }
}

public enum CodeActionType
{
    WriteFile,
    ReadFile,
    Delete,
    Rename,
    CreateDirectory,
    RunCommand,
    RunScript
}

public class CodeAction
{
    public CodeActionType Type { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string ScriptExtension { get; set; } = ".sh";
    public string RawBlock { get; set; } = string.Empty;

    public string Description => Type switch
    {
        CodeActionType.WriteFile => $"Write file: {FilePath}",
        CodeActionType.ReadFile => $"Read file: {FilePath}",
        CodeActionType.Delete => $"Delete: {FilePath}",
        CodeActionType.Rename => $"Rename: {FilePath} -> {DestinationPath}",
        CodeActionType.CreateDirectory => $"Create directory: {FilePath}",
        CodeActionType.RunCommand => $"Run command: {(Command.Length > 50 ? Command[..50] + "..." : Command)}",
        CodeActionType.RunScript => $"Run script ({ScriptExtension})",
        _ => "Unknown action"
    };
}
