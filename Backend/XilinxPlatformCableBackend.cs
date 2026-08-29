using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenXeniumCPLDFlasher.Backend;

public sealed class XilinxPlatformCableBackend
{
    private Process? _process;

    private static readonly Regex IdCodeRegex = new(
        @"idcode\s+0x(?<id>[0-9A-Fa-f]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PercentRegex = new(
        @"(?<pct>\d{1,3}(?:\.\d+)?)%",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string OpenFpgaloaderPath { get; }
    public string FxloadPath { get; }
    public string BootstrapPath { get; }
    public string RuntimeFirmwarePath { get; }

    public XilinxPlatformCableBackend(
        string openFpgaloaderPath,
        string fxloadPath,
        string bootstrapPath,
        string runtimeFirmwarePath)
    {
        OpenFpgaloaderPath = openFpgaloaderPath;
        FxloadPath = fxloadPath;
        BootstrapPath = bootstrapPath;
        RuntimeFirmwarePath = runtimeFirmwarePath;
    }

    public bool IsRunning => _process is { HasExited: false };

    public bool ToolExists() => File.Exists(OpenFpgaloaderPath);

    public bool InitializationFilesExist() =>
        File.Exists(FxloadPath) &&
        File.Exists(BootstrapPath) &&
        File.Exists(RuntimeFirmwarePath);

    public string MissingFilesDescription()
    {
        var missing = new List<string>();
        if (!File.Exists(OpenFpgaloaderPath)) missing.Add("openFPGALoader.exe");
        if (!File.Exists(FxloadPath)) missing.Add("fxload.exe");
        if (!File.Exists(BootstrapPath)) missing.Add("xusb_xlp_bootstrap_extracted.hex");
        if (!File.Exists(RuntimeFirmwarePath)) missing.Add("xusb_xlp.hex");
        return string.Join(", ", missing);
    }

    public async Task<XilinxDetectResult> DetectAsync(
        Action<string>? onOutput,
        CancellationToken cancellationToken)
    {
        // First try the already-initialized cable. This keeps repeated GUI use fast.
        XilinxDetectResult direct = await ProbeAsync(onOutput, cancellationToken);
        if (direct.SupportedDevice)
            return direct;

        onOutput?.Invoke("Xilinx cable not ready yet; checking USB firmware state...");

        bool loaderPresent = await UsbDevicePresentAsync("000F", cancellationToken);
        bool initializedPresent = await UsbDevicePresentAsync("0008", cancellationToken);

        if (!loaderPresent && !initializedPresent)
        {
            onOutput?.Invoke("No Xilinx Platform Cable USB found (03FD:000F / 03FD:0008).");
            return direct;
        }

        if (!InitializationFilesExist())
            throw new FileNotFoundException(
                "Xilinx cable initialization files are missing: " + MissingFilesDescription());

        if (loaderPresent)
        {
            onOutput?.Invoke("Cold cable detected (03FD:000F). Loading bootstrap...");
            await RunFxloadAsync(BootstrapPath, "FX2", "000f", onOutput, cancellationToken);
            await WaitForUsbStateAsync("0008", TimeSpan.FromSeconds(5), cancellationToken);
        }
        else
        {
            onOutput?.Invoke("Initialized USB device detected (03FD:0008).");
        }

        // Reproducibly required for this cable: load xusb_xlp.hex twice.
        onOutput?.Invoke("Loading Xilinx runtime firmware (1/2)...");
        await RunFxloadAsync(RuntimeFirmwarePath, "FX2LP", "0008", onOutput, cancellationToken);

        onOutput?.Invoke("Loading Xilinx runtime firmware (2/2)...");
        await RunFxloadAsync(RuntimeFirmwarePath, "FX2LP", "0008", onOutput, cancellationToken);

        await Task.Delay(250, cancellationToken);
        return await ProbeAsync(onOutput, cancellationToken);
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

        string args =
            "-c xilinxPlatformCableUsb --vid 0x03fd --pid 0x0008 " +
            $"--freq 750000 \"{jedPath}\" -v";

        return RunProcessAsync(OpenFpgaloaderPath, args, null, onOutput, onProgress, cancellationToken);
    }

    public Task<RunResult> EraseAsync(
        Action<string>? onOutput,
        CancellationToken cancellationToken)
    {
        string args =
            "-c xilinxPlatformCableUsb --vid 0x03fd --pid 0x0008 " +
            "--freq 750000 --erase-only -v";

        return RunProcessAsync(OpenFpgaloaderPath, args, null, onOutput, null, cancellationToken);
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
            // Process may have already exited.
        }
    }

