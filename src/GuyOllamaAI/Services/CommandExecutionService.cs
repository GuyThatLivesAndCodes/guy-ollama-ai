using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GuyOllamaAI.Services;

public class CommandExecutionService
{
    public async Task<CommandResult> ExecuteAsync(
        string command,
        string workingDirectory,
        int timeoutMs = 60000,
        CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        try
        {
            using var process = new Process();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c {command}";
            }
            else
            {
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"";
            }

            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);

            try
            {
                await process.WaitForExitAsync(cts.Token);
                result.ExitCode = process.ExitCode;
                result.Success = process.ExitCode == 0;
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch { }

                result.Success = false;
                result.TimedOut = true;
                errorBuilder.AppendLine($"Command timed out after {timeoutMs}ms");
            }

            result.Output = outputBuilder.ToString().TrimEnd();
            result.Error = errorBuilder.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Failed to execute command: {ex.Message}";
        }

        return result;
    }

    public async Task<CommandResult> ExecuteScriptAsync(
        string scriptContent,
        string workingDirectory,
        string scriptExtension = ".sh",
        int timeoutMs = 60000,
        CancellationToken cancellationToken = default)
    {
        // Create a temporary script file
        var scriptFileName = $"temp_script_{Guid.NewGuid():N}{scriptExtension}";
        var scriptPath = System.IO.Path.Combine(workingDirectory, scriptFileName);

        try
        {
            await System.IO.File.WriteAllTextAsync(scriptPath, scriptContent, cancellationToken);

            string command;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (scriptExtension == ".ps1")
                {
                    command = $"powershell -ExecutionPolicy Bypass -File \"{scriptPath}\"";
                }
                else
                {
                    command = $"\"{scriptPath}\"";
                }
            }
            else
            {
                // Make script executable on Unix
                await ExecuteAsync($"chmod +x \"{scriptPath}\"", workingDirectory, 5000, cancellationToken);
                command = $"\"{scriptPath}\"";
            }

            return await ExecuteAsync(command, workingDirectory, timeoutMs, cancellationToken);
        }
        finally
        {
            // Clean up the script file
            try
            {
                if (System.IO.File.Exists(scriptPath))
                    System.IO.File.Delete(scriptPath);
            }
            catch { }
        }
    }
}

public class CommandResult
{
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public bool TimedOut { get; set; }

    public string FullOutput => string.IsNullOrEmpty(Error)
        ? Output
        : string.IsNullOrEmpty(Output)
            ? Error
            : $"{Output}\n\nErrors:\n{Error}";
}
