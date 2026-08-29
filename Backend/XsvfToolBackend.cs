using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenXeniumCPLDFlasher.Backend;

public sealed class XsvfToolBackend
{
    private Process? _process;

    private static readonly Regex IdCodeRegex = new(
        @"idcode=0x(?<id>[0-9A-Fa-f]{8}),\s*revision=0x(?<rev>[0-9A-Fa-f]+),\s*part=0x(?<part>[0-9A-Fa-f]+),\s*manufactor=0x(?<mfg>[0-9A-Fa-f]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ProgressRegex = new(
        @"Progress\s*:\s*\[\s*(?<pct>\d{1,3})%\s*\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string ToolPath { get; }

    public XsvfToolBackend(string toolPath)
    {
        ToolPath = toolPath;
    }

    public bool IsRunning => _process is { HasExited: false };

    public bool ToolExists() => File.Exists(ToolPath);

    public async Task<DetectResult> DetectAsync(Action<string>? onOutput, CancellationToken cancellationToken)
    {
        var result = await RunAsync("-A -j 0 -c", onOutput, null, cancellationToken);

        if (result.ExitCode != 0)
            return new DetectResult(false, null, null, null, null, result.ExitCode, result.Output);

        Match match = IdCodeRegex.Match(result.Output);
        if (!match.Success)
            return new DetectResult(false, null, null, null, null, result.ExitCode, result.Output);

        string id = "0x" + match.Groups["id"].Value.ToUpperInvariant();
        string rev = "0x" + match.Groups["rev"].Value.ToUpperInvariant();
        string part = "0x" + match.Groups["part"].Value.ToUpperInvariant();
        string mfg = "0x" + match.Groups["mfg"].Value.ToUpperInvariant();

        // XC9572XL family part code as reported by xsvftool is 0x9604.
        bool supported = string.Equals(part, "0x9604", StringComparison.OrdinalIgnoreCase)
                         && string.Equals(mfg, "0x049", StringComparison.OrdinalIgnoreCase);

        return new DetectResult(supported, id, rev, part, mfg, result.ExitCode, result.Output);
    }

    public Task<RunResult> ProgramAsync(
        string svfPath,
        Action<string>? onOutput,
        Action<int>? onProgress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(svfPath))
            throw new ArgumentException("No SVF file selected.", nameof(svfPath));
        if (!File.Exists(svfPath))
            throw new FileNotFoundException("SVF file not found.", svfPath);

        string args = $"-A -j 0 -p -s \"{svfPath}\"";
        return RunAsync(args, onOutput, onProgress, cancellationToken);
    }

    public void Cancel()
    {
        try
        {
            if (_process is { HasExited: false })
                _process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Process may have exited between the checks.
        }
    }

    private async Task<RunResult> RunAsync(
        string arguments,
        Action<string>? onOutput,
        Action<int>? onProgress,
        CancellationToken cancellationToken)
    {
        if (!ToolExists())
            throw new FileNotFoundException("xsvftool.exe was not found.", ToolPath);

        var psi = new ProcessStartInfo
        {
            FileName = ToolPath,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(ToolPath) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        var allOutput = new StringBuilder();
        var lineBuffer = new StringBuilder();
        object gate = new();

        void ProcessChunk(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            lock (gate)
            {
                allOutput.Append(text);

                // xsvftool progress uses carriage returns rather than normal lines.
                foreach (char ch in text)
                {
                    if (ch == '\r' || ch == '\n')
                    {
                        if (lineBuffer.Length > 0)
                        {
                            string line = lineBuffer.ToString();
                            lineBuffer.Clear();
                            HandleLine(line, onOutput, onProgress);
                        }
                    }
                    else
                    {
                        lineBuffer.Append(ch);
                    }
                }
            }
        }

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process = process;

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                ProcessChunk(e.Data + Environment.NewLine);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                ProcessChunk(e.Data + Environment.NewLine);
        };

        if (!process.Start())
            throw new InvalidOperationException("Could not start xsvftool.exe.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var registration = cancellationToken.Register(Cancel);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
            process.WaitForExit(); // ensure async redirected streams are drained

            lock (gate)
            {
                if (lineBuffer.Length > 0)
                {
                    HandleLine(lineBuffer.ToString(), onOutput, onProgress);
                    lineBuffer.Clear();
                }
            }

            return new RunResult(process.ExitCode, allOutput.ToString());
        }
        catch (OperationCanceledException)
        {
            Cancel();
            throw;
        }
        finally
        {
            _process = null;
        }
    }

    private static void HandleLine(string rawLine, Action<string>? onOutput, Action<int>? onProgress)
    {
        string line = rawLine.Trim();
        if (line.Length == 0)
            return;

        Match progress = ProgressRegex.Match(line);
        if (progress.Success && int.TryParse(progress.Groups["pct"].Value, out int pct))
        {
            onProgress?.Invoke(Math.Clamp(pct, 0, 100));
            return;
        }

        onOutput?.Invoke(line);
    }
}

public sealed record DetectResult(
    bool SupportedDevice,
    string? IdCode,
    string? Revision,
    string? Part,
    string? Manufacturer,
    int ExitCode,
    string Output);

public sealed record RunResult(int ExitCode, string Output);