    private async Task<XilinxDetectResult> ProbeAsync(
        Action<string>? onOutput,
        CancellationToken cancellationToken)
    {
        if (!ToolExists())
            throw new FileNotFoundException("openFPGALoader.exe was not found.", OpenFpgaloaderPath);

        const string args =
            "-c xilinxPlatformCableUsb --vid 0x03fd --pid 0x0008 " +
            "--freq 750000 --detect -v";

        RunResult run = await RunProcessAsync(
            OpenFpgaloaderPath, args, null, onOutput, null, cancellationToken,
            throwOnStartFailure: false);

        Match match = IdCodeRegex.Match(run.Output);
        string? id = match.Success
            ? "0x" + match.Groups["id"].Value.PadLeft(8, '0').ToUpperInvariant()
            : null;

        bool xc9572xl = run.ExitCode == 0 &&
                        run.Output.Contains("family xc9500xl", StringComparison.OrdinalIgnoreCase) &&
                        run.Output.Contains("model  xc9572xl", StringComparison.OrdinalIgnoreCase);

        return new XilinxDetectResult(xc9572xl, id, run.ExitCode, run.Output);
    }

    private async Task RunFxloadAsync(
        string hexPath,
        string target,
        string expectedPid,
        Action<string>? onOutput,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(FxloadPath))
            throw new FileNotFoundException("fxload.exe was not found.", FxloadPath);

        string args = $"load_ram --ihex-path \"{hexPath}\" -t {target}";

        var psi = new ProcessStartInfo
        {
            FileName = FxloadPath,
            Arguments = args,
            WorkingDirectory = Path.GetDirectoryName(FxloadPath) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process = process;

        var output = new StringBuilder();
        int selectedIndex = -1;
        bool selectionSent = false;
        object gate = new();

        void HandleLine(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            string trimmed = line.TrimEnd();

            lock (gate)
                output.AppendLine(trimmed);

            onOutput?.Invoke(trimmed);

            // Example:
            // 1: Bus 003 Device 002: ID 03fd:000f
            Match deviceMatch = Regex.Match(
                trimmed,
                @"^\s*(?<index>\d+):.*\bID\s+03fd:(?<pid>[0-9a-fA-F]{4})\b",
                RegexOptions.IgnoreCase);

            if (deviceMatch.Success &&
                deviceMatch.Groups["pid"].Value.Equals(expectedPid, StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = int.Parse(deviceMatch.Groups["index"].Value);

                // fxload prints its selection prompt without a terminating newline,
                // so OutputDataReceived may never deliver that prompt. Send the
                // selection immediately when the matching Xilinx device is listed.
                if (!selectionSent)
                {
                    try
                    {
                        process.StandardInput.WriteLine(selectedIndex);
                        process.StandardInput.Flush();
                        selectionSent = true;
                        onOutput?.Invoke(
                            $"Automatically selected Xilinx cable: device {selectedIndex} " +
                            $"(03FD:{expectedPid.ToUpperInvariant()})");
                    }
                    catch
                    {
                        // The process may have exited between output and input.
                    }
                }
            }
        }

        process.OutputDataReceived += (_, e) => HandleLine(e.Data);
        process.ErrorDataReceived += (_, e) => HandleLine(e.Data);

        try
        {
            if (!process.Start())
                throw new InvalidOperationException("Could not start fxload.exe.");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var registration = cancellationToken.Register(Cancel);

            await process.WaitForExitAsync(cancellationToken);
            process.WaitForExit();

            if (!selectionSent)
            {
                throw new InvalidOperationException(
                    $"fxload did not find/select Xilinx cable 03FD:{expectedPid.ToUpperInvariant()}.");
            }

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"fxload failed with exit code {process.ExitCode}.");
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

    private static async Task<bool> UsbDevicePresentAsync(
        string pid,
        CancellationToken cancellationToken)
    {
        string ps =
            "$d = Get-PnpDevice -PresentOnly -ErrorAction SilentlyContinue | " +
            $"Where-Object {{ $_.InstanceId -like 'USB\\VID_03FD&PID_{pid}*' }}; " +
            "if ($d) { exit 0 } else { exit 1 }";

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + ps.Replace("\"", "\\\"") + "\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Could not start PowerShell.");

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0;
    }

    private static async Task WaitForUsbStateAsync(
        string pid,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        DateTime limit = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < limit)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await UsbDevicePresentAsync(pid, cancellationToken))
                return;

            await Task.Delay(150, cancellationToken);
        }

        throw new TimeoutException($"Xilinx cable did not re-enumerate as 03FD:{pid}.");
    }

    private async Task<RunResult> RunProcessAsync(
        string exePath,
        string arguments,
        string? standardInput,
        Action<string>? onOutput,
        Action<int>? onProgress,
        CancellationToken cancellationToken,
        bool throwOnStartFailure = true)
    {
        if (!File.Exists(exePath))
            throw new FileNotFoundException(Path.GetFileName(exePath) + " was not found.", exePath);

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = standardInput != null,
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
                    throw new InvalidOperationException("Could not start " + Path.GetFileName(exePath));
                return new RunResult(-1, "");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (standardInput != null)
            {
                await process.StandardInput.WriteAsync(standardInput);
                await process.StandardInput.FlushAsync();
                process.StandardInput.Close();
            }

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

public sealed record XilinxDetectResult(
    bool SupportedDevice,
    string? IdCode,
    int ExitCode,
    string Output);
