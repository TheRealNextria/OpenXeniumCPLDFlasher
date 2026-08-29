namespace OpenXeniumCPLDFlasher;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private GroupBox grpProgrammer;
    private Label lblProgrammerType;
    private ComboBox cboProgrammer;
    private Label lblProgrammerCaption;
    private Label lblProgrammerStatus;
    private Label lblBackendCaption;
    private Label lblBackendStatus;
    private GroupBox grpTarget;
    private Label lblTargetCaption;
    private Label lblTarget;
    private Label lblIdCaption;
    private Label lblIdCode;
    private Label lblRevisionCaption;
    private Label lblRevision;
    private Button btnDetect;
    private GroupBox grpSvf;
    private TextBox txtSvfPath;
    private Button btnBrowse;
    private Button btnProgram;
    private Button btnErase;
    private Button btnCancel;
    private ProgressBar progressProgram;
    private Label lblProgress;
    private GroupBox grpLog;
    private TextBox txtLog;
    private Button btnClearLog;
    private OpenFileDialog openFileDialogSvf;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        grpProgrammer = new GroupBox();
        lblProgrammerType = new Label();
        cboProgrammer = new ComboBox();
        btnDetect = new Button();
        lblBackendStatus = new Label();
        lblBackendCaption = new Label();
        lblProgrammerStatus = new Label();
        lblProgrammerCaption = new Label();

        grpTarget = new GroupBox();
        lblRevision = new Label();
        lblRevisionCaption = new Label();
        lblIdCode = new Label();
        lblIdCaption = new Label();
        lblTarget = new Label();
        lblTargetCaption = new Label();

        grpSvf = new GroupBox();
        lblProgress = new Label();
        progressProgram = new ProgressBar();
        btnCancel = new Button();
        btnErase = new Button();
        btnProgram = new Button();
        btnBrowse = new Button();
        txtSvfPath = new TextBox();

        grpLog = new GroupBox();
        btnClearLog = new Button();
        txtLog = new TextBox();

        openFileDialogSvf = new OpenFileDialog();

        SuspendLayout();

        // grpProgrammer
        grpProgrammer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpProgrammer.Controls.Add(lblProgrammerType);
        grpProgrammer.Controls.Add(cboProgrammer);
        grpProgrammer.Controls.Add(btnDetect);
        grpProgrammer.Controls.Add(lblBackendStatus);
        grpProgrammer.Controls.Add(lblBackendCaption);
        grpProgrammer.Controls.Add(lblProgrammerStatus);
        grpProgrammer.Controls.Add(lblProgrammerCaption);
        grpProgrammer.Location = new Point(12, 12);
        grpProgrammer.Name = "grpProgrammer";
        grpProgrammer.Size = new Size(760, 130);
        grpProgrammer.TabIndex = 0;
        grpProgrammer.TabStop = false;
        grpProgrammer.Text = "Programmer";

        // lblProgrammerType
        lblProgrammerType.AutoSize = true;
        lblProgrammerType.Location = new Point(18, 28);
        lblProgrammerType.Name = "lblProgrammerType";
        lblProgrammerType.Size = new Size(104, 15);
        lblProgrammerType.Text = "Programmer type:";

        // cboProgrammer
        cboProgrammer.DropDownStyle = ComboBoxStyle.DropDownList;
        cboProgrammer.FormattingEnabled = true;
        cboProgrammer.Items.AddRange(new object[]
        {
            "xFlasher 360",
            "Xilinx Platform Cable USB"
        });
        cboProgrammer.Location = new Point(140, 24);
        cboProgrammer.Name = "cboProgrammer";
        cboProgrammer.Size = new Size(250, 23);
        cboProgrammer.TabIndex = 0;
        cboProgrammer.SelectedIndexChanged += cboProgrammer_SelectedIndexChanged;

        // btnDetect
        btnDetect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnDetect.Location = new Point(622, 22);
        btnDetect.Name = "btnDetect";
        btnDetect.Size = new Size(120, 30);
        btnDetect.TabIndex = 1;
        btnDetect.Text = "Detect";
        btnDetect.UseVisualStyleBackColor = true;
        btnDetect.Click += btnDetect_Click;

        // lblProgrammerCaption
        lblProgrammerCaption.AutoSize = true;
        lblProgrammerCaption.Location = new Point(18, 62);
        lblProgrammerCaption.Name = "lblProgrammerCaption";
        lblProgrammerCaption.Size = new Size(42, 15);
        lblProgrammerCaption.Text = "Status:";

        // lblProgrammerStatus
        lblProgrammerStatus.AutoSize = true;
        lblProgrammerStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblProgrammerStatus.ForeColor = Color.FromArgb(120, 120, 125);
        lblProgrammerStatus.Location = new Point(140, 62);
        lblProgrammerStatus.Name = "lblProgrammerStatus";
        lblProgrammerStatus.Size = new Size(79, 15);
        lblProgrammerStatus.Text = "Not detected";

        // lblBackendCaption
        lblBackendCaption.AutoSize = true;
        lblBackendCaption.Location = new Point(18, 92);
        lblBackendCaption.Name = "lblBackendCaption";
        lblBackendCaption.Size = new Size(54, 15);
        lblBackendCaption.Text = "Backend:";

        // lblBackendStatus
        lblBackendStatus.AutoSize = true;
        lblBackendStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblBackendStatus.Location = new Point(140, 92);
        lblBackendStatus.Name = "lblBackendStatus";
        lblBackendStatus.Size = new Size(12, 15);
        lblBackendStatus.Text = "-";

        // grpTarget
        grpTarget.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpTarget.Controls.Add(lblRevision);
        grpTarget.Controls.Add(lblRevisionCaption);
        grpTarget.Controls.Add(lblIdCode);
        grpTarget.Controls.Add(lblIdCaption);
        grpTarget.Controls.Add(lblTarget);
        grpTarget.Controls.Add(lblTargetCaption);
        grpTarget.Location = new Point(12, 148);
        grpTarget.Name = "grpTarget";
        grpTarget.Size = new Size(760, 100);
        grpTarget.TabIndex = 1;
        grpTarget.TabStop = false;
        grpTarget.Text = "Target";

        lblTargetCaption.AutoSize = true;
        lblTargetCaption.Location = new Point(18, 27);
        lblTargetCaption.Text = "Device:";

        lblTarget.AutoSize = true;
        lblTarget.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblTarget.Location = new Point(140, 27);
        lblTarget.Text = "Xilinx XC9572XL-10VQG64C";

        lblIdCaption.AutoSize = true;
        lblIdCaption.Location = new Point(18, 55);
        lblIdCaption.Text = "IDCODE:";

        lblIdCode.AutoSize = true;
        lblIdCode.Font = new Font("Consolas", 9F, FontStyle.Bold);
        lblIdCode.Location = new Point(140, 55);
        lblIdCode.Text = "-";

        lblRevisionCaption.AutoSize = true;
        lblRevisionCaption.Location = new Point(360, 55);
        lblRevisionCaption.Text = "Revision:";

        lblRevision.AutoSize = true;
        lblRevision.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblRevision.Location = new Point(430, 55);
        lblRevision.Text = "-";

        // grpSvf
        grpSvf.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpSvf.Controls.Add(lblProgress);
        grpSvf.Controls.Add(progressProgram);
        grpSvf.Controls.Add(btnCancel);
        grpSvf.Controls.Add(btnErase);
        grpSvf.Controls.Add(btnProgram);
        grpSvf.Controls.Add(btnBrowse);
        grpSvf.Controls.Add(txtSvfPath);
        grpSvf.Location = new Point(12, 254);
        grpSvf.Name = "grpSvf";
        grpSvf.Size = new Size(760, 130);
        grpSvf.TabIndex = 2;
        grpSvf.TabStop = false;
        grpSvf.Text = "SVF firmware";

        txtSvfPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtSvfPath.Location = new Point(18, 27);
        txtSvfPath.Name = "txtSvfPath";
        txtSvfPath.ReadOnly = true;
        txtSvfPath.Size = new Size(604, 23);

        btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowse.Location = new Point(628, 25);
        btnBrowse.Name = "btnBrowse";
        btnBrowse.Size = new Size(114, 27);
        btnBrowse.Text = "Browse...";
        btnBrowse.UseVisualStyleBackColor = true;
        btnBrowse.Click += btnBrowse_Click;

        btnProgram.Location = new Point(18, 62);
        btnProgram.Name = "btnProgram";
        btnProgram.Size = new Size(130, 30);
        btnProgram.Text = "Program SVF";
        btnProgram.UseVisualStyleBackColor = true;
        btnProgram.Click += btnProgram_Click;

        btnErase.Location = new Point(156, 62);
        btnErase.Name = "btnErase";
        btnErase.Size = new Size(130, 30);
        btnErase.Text = "Erase CPLD";
        btnErase.UseVisualStyleBackColor = true;
        btnErase.Visible = false;
        btnErase.Click += btnErase_Click;

        btnCancel.Location = new Point(294, 62);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(100, 30);
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;

        progressProgram.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        progressProgram.Location = new Point(18, 101);
        progressProgram.Name = "progressProgram";
        progressProgram.Size = new Size(676, 18);

        lblProgress.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblProgress.Location = new Point(700, 101);
        lblProgress.Name = "lblProgress";
        lblProgress.Size = new Size(42, 18);
        lblProgress.Text = "0%";
        lblProgress.TextAlign = ContentAlignment.MiddleRight;

        // grpLog
        grpLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        grpLog.Controls.Add(btnClearLog);
        grpLog.Controls.Add(txtLog);
        grpLog.Location = new Point(12, 390);
        grpLog.Name = "grpLog";
        grpLog.Size = new Size(760, 259);
        grpLog.TabIndex = 3;
        grpLog.TabStop = false;
        grpLog.Text = "Log";

        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.BackColor = Color.White;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.Location = new Point(18, 25);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Size = new Size(724, 192);

        btnClearLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnClearLog.Location = new Point(642, 223);
        btnClearLog.Name = "btnClearLog";
        btnClearLog.Size = new Size(100, 27);
        btnClearLog.Text = "Clear log";
        btnClearLog.UseVisualStyleBackColor = true;
        btnClearLog.Click += btnClearLog_Click;

        // MainForm
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(243, 243, 246);
        ClientSize = new Size(784, 661);
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.FromArgb(45, 45, 48);
        Controls.Add(grpLog);
        Controls.Add(grpSvf);
        Controls.Add(grpTarget);
        Controls.Add(grpProgrammer);
        MinimumSize = new Size(800, 700);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "OpenXenium CPLD Flasher v0.5.2";
        FormClosing += MainForm_FormClosing;
        ResumeLayout(false);
    }
}
