using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenXeniumCPLDFlasher.Backend;

public sealed class DigilentHs2Backend
{
    private Process? _process;

    private static readonly Regex IdCodeRegex = new(
        @"idcode\s+0x(?<id>[0-9A-Fa-f]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PercentRegex = new(
        @"(?<pct>\d{1,3}(?:\.\d+)?)%",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string OpenFpgaloaderPath { get; }

    public DigilentHs2Backend(string openFpgaloaderPath)
    {
        OpenFpgaloaderPath = openFpgaloaderPath;
    }

    public bool IsRunning => _process is { HasExited: false };

    public bool ToolExists() => File.Exists(OpenFpgaloaderPath);

    public async Task<DigilentDetectResult> DetectAsync(
        Action<string>? onOutput,
        CancellationToken cancellationToken)
    {
        if (!ToolExists())
            throw new FileNotFoundException("openFPGALoader.exe was not found.", OpenFpgaloaderPath);

        const string args = "-c digilent_hs2 --freq 740000 --detect -v";

        RunResult run = await RunProcessAsync(
            args, onOutput, null, cancellationToken, throwOnStartFailure: false);

        Match match = IdCodeRegex.Match(run.Output);
        string? id = match.Success
            ? "0x" + match.Groups["id"].Value.PadLeft(8, '0').ToUpperInvariant()
            : null;

        bool xc9572xl = run.ExitCode == 0 &&
                        run.Output.Contains("family xc9500xl", StringComparison.OrdinalIgnoreCase) &&
                        run.Output.Contains("model  xc9572xl", StringComparison.OrdinalIgnoreCase);

        return new DigilentDetectResult(xc9572xl, id, run.ExitCode, run.Output);
    }

    public Task<RunResult> ProgramAsync(
        string jedPath,
        Action<string>? onOutput,
        Action<int>? onProgress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jedPath))
            throw new ArgumentException("No JED file selected.", nameof(jedPath));
        if (!File.Exists(jedPath))
            throw new FileNotFoundException("JED file not found.", jedPath);

        string args = $"-c digilent_hs2 --freq 740000 \"{jedPath}\" -v";
        return RunProcessAsync(args, onOutput, onProgress, cancellationToken);
    }

    public Task<RunResult> EraseAsync(
        Action<string>? onOutput,
        CancellationToken cancellationToken)
    {
        const string args = "-c digilent_hs2 --freq 740000 --erase-only -v";
        return RunProcessAsync(args, onOutput, null, cancellationToken);
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
        }
    }

    private async Task<RunResult> RunProcessAsync(
        string arguments,
        Action<string>? onOutput,
        Action<int>? onProgress,
        CancellationToken cancellationToken,
        bool throwOnStartFailure = true)
    {
        if (!File.Exists(OpenFpgaloaderPath))
            throw new FileNotFoundException("openFPGALoader.exe was not found.", OpenFpgaloaderPath);

        var psi = new ProcessStartInfo
        {
            FileName = OpenFpgaloaderPath,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(OpenFpgaloaderPath) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process = process;

        var allOutput = new StringBuilder();
        object gate = new();

        void HandleText(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            lock (gate)
                allOutput.AppendLine(line);

            Match m = PercentRegex.Match(line);
            if (m.Success &&
                double.TryParse(m.Groups["pct"].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double pct))
            {
                onProgress?.Invoke(Math.Clamp((int)Math.Round(pct), 0, 100));
            }

            onOutput?.Invoke(line.TrimEnd());
        }

        process.OutputDataReceived += (_, e) => HandleText(e.Data);
        process.ErrorDataReceived += (_, e) => HandleText(e.Data);

        try
        {
            if (!process.Start())
            {
                if (throwOnStartFailure)
                    throw new InvalidOperationException("Could not start openFPGALoader.exe");
                return new RunResult(-1, "");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var registration = cancellationToken.Register(Cancel);

            await process.WaitForExitAsync(cancellationToken);
            process.WaitForExit();

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
}

public sealed record DigilentDetectResult(
    bool SupportedDevice,
    string? IdCode,
    int ExitCode,
    string Output);
