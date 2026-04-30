using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace LearnToCode.API.Services;

public class CodeRunResult
{
    public string Stdout { get; set; } = string.Empty;
    public string Stderr { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public bool TimedOut { get; set; }
}

public class DockerCodeRunnerService
{
    public static readonly TimeSpan ExecutionTimeout = TimeSpan.FromSeconds(5);

    // Wraps user code so that sys.stdin is pre-loaded with the test input.
    // JSON serialization produces a Python-compatible string literal for any input content.
    private static string BuildPythonScript(string userCode, string testInput)
    {
        var inputJson = JsonSerializer.Serialize(testInput);
        return $"import sys, io as _io\nsys.stdin = _io.StringIO({inputJson})\n{userCode}\n";
    }

    private static string BuildJavaShellScript(string userCode, string testInput)
    {
        var codeBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(userCode));
        var inputBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(testInput));

        return string.Join("\n", new[]
        {
            "set -eu",
            $"printf '%s' '{codeBase64}' | base64 -d > /tmp/Main.java",
            $"printf '%s' '{inputBase64}' | base64 -d > /tmp/input.txt",
            "javac -encoding UTF-8 -d /tmp /tmp/Main.java",
            "java -cp /tmp Main < /tmp/input.txt",
        });
    }

    public async Task<CodeRunResult> RunAsync(string code, string input, string language, CancellationToken ct)
    {
        var normalizedLanguage = language.Trim().ToLowerInvariant();

        return normalizedLanguage switch
        {
            "python" => await RunPythonAsync(code, input, ct),
            "java" => await RunJavaAsync(code, input, ct),
            _ => new CodeRunResult
            {
                ExitCode = -1,
                Stderr = "Neatbalstīta programmēšanas valoda.",
            },
        };
    }

    private async Task<CodeRunResult> RunPythonAsync(string code, string input, CancellationToken ct)
    {
        var script = BuildPythonScript(code, input);

        return await RunContainerAsync(
            "python:3.11-slim",
            new[] { "python3", "-" },
            script,
            ct);
    }

    private async Task<CodeRunResult> RunJavaAsync(string code, string input, CancellationToken ct)
    {
        return await RunContainerAsync(
            "eclipse-temurin:21-jdk",
            new[] { "sh", "-s" },
            BuildJavaShellScript(code, input),
            ct);
    }

    private async Task<CodeRunResult> RunContainerAsync(
        string image,
        IEnumerable<string> commandArgs,
        string? stdinPayload,
        CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            // Ensure UTF-8 across stdin/stdout/stderr so Latvian characters in user
            // comments / string literals survive the round trip into the container.
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        // Tell the in-container Python interpreter that stdin is UTF-8 too — without
        // this, Python falls back to the locale (often POSIX/ASCII) and chokes on
        // diacritic-bearing comments such as `# Raksti risinājumu šeit`.
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.Environment["PYTHONUTF8"] = "1";
        startInfo.Environment["LANG"] = "C.UTF-8";

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--rm");
        startInfo.ArgumentList.Add("--network");
        startInfo.ArgumentList.Add("none");
        startInfo.ArgumentList.Add("--memory");
        startInfo.ArgumentList.Add("128m");
        startInfo.ArgumentList.Add("--cpus");
        startInfo.ArgumentList.Add("0.5");
        startInfo.ArgumentList.Add("-i");
        // Force UTF-8 inside the container so non-ASCII comments / string literals
        // (e.g. "Raksti risinājumu šeit") don't trip Python's default-locale parser.
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("PYTHONIOENCODING=utf-8");
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("PYTHONUTF8=1");
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("LANG=C.UTF-8");
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("LC_ALL=C.UTF-8");
        startInfo.ArgumentList.Add(image);

        foreach (var argument in commandArgs)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(ExecutionTimeout);
        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            if (stdinPayload is not null)
            {
                await process.StandardInput.WriteAsync(stdinPayload);
            }

            process.StandardInput.Close();

            await process.WaitForExitAsync(timeoutCts.Token);
            await Task.WhenAll(stdoutTask, stderrTask);

            return new CodeRunResult
            {
                Stdout = await stdoutTask,
                Stderr = await stderrTask,
                ExitCode = process.ExitCode,
                TimedOut = false,
            };
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            TryKillProcess(process);
            return new CodeRunResult { TimedOut = true, ExitCode = -1 };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new CodeRunResult { ExitCode = -1, Stderr = ex.Message };
        }
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // The process may have exited between the timeout and cleanup.
        }
    }
}
