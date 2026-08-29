using OpenXeniumCPLDFlasher.Backend;

namespace OpenXeniumCPLDFlasher;

public partial class MainForm : Form
{
    private readonly XsvfToolBackend _xflasherBackend;
    private readonly XilinxPlatformCableBackend _xilinxBackend;
    private CancellationTokenSource? _cts;
    private bool _cpldDetected;

    private bool XilinxSelected => cboProgrammer.SelectedIndex == 1;

    public MainForm()
    {
        InitializeComponent();
		try
    {
		this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
    }
		catch
    {
        // Ignore if icon is unavailable.
    }

		this.ShowIcon = true;
		this.ShowInTaskbar = true;


        string tools = Path.Combine(AppContext.BaseDirectory, "Tools");

        _xflasherBackend = new XsvfToolBackend(
            Path.Combine(tools, "xsvftool.exe"));

        _xilinxBackend = new XilinxPlatformCableBackend(
            Path.Combine(tools, "openFPGALoader.exe"),
            Path.Combine(tools, "fxload.exe"),
            Path.Combine(tools, "xusb_xlp_bootstrap_extracted.hex"),
            Path.Combine(tools, "xusb_xlp.hex"));

        cboProgrammer.SelectedIndex = 0;

        AppendLog("OpenXenium CPLD Flasher v0.5.2 started.");
        AppendLog("Target: Xilinx XC9572XL-10VQG64C");
        UpdateBackendUi();
        UpdateButtons();
    }

