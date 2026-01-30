using System;
using System.IO;
using System.Threading.Tasks;

namespace GuyOllamaAI.Services;

public class FileOperationService
{
    /// <summary>
    /// Validates that the path is within the allowed workspace
    /// </summary>
    private string ValidatePath(string workspacePath, string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(workspacePath, relativePath));
        var normalizedWorkspace = Path.GetFullPath(workspacePath);

        if (!fullPath.StartsWith(normalizedWorkspace, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Access denied: Path is outside workspace");
        }

        return fullPath;
    }

    public async Task<string> ReadFileAsync(string workspacePath, string relativePath)
    {
        var fullPath = ValidatePath(workspacePath, relativePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {relativePath}");
        }

        return await File.ReadAllTextAsync(fullPath);
    }

    public async Task WriteFileAsync(string workspacePath, string relativePath, string content)
    {
        var fullPath = ValidatePath(workspacePath, relativePath);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(fullPath, content);
    }

    public async Task AppendFileAsync(string workspacePath, string relativePath, string content)
    {
        var fullPath = ValidatePath(workspacePath, relativePath);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.AppendAllTextAsync(fullPath, content);
    }

    public void CreateDirectory(string workspacePath, string relativePath)
    {
        var fullPath = ValidatePath(workspacePath, relativePath);
        Directory.CreateDirectory(fullPath);
    }

    public void DeleteFile(string workspacePath, string relativePath)
    {
        var fullPath = ValidatePath(workspacePath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        else
        {
            throw new FileNotFoundException($"File not found: {relativePath}");
        }
    }

    public void DeleteDirectory(string workspacePath, string relativePath, bool recursive = true)
    {
        var fullPath = ValidatePath(workspacePath, relativePath);

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive);
        }
        else
        {
            throw new DirectoryNotFoundException($"Directory not found: {relativePath}");
        }
    }

    public void Rename(string workspacePath, string oldRelativePath, string newRelativePath)
    {
        var oldFullPath = ValidatePath(workspacePath, oldRelativePath);
        var newFullPath = ValidatePath(workspacePath, newRelativePath);

        if (File.Exists(oldFullPath))
        {
            // Ensure target directory exists
            var targetDir = Path.GetDirectoryName(newFullPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.Move(oldFullPath, newFullPath);
        }
        else if (Directory.Exists(oldFullPath))
        {
            Directory.Move(oldFullPath, newFullPath);
        }
        else
        {
            throw new FileNotFoundException($"Path not found: {oldRelativePath}");
        }
    }

    public void Copy(string workspacePath, string sourceRelativePath, string destRelativePath)
    {
        var sourceFullPath = ValidatePath(workspacePath, sourceRelativePath);
        var destFullPath = ValidatePath(workspacePath, destRelativePath);

        if (File.Exists(sourceFullPath))
        {
            // Ensure target directory exists
            var targetDir = Path.GetDirectoryName(destFullPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.Copy(sourceFullPath, destFullPath, overwrite: true);
        }
        else if (Directory.Exists(sourceFullPath))
        {
            CopyDirectory(sourceFullPath, destFullPath);
        }
        else
        {
            throw new FileNotFoundException($"Path not found: {sourceRelativePath}");
        }
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    public bool Exists(string workspacePath, string relativePath)
    {
        var fullPath = ValidatePath(workspacePath, relativePath);
        return File.Exists(fullPath) || Directory.Exists(fullPath);
    }

    public FileSystemInfo? GetInfo(string workspacePath, string relativePath)
    {
        var fullPath = ValidatePath(workspacePath, relativePath);

        if (File.Exists(fullPath))
            return new FileInfo(fullPath);
        if (Directory.Exists(fullPath))
            return new DirectoryInfo(fullPath);

        return null;
    }
}
