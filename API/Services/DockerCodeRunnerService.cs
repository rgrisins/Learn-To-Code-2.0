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
    // Viena testa izpildes limits, ko ievēro konteinera iekšienē.
    public static readonly TimeSpan ExecutionTimeout = TimeSpan.FromSeconds(5);

    // Papildu laiks kompilēšanai un konteinera sagatavošanai.
    private static readonly TimeSpan BatchOverhead = TimeSpan.FromSeconds(20);

    private readonly string? _dockerHost;

    public DockerCodeRunnerService(IConfiguration configuration)
    {
        // Lasa Docker:Host no appsettings vai vides:
        //   ssh://riddle@server.lat   — caur SSH (ieteicams attālinātam serverim)
        //   tcp://server.lat:2375     — TCP (tikai LAN / aiz VPN)
        //   tukšs                     — lokālais Docker socket / Windows pipe
        _dockerHost = configuration["Docker:Host"];
    }

    // Viena testa izpilde atsevišķiem pārbaudes gadījumiem.

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
        // Vienkāršotā gadījumā delegējam batch metodei ar 1 testu — tā varam
        // paturēt vienu palaišanas ceļu un izvairīties no koda dublēšanās.
        var batch = await RunBatchAsync(code, new[] { input }, language, onResult: null, ct);
        return batch[0];
    }

    // Vairāku testu izpilde vienā konteinerā.

    /// <summary>
    /// Palaiž lietotāja kodu ar visiem testiem vienā Docker konteinerā.
    /// Kompilēšana vai interpretatora startēšana notiek vienu reizi, bet
    /// katrs testa rezultāts tiek atdots secīgi arī progresa plūsmai.
    /// </summary>
    public async Task<IReadOnlyList<CodeRunResult>> RunBatchAsync(
        string code,
        IReadOnlyList<string> inputs,
        string language,
        Func<int, CodeRunResult, CancellationToken, Task>? onResult,
        CancellationToken ct)
    {
        if (inputs.Count == 0)
        {
            return Array.Empty<CodeRunResult>();
        }

        var normalizedLanguage = language.Trim().ToLowerInvariant();

        return normalizedLanguage switch
        {
            "python" => await RunBatchInternalAsync(
                image: "python:3.11-slim",
                command: new[] { "python3", "-" },
                stdinPayload: BuildPythonOrchestrator(code, inputs),
                inputs.Count,
                onResult,
                ct),
            "java" => await RunBatchInternalAsync(
                image: "eclipse-temurin:21-jdk",
                command: new[] { "bash", "-s" },
                stdinPayload: BuildJavaOrchestrator(code, inputs),
                inputs.Count,
                onResult,
                ct),
            _ => CreateUnsupportedLanguageResults(inputs.Count),
        };
    }

    private static IReadOnlyList<CodeRunResult> CreateUnsupportedLanguageResults(int count)
    {
        var results = new CodeRunResult[count];
        for (var i = 0; i < count; i++)
        {
            results[i] = new CodeRunResult
            {
                ExitCode = -1,
                Stderr = "Neatbalstīta programmēšanas valoda.",
            };
        }
        return results;
    }

    // Orķestratoru veidošana konkrētām valodām.

    // Python kods katram testam tiek palaists jaunā procesā, lai testi
    // neietekmētu cits citu ar globāliem mainīgajiem.
    private static string BuildPythonOrchestrator(string userCode, IReadOnlyList<string> inputs)
    {
        var codeB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(userCode));
        var inputsArrayLiteral = string.Join(",\n    ",
            inputs.Select(i => "\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(i)) + "\""));

        var timeoutSec = ExecutionTimeout.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return $@"import sys, os, base64, subprocess, io
USER_CODE_B64 = ""{codeB64}""
INPUTS_B64 = [
    {inputsArrayLiteral}
]
TIMEOUT = {timeoutSec}

