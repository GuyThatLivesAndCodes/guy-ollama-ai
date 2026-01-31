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
            "readlines" => ParseReadLinesAction(content),
            "append" => ParseAppendAction(content),
            "replace" => ParseReplaceAction(content),
            "insert" => ParseInsertAction(content),
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

    private CodeAction ParseAppendAction(string content)
    {
        var lines = content.Split('\n', 2);
        var fileMatch = FilePathPattern.Match(lines[0]);

        return new CodeAction
        {
            Type = CodeActionType.AppendFile,
            FilePath = fileMatch.Success ? fileMatch.Groups[1].Value.Trim() : lines[0].Trim(),
            Content = lines.Length > 1 ? lines[1].TrimStart('\n') : string.Empty
        };
    }

    private CodeAction ParseReplaceAction(string content)
    {
        var lines = content.Trim().Split('\n');
        var filePath = "";
        var searchText = "";
        var replaceText = "";
        var inSearch = false;
        var inReplace = false;
        var searchLines = new List<string>();
        var replaceLines = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                filePath = line.Substring(5).Trim();
                inSearch = false;
                inReplace = false;
            }
            else if (line.StartsWith("search:", StringComparison.OrdinalIgnoreCase))
            {
                var searchValue = line.Substring(7).Trim();
                if (!string.IsNullOrEmpty(searchValue))
                    searchLines.Add(searchValue);
                inSearch = true;
                inReplace = false;
            }
            else if (line.StartsWith("replace:", StringComparison.OrdinalIgnoreCase))
            {
                var replaceValue = line.Substring(8).Trim();
                if (!string.IsNullOrEmpty(replaceValue))
                    replaceLines.Add(replaceValue);
                inSearch = false;
                inReplace = true;
            }
            else if (inSearch)
            {
                searchLines.Add(line);
            }
            else if (inReplace)
            {
                replaceLines.Add(line);
            }
        }

        searchText = string.Join("\n", searchLines);
        replaceText = string.Join("\n", replaceLines);

        return new CodeAction
        {
            Type = CodeActionType.ReplaceInFile,
            FilePath = filePath,
            SearchText = searchText,
            ReplaceText = replaceText
        };
    }

    private CodeAction ParseInsertAction(string content)
    {
        var lines = content.Trim().Split('\n');
        var filePath = "";
        var lineNumber = 0;
        var contentLines = new List<string>();
        var inContent = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                filePath = line.Substring(5).Trim();
            }
            else if (line.StartsWith("line:", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(line.Substring(5).Trim(), out lineNumber);
                inContent = true;
            }
            else if (inContent)
            {
                contentLines.Add(line);
            }
        }

        return new CodeAction
        {
            Type = CodeActionType.InsertAtLine,
            FilePath = filePath,
            StartLine = lineNumber,
            Content = string.Join("\n", contentLines)
        };
    }

    private CodeAction ParseReadLinesAction(string content)
    {
        var lines = content.Trim().Split('\n');
        var filePath = "";
        var startLine = 1;
        var endLine = -1; // -1 means to end of file

        foreach (var line in lines)
        {
            if (line.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                filePath = line.Substring(5).Trim();
            }
            else if (line.StartsWith("start:", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(line.Substring(6).Trim(), out startLine);
            }
            else if (line.StartsWith("end:", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(line.Substring(4).Trim(), out endLine);
            }
            else if (line.StartsWith("lines:", StringComparison.OrdinalIgnoreCase))
            {
                // Support "lines: 5-10" format
                var range = line.Substring(6).Trim();
                var parts = range.Split('-');
                if (parts.Length == 2)
                {
                    int.TryParse(parts[0].Trim(), out startLine);
                    int.TryParse(parts[1].Trim(), out endLine);
                }
            }
        }

        return new CodeAction
        {
            Type = CodeActionType.ReadLines,
            FilePath = filePath,
            StartLine = startLine,
            EndLine = endLine
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
    ReadLines,
    AppendFile,
    ReplaceInFile,
    InsertAtLine,
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

    // For replace operations
    public string SearchText { get; set; } = string.Empty;
    public string ReplaceText { get; set; } = string.Empty;

    // For line-based operations
    public int StartLine { get; set; }
    public int EndLine { get; set; }

    public string Description => Type switch
    {
        CodeActionType.WriteFile => $"Write file: {FilePath}",
        CodeActionType.ReadFile => $"Read file: {FilePath}",
        CodeActionType.ReadLines => $"Read lines {StartLine}-{EndLine} from: {FilePath}",
        CodeActionType.AppendFile => $"Append to file: {FilePath}",
        CodeActionType.ReplaceInFile => $"Replace in file: {FilePath}",
        CodeActionType.InsertAtLine => $"Insert at line {StartLine} in: {FilePath}",
        CodeActionType.Delete => $"Delete: {FilePath}",
        CodeActionType.Rename => $"Rename: {FilePath} -> {DestinationPath}",
        CodeActionType.CreateDirectory => $"Create directory: {FilePath}",
        CodeActionType.RunCommand => $"Run command: {(Command.Length > 50 ? Command[..50] + "..." : Command)}",
        CodeActionType.RunScript => $"Run script ({ScriptExtension})",
        _ => "Unknown action"
    };
}