    private async void btnDetect_Click(object? sender, EventArgs e)
    {
        if (!SelectedBackendReady())
        {
            ShowMissingBackend();
            return;
        }

        SetBusy(true);
        _cts = new CancellationTokenSource();
        _cpldDetected = false;
        lblProgrammerStatus.Text = "Detecting...";
        lblProgrammerStatus.ForeColor = Color.FromArgb(0, 120, 215);
        lblIdCode.Text = "-";
        lblRevision.Text = "-";

        try
        {
            if (XilinxSelected)
            {
                AppendLog("Detecting Xilinx Platform Cable USB / CPLD...");
                XilinxDetectResult result =
                    await _xilinxBackend.DetectAsync(AppendLogThreadSafe, _cts.Token);

                if (result.SupportedDevice)
                {
                    _cpldDetected = true;
                    lblProgrammerStatus.Text = "Xilinx Platform Cable USB: Connected";
                    lblProgrammerStatus.ForeColor = Color.FromArgb(16, 124, 16);
                    lblIdCode.Text = result.IdCode ?? "-";
                    lblRevision.Text = "-";
                    AppendLog($"Detected: Xilinx XC9572XL, IDCODE {result.IdCode}");
                    AppendLog("JTAG frequency: 750 kHz");
                }
                else
                {
                    lblProgrammerStatus.Text = "Not detected";
                    lblProgrammerStatus.ForeColor = Color.FromArgb(196, 43, 28);
                    AppendLog("No supported XC9572XL detected with Xilinx Platform Cable USB.");
                }
            }
            else
            {
                AppendLog("Detecting xFlasher 360 / CPLD...");
                DetectResult result =
                    await _xflasherBackend.DetectAsync(AppendLogThreadSafe, _cts.Token);

                if (result.SupportedDevice)
                {
                    _cpldDetected = true;
                    lblProgrammerStatus.Text = "xFlasher 360: Connected";
                    lblProgrammerStatus.ForeColor = Color.FromArgb(16, 124, 16);
                    lblIdCode.Text = result.IdCode ?? "-";
                    lblRevision.Text = ParseRevision(result.Revision);
                    AppendLog($"Detected: Xilinx XC9572XL, revision {ParseRevision(result.Revision)}, IDCODE {result.IdCode}");
                }
                else if (result.IdCode != null)
                {
                    lblProgrammerStatus.Text = "Unsupported JTAG device";
                    lblProgrammerStatus.ForeColor = Color.FromArgb(196, 43, 28);
                    lblIdCode.Text = result.IdCode;
                    lblRevision.Text = ParseRevision(result.Revision);
                    AppendLog($"Unsupported device: IDCODE {result.IdCode}, part {result.Part}, manufacturer {result.Manufacturer}");
                }
                else
                {
                    lblProgrammerStatus.Text = "Not detected";
                    lblProgrammerStatus.ForeColor = Color.FromArgb(196, 43, 28);
                    AppendLog("No supported XC9572XL detected on xFlasher JTAG channel 0.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            AppendLog("Detection cancelled.");
            lblProgrammerStatus.Text = "Cancelled";
            lblProgrammerStatus.ForeColor = Color.FromArgb(120, 120, 125);
        }
        catch (Exception ex)
        {
            AppendLog("ERROR: " + ex.Message);
            lblProgrammerStatus.Text = "Error";
            lblProgrammerStatus.ForeColor = Color.FromArgb(196, 43, 28);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            SetBusy(false);
        }
    }

    private void cboProgrammer_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _cpldDetected = false;
        lblProgrammerStatus.Text = "Not detected";
        lblProgrammerStatus.ForeColor = Color.FromArgb(120, 120, 125);
        lblIdCode.Text = "-";
        lblRevision.Text = "-";
        txtSvfPath.Clear();
        SetProgress(0);
        UpdateBackendUi();
        UpdateButtons();
    }

    private void UpdateBackendUi()
    {
        if (XilinxSelected)
        {
            bool ready = _xilinxBackend.ToolExists();
            lblBackendStatus.Text = ready ? "openFPGALoader.exe ready" : "openFPGALoader.exe missing";
            lblBackendStatus.ForeColor = ready ? Color.FromArgb(16, 124, 16) : Color.FromArgb(196, 43, 28);
            grpSvf.Text = "JED firmware";
            btnProgram.Text = "Program JED";
            btnErase.Visible = true;
            openFileDialogSvf.Filter = "JED files (*.jed)|*.jed|All files (*.*)|*.*";
            openFileDialogSvf.Title = "Select OpenXenium JED file";
            AppendLog("Programmer selected: Xilinx Platform Cable USB (750 kHz).");
        }
        else
        {
            bool ready = _xflasherBackend.ToolExists();
            lblBackendStatus.Text = ready ? "xsvftool.exe ready" : "xsvftool.exe missing";
            lblBackendStatus.ForeColor = ready ? Color.FromArgb(16, 124, 16) : Color.FromArgb(196, 43, 28);
            grpSvf.Text = "SVF firmware";
            btnProgram.Text = "Program SVF";
            btnErase.Visible = false;
            openFileDialogSvf.Filter = "SVF files (*.svf)|*.svf|All files (*.*)|*.*";
            openFileDialogSvf.Title = "Select OpenXenium SVF file";
            AppendLog("Programmer selected: xFlasher 360.");
        }
    }

    private void btnBrowse_Click(object? sender, EventArgs e)
    {
        if (openFileDialogSvf.ShowDialog(this) != DialogResult.OK)
            return;

        txtSvfPath.Text = openFileDialogSvf.FileName;
        AppendLog($"{(XilinxSelected ? "JED" : "SVF")} selected: {Path.GetFileName(openFileDialogSvf.FileName)}");
        UpdateButtons();
    }

    private async void btnProgram_Click(object? sender, EventArgs e)
    {
        string firmware = txtSvfPath.Text;

        if (!File.Exists(firmware))
        {
            MessageBox.Show(this,
                $"Select a valid {(XilinxSelected ? "JED" : "SVF")} file first.",
                "Firmware file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!_cpldDetected)
        {
            MessageBox.Show(this, "Detect the CPLD before programming.", "CPLD not detected",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        SetProgress(0);
        _cts = new CancellationTokenSource();
        AppendLog("Programming: " + Path.GetFileName(firmware));

        try
        {
            RunResult result;

            if (XilinxSelected)
            {
                AppendLog("Backend: Xilinx Platform Cable USB @ 750 kHz");
                result = await _xilinxBackend.ProgramAsync(
                    firmware, AppendLogThreadSafe, SetProgressThreadSafe, _cts.Token);
            }
            else
            {
                AppendLog("Backend: xFlasher 360 / patched xsvftool");
                result = await _xflasherBackend.ProgramAsync(
                    firmware, AppendLogThreadSafe, SetProgressThreadSafe, _cts.Token);
            }

            if (result.ExitCode == 0)
            {
                SetProgress(100);
                AppendLog("Programming completed successfully.");
                MessageBox.Show(this, "CPLD programming completed successfully.",
                    "OpenXenium CPLD Flasher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                AppendLog($"Programming failed. Exit code: {result.ExitCode}");
                MessageBox.Show(this, "Programming failed. Check the log for details.",
                    "Programming error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (OperationCanceledException)
        {
            AppendLog("Programming cancelled.");
        }
        catch (Exception ex)
        {
            AppendLog("ERROR: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Programming error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            SetBusy(false);
        }
    }

    private async void btnErase_Click(object? sender, EventArgs e)
    {
        if (!XilinxSelected || !_cpldDetected)
            return;

        if (MessageBox.Show(this,
                "Erase the XC9572XL CPLD?\n\nThe OpenXenium LED should turn white after a successful erase.",
                "Erase CPLD", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        SetBusy(true);
        SetProgress(0);
        _cts = new CancellationTokenSource();
        AppendLog("Erasing XC9572XL...");

        try
        {
            RunResult result = await _xilinxBackend.EraseAsync(
                AppendLogThreadSafe, _cts.Token);

            if (result.ExitCode == 0)
            {
                AppendLog("Erase completed successfully.");
                MessageBox.Show(this, "CPLD erase completed successfully.",
                    "OpenXenium CPLD Flasher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                AppendLog($"Erase failed. Exit code: {result.ExitCode}");
                MessageBox.Show(this, "Erase failed. Check the log for details.",
                    "Erase error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (OperationCanceledException)
        {
            AppendLog("Erase cancelled.");
        }
        catch (Exception ex)
        {
            AppendLog("ERROR: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Erase error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            SetBusy(false);
        }
    }

    private void btnCancel_Click(object? sender, EventArgs e)
    {
        btnCancel.Enabled = false;
        AppendLog("Cancelling...");
        _cts?.Cancel();
        _xflasherBackend.Cancel();
        _xilinxBackend.Cancel();
    }

    private void btnClearLog_Click(object? sender, EventArgs e) => txtLog.Clear();

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_xflasherBackend.IsRunning || _xilinxBackend.IsRunning)
        {
            _cts?.Cancel();
            _xflasherBackend.Cancel();
            _xilinxBackend.Cancel();
        }
    }

    private bool SelectedBackendReady() =>
        XilinxSelected ? _xilinxBackend.ToolExists() : _xflasherBackend.ToolExists();

    private void ShowMissingBackend()
    {
        if (XilinxSelected)
        {
            MessageBox.Show(this,
                "Tools\\openFPGALoader.exe was not found.",
                "Backend missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            MessageBox.Show(this,
                "Tools\\xsvftool.exe was not found.",
                "Backend missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetBusy(bool busy)
    {
        cboProgrammer.Enabled = !busy;
        btnDetect.Enabled = !busy;
        btnBrowse.Enabled = !busy;
        btnCancel.Enabled = busy;
        UpdateButtons(busy);
    }

    private void UpdateButtons(bool? busyOverride = null)
    {
        bool busy = busyOverride ??
            (_xflasherBackend.IsRunning || _xilinxBackend.IsRunning);

        btnProgram.Enabled =
            !busy &&
            _cpldDetected &&
            File.Exists(txtSvfPath.Text) &&
            SelectedBackendReady();

        btnErase.Enabled =
            !busy &&
            XilinxSelected &&
            _cpldDetected &&
            _xilinxBackend.ToolExists();

        if (!busy)
            btnCancel.Enabled = false;
    }

    private void SetProgress(int percent)
    {
        int p = Math.Clamp(percent, 0, 100);
        progressProgram.Value = p;
        lblProgress.Text = $"{p}%";
    }

    private void SetProgressThreadSafe(int percent)
    {
        if (InvokeRequired)
            BeginInvoke(new Action<int>(SetProgress), percent);
        else
            SetProgress(percent);
    }

    private void AppendLogThreadSafe(string text)
    {
        if (InvokeRequired)
            BeginInvoke(new Action<string>(AppendLog), text);
        else
            AppendLog(text);
    }

    private void AppendLog(string text)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        txtLog.SelectionStart = txtLog.TextLength;
        txtLog.ScrollToCaret();
    }

    private static string ParseRevision(string? revision)
    {
        if (string.IsNullOrWhiteSpace(revision)) return "-";
        string value = revision.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? revision[2..]
            : revision;

        return int.TryParse(value,
            System.Globalization.NumberStyles.HexNumber,
            null,
            out int rev)
            ? rev.ToString()
            : revision;
    }
}