with open('/tmp/u.py', 'wb') as f:
    f.write(base64.b64decode(USER_CODE_B64))

env = dict(os.environ)
env['PYTHONIOENCODING'] = 'utf-8'
env['PYTHONUTF8'] = '1'
env['LANG'] = 'C.UTF-8'
env['LC_ALL'] = 'C.UTF-8'

for i, b64 in enumerate(INPUTS_B64):
    test_input = base64.b64decode(b64).decode('utf-8', errors='replace')
    timed_out = False
    exit_code = 0
    out = ''
    err = ''
    try:
        r = subprocess.run(
            ['python3', '/tmp/u.py'],
            input=test_input,
            capture_output=True,
            text=True,
            encoding='utf-8',
            errors='replace',
            timeout=TIMEOUT,
            env=env,
        )
        out = r.stdout or ''
        err = r.stderr or ''
        exit_code = r.returncode
    except subprocess.TimeoutExpired as e:
        out = (e.stdout or '') if isinstance(e.stdout, str) else (e.stdout.decode('utf-8','replace') if e.stdout else '')
        err = (e.stderr or '') if isinstance(e.stderr, str) else (e.stderr.decode('utf-8','replace') if e.stderr else '')
        timed_out = True
        exit_code = -1
    except Exception as e:
        err = repr(e)
        exit_code = -1

    sys.stdout.write('===TEST===' + str(i) + '===\n')
    sys.stdout.write(base64.b64encode(out.encode('utf-8')).decode('ascii') + '\n')
    sys.stdout.write('===STDERR===\n')
    sys.stdout.write(base64.b64encode(err.encode('utf-8')).decode('ascii') + '\n')
    sys.stdout.write('===EXIT===' + str(exit_code) + '===\n')
    sys.stdout.write('===TIMEOUT===' + ('1' if timed_out else '0') + '===\n')
    sys.stdout.write('===END===\n')
    sys.stdout.flush()
";
    }

    // Java kodu kompilē vienreiz, pēc tam katru testu palaiž ar atsevišķu
    // ievadi un vienādu rezultātu marķēšanas formātu.
    private static string BuildJavaOrchestrator(string userCode, IReadOnlyList<string> inputs)
    {
        var codeB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(userCode));
        var sb = new StringBuilder();
        sb.AppendLine("set -u");
        sb.AppendLine($"printf '%s' '{codeB64}' | base64 -d > /tmp/Main.java");
        sb.AppendLine("export LANG=C.UTF-8 LC_ALL=C.UTF-8");
        sb.AppendLine();
        // Ja kompilēšana neizdodas, katram testam atgriež vienādu kļūdu.
        sb.AppendLine("if ! javac -encoding UTF-8 -d /tmp /tmp/Main.java 2>/tmp/javac.err; then");
        sb.AppendLine("  COMPILE_ERR_B64=$(base64 -w0 < /tmp/javac.err)");
        sb.AppendLine($"  for i in $(seq 0 {inputs.Count - 1}); do");
        sb.AppendLine("    echo \"===TEST===$i===\"");
        sb.AppendLine("    echo \"\"");
        sb.AppendLine("    echo \"===STDERR===\"");
        sb.AppendLine("    echo \"$COMPILE_ERR_B64\"");
        sb.AppendLine("    echo \"===EXIT===1===\"");
        sb.AppendLine("    echo \"===TIMEOUT===0===\"");
        sb.AppendLine("    echo \"===END===\"");
        sb.AppendLine("  done");
        sb.AppendLine("  exit 0");
        sb.AppendLine("fi");
        sb.AppendLine();

        // Katram testam sagatavo ievadi un nolasa izvadi.
        for (var i = 0; i < inputs.Count; i++)
        {
            var inputB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(inputs[i]));
            sb.AppendLine($"printf '%s' '{inputB64}' | base64 -d > /tmp/in_{i}.txt");
            sb.AppendLine($"timeout {ExecutionTimeout.TotalSeconds:0}s java -cp /tmp Main < /tmp/in_{i}.txt > /tmp/out_{i}.txt 2> /tmp/err_{i}.txt");
            sb.AppendLine($"EXIT_{i}=$?");
            sb.AppendLine($"if [ \"$EXIT_{i}\" = \"124\" ]; then TO_{i}=1; else TO_{i}=0; fi");
            sb.AppendLine($"echo \"===TEST==={i}===\"");
            sb.AppendLine($"base64 -w0 < /tmp/out_{i}.txt; echo");
            sb.AppendLine("echo \"===STDERR===\"");
            sb.AppendLine($"base64 -w0 < /tmp/err_{i}.txt; echo");
            sb.AppendLine($"echo \"===EXIT===$EXIT_{i}===\"");
            sb.AppendLine($"echo \"===TIMEOUT===$TO_{i}===\"");
            sb.AppendLine("echo \"===END===\"");
        }

        return sb.ToString();
    }

    // Konteinera palaišana un rezultātu nolasīšana plūsmā.

    private async Task<IReadOnlyList<CodeRunResult>> RunBatchInternalAsync(
        string image,
        IEnumerable<string> command,
        string stdinPayload,
        int testCount,
        Func<int, CodeRunResult, CancellationToken, Task>? onResult,
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
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.Environment["PYTHONUTF8"] = "1";
        startInfo.Environment["LANG"] = "C.UTF-8";

        if (!string.IsNullOrWhiteSpace(_dockerHost))
        {
            startInfo.Environment["DOCKER_HOST"] = _dockerHost;
        }

        // Resursu limits ir kopīgs orķestratoram un visiem testa procesiem.
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--rm");
        startInfo.ArgumentList.Add("--network");
        startInfo.ArgumentList.Add("none");
        startInfo.ArgumentList.Add("--memory");
        startInfo.ArgumentList.Add("256m");
        startInfo.ArgumentList.Add("--cpus");
        startInfo.ArgumentList.Add("1.0");
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("PYTHONIOENCODING=utf-8");
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("PYTHONUTF8=1");
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("LANG=C.UTF-8");
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("LC_ALL=C.UTF-8");
        startInfo.ArgumentList.Add(image);

        foreach (var argument in command)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Kopējais limits = viena testa limits * testu skaits + rezerves laiks.
        var overallTimeout = TimeSpan.FromSeconds(
            ExecutionTimeout.TotalSeconds * testCount) + BatchOverhead;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(overallTimeout);

        using var process = new Process { StartInfo = startInfo };

        var results = new CodeRunResult?[testCount];

        try
        {
            process.Start();

            // Standarta izvade tiek lasīta plūsmā, lai progress parādītos pēc katra testa.
            var stdoutTask = Task.Run(async () =>
            {
                await ParseOrchestratorOutputAsync(process.StandardOutput, results, onResult, timeoutCts.Token);
            }, timeoutCts.Token);

            var stderrTask = process.StandardError.ReadToEndAsync();

            await process.StandardInput.WriteAsync(stdinPayload);
            process.StandardInput.Close();

            await process.WaitForExitAsync(timeoutCts.Token);
            await Task.WhenAll(stdoutTask, stderrTask);

            // Ja kāds tests nav atgriezts, parāda konteinera kļūdas tekstu.
            var orchestratorStderr = await stderrTask;
            for (var i = 0; i < testCount; i++)
            {
                if (results[i] is null)
                {
                    results[i] = new CodeRunResult
                    {
                        ExitCode = process.ExitCode == 0 ? -1 : process.ExitCode,
                        Stderr = string.IsNullOrWhiteSpace(orchestratorStderr)
                            ? "Konteinera izpilde negaidīti pārtraukta."
                            : orchestratorStderr,
                    };
                }
            }

            return results.Select(r => r!).ToList();
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            TryKillProcess(process);
            // Nepabeigtie testi tiek atzīmēti kā pārsnieguši laiku.
            for (var i = 0; i < testCount; i++)
            {
                results[i] ??= new CodeRunResult { TimedOut = true, ExitCode = -1 };
            }
            return results.Select(r => r!).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            for (var i = 0; i < testCount; i++)
            {
                results[i] ??= new CodeRunResult { ExitCode = -1, Stderr = ex.Message };
            }
            return results.Select(r => r!).ToList();
        }
    }

    // Nolasa orķestratoru marķēto standarta izvadi.
    // Kadra formāts:
    //     ===TEST===<i>===
    //     <base64 standarta izvade>
    //     ===STDERR===
    //     <base64 kļūdu izvade>
    //     ===EXIT===<code>===
    //     ===TIMEOUT===<0|1>===
    //     ===END===
    private static async Task ParseOrchestratorOutputAsync(
        StreamReader reader,
        CodeRunResult?[] results,
        Func<int, CodeRunResult, CancellationToken, Task>? onResult,
        CancellationToken ct)
    {
        var currentIndex = -1;
        string? stdoutB64 = null;
        string? stderrB64 = null;
        int exitCode = 0;
        bool timedOut = false;
        var stage = 0; // 0=TEST, 1=izvade b64, 2=STDERR, 3=kļūdu izvade b64, 4=EXIT, 5=TIMEOUT, 6=END

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;

            switch (stage)
            {
                case 0:
                    if (line.StartsWith("===TEST===") && line.EndsWith("==="))
                    {
                        var middle = line.Substring("===TEST===".Length);
                        middle = middle.Substring(0, middle.Length - "===".Length);
                        if (int.TryParse(middle, out var idx))
                        {
                            currentIndex = idx;
                            stage = 1;
                        }
                    }
                    break;
                case 1:
                    stdoutB64 = line;
                    stage = 2;
                    break;
                case 2:
                    if (line == "===STDERR===") stage = 3;
                    break;
                case 3:
                    stderrB64 = line;
                    stage = 4;
                    break;
                case 4:
                    if (line.StartsWith("===EXIT===") && line.EndsWith("==="))
                    {
                        var middle = line.Substring("===EXIT===".Length);
                        middle = middle.Substring(0, middle.Length - "===".Length);
                        int.TryParse(middle, out exitCode);
                        stage = 5;
                    }
                    break;
                case 5:
                    if (line.StartsWith("===TIMEOUT===") && line.EndsWith("==="))
                    {
                        var middle = line.Substring("===TIMEOUT===".Length);
                        middle = middle.Substring(0, middle.Length - "===".Length);
                        timedOut = middle == "1";
                        stage = 6;
                    }
                    break;
                case 6:
                    if (line == "===END===")
                    {
                        var result = new CodeRunResult
                        {
                            Stdout = SafeBase64Decode(stdoutB64),
                            Stderr = SafeBase64Decode(stderrB64),
                            ExitCode = exitCode,
                            TimedOut = timedOut,
                        };

                        if (currentIndex >= 0 && currentIndex < results.Length)
                        {
                            results[currentIndex] = result;
                            if (onResult is not null)
                            {
                                try { await onResult(currentIndex, result, ct); }
                                catch { /* Progresa nosūtīšanas kļūda nedrīkst apturēt koda izpildi. */ }
                            }
                        }

                        // Sagatavo nolasīšanu nākamajam kadram.
                        stdoutB64 = null;
                        stderrB64 = null;
                        exitCode = 0;
                        timedOut = false;
                        currentIndex = -1;
                        stage = 0;
                    }
                    break;
            }
        }
    }

    private static string SafeBase64Decode(string? b64)
    {
        if (string.IsNullOrEmpty(b64)) return string.Empty;
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        }
        catch
        {
            return string.Empty;
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
            // Process var būt beidzies starp laika limita sasniegšanu un tīrīšanu.
        }
    }
}
