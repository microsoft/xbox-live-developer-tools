namespace SessionHistoryViewer
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.Intro = new System.Windows.Forms.Label();
            this.lblScid = new System.Windows.Forms.Label();
            this.tbScid = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbTemplateName = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnPriorQuery = new System.Windows.Forms.Button();
            this.cmbQueryType = new System.Windows.Forms.ComboBox();
            this.tbSandbox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.dateTimePicker2 = new System.Windows.Forms.DateTimePicker();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.btnQuery = new System.Windows.Forms.Button();
            this.tbQueryKey = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.menuStrip2 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadSessionHistoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sendFeedbackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.displayDateTimesAsUTCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.lblChangesHelp = new System.Windows.Forms.Label();
            this.downloadPanel = new System.Windows.Forms.Panel();
            this.lblExplanation = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.listView2 = new System.Windows.Forms.ListView();
            this.lblChangeCount = new System.Windows.Forms.Label();
            this.checkboxLockVScroll = new System.Windows.Forms.CheckBox();
            this.lblChangeRight = new System.Windows.Forms.Label();
            this.lblChangeLeft = new System.Windows.Forms.Label();
            this.rtbSnapshotRight = new System.Windows.Forms.RichTextBox();
            this.rtbSnapshotLeft = new System.Windows.Forms.RichTextBox();
            this.SessionDocumentContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.saveSessionHistoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listView1 = new System.Windows.Forms.ListView();
            this.lblDocCount = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbAccountSource = new System.Windows.Forms.ComboBox();
            this.labelUserName = new System.Windows.Forms.Label();
            this.btnSingInout = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.menuStrip2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.downloadPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SessionDocumentContextMenuStrip.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // Intro
            // 
            this.Intro.Location = new System.Drawing.Point(286, 81);
            this.Intro.Name = "Intro";
            this.Intro.Size = new System.Drawing.Size(122, 16);
            this.Intro.TabIndex = 25;
            this.Intro.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblScid
            // 
            this.lblScid.AutoSize = true;
            this.lblScid.Location = new System.Drawing.Point(69, 46);
            this.lblScid.Name = "lblScid";
            this.lblScid.Size = new System.Drawing.Size(35, 13);
            this.lblScid.TabIndex = 27;
            this.lblScid.Text = "SCID:";
            // 
            // tbScid
            // 
            this.tbScid.Location = new System.Drawing.Point(110, 40);
            this.tbScid.Name = "tbScid";
            this.tbScid.Size = new System.Drawing.Size(282, 20);
            this.tbScid.TabIndex = 2;
            this.tbScid.Validated += new System.EventHandler(this.TbScid_Validated);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(19, 68);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 13);
            this.label4.TabIndex = 30;
            this.label4.Text = "Template Name:";
            // 
            // tbTemplateName
            // 
            this.tbTemplateName.Location = new System.Drawing.Point(110, 63);
            this.tbTemplateName.Name = "tbTemplateName";
            this.tbTemplateName.Size = new System.Drawing.Size(282, 20);
            this.tbTemplateName.TabIndex = 3;
            this.tbTemplateName.Validated += new System.EventHandler(this.TbTemplateName_Validated);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnPriorQuery);
            this.groupBox1.Controls.Add(this.cmbQueryType);
            this.groupBox1.Controls.Add(this.tbSandbox);
            this.groupBox1.Controls.Add(this.tbTemplateName);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.tbScid);
            this.groupBox1.Controls.Add(this.lblScid);
            this.groupBox1.Controls.Add(this.dateTimePicker2);
            this.groupBox1.Controls.Add(this.dateTimePicker1);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.btnQuery);
            this.groupBox1.Controls.Add(this.tbQueryKey);
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Location = new System.Drawing.Point(282, 27);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(861, 118);
            this.groupBox1.TabIndex = 46;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Query Builder";
            // 
            // btnPriorQuery
            // 
            this.btnPriorQuery.Location = new System.Drawing.Point(787, 14);
            this.btnPriorQuery.Name = "btnPriorQuery";
            this.btnPriorQuery.Size = new System.Drawing.Size(67, 45);
            this.btnPriorQuery.TabIndex = 9;
            this.btnPriorQuery.Text = "<---   Go Back";
            this.btnPriorQuery.UseVisualStyleBackColor = true;
            this.btnPriorQuery.Visible = false;
            this.btnPriorQuery.Click += new System.EventHandler(this.BtnPriorQuery_Click);
            // 
            // cmbQueryType
            // 
            this.cmbQueryType.FormattingEnabled = true;
            this.cmbQueryType.Items.AddRange(new object[] {
            "Session Name",
            "Gamer Tag",
            "Gamer ID (xuid)",
            "CorrelationId"});
            this.cmbQueryType.Location = new System.Drawing.Point(396, 86);
            this.cmbQueryType.Name = "cmbQueryType";
            this.cmbQueryType.Size = new System.Drawing.Size(145, 21);
            this.cmbQueryType.TabIndex = 5;
            this.cmbQueryType.SelectedIndexChanged += new System.EventHandler(this.CmbQueryType_SelectedIndexChanged);
            // 
            // tbSandbox
            // 
            this.tbSandbox.Location = new System.Drawing.Point(110, 17);
            this.tbSandbox.Name = "tbSandbox";
            this.tbSandbox.Size = new System.Drawing.Size(105, 20);
            this.tbSandbox.TabIndex = 1;
            this.tbSandbox.Validated += new System.EventHandler(this.TbSandbox_Validated);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(52, 20);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(52, 13);
            this.label3.TabIndex = 62;
            this.label3.Text = "Sandbox:";
            // 
            // dateTimePicker2
            // 
            this.dateTimePicker2.Location = new System.Drawing.Point(431, 40);
            this.dateTimePicker2.Name = "dateTimePicker2";
            this.dateTimePicker2.Size = new System.Drawing.Size(209, 20);
            this.dateTimePicker2.TabIndex = 7;
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.Location = new System.Drawing.Point(431, 13);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(209, 20);
            this.dateTimePicker1.TabIndex = 6;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(409, 42);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(23, 13);
            this.label14.TabIndex = 52;
            this.label14.Text = "To:";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(397, 17);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(33, 13);
            this.label13.TabIndex = 50;
            this.label13.Text = "From:";
            // 
            // btnQuery
            // 
            this.btnQuery.Location = new System.Drawing.Point(663, 12);
            this.btnQuery.Name = "btnQuery";
            this.btnQuery.Size = new System.Drawing.Size(118, 69);
            this.btnQuery.TabIndex = 8;
            this.btnQuery.Text = "Query";
            this.btnQuery.UseVisualStyleBackColor = true;
            this.btnQuery.Click += new System.EventHandler(this.BtnQuery_Click);
            // 
            // tbQueryKey
            // 
            this.tbQueryKey.Location = new System.Drawing.Point(110, 87);
            this.tbQueryKey.Name = "tbQueryKey";
            this.tbQueryKey.Size = new System.Drawing.Size(282, 20);
            this.tbQueryKey.TabIndex = 4;
            this.tbQueryKey.Validated += new System.EventHandler(this.TbQueryKey_Validated);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(45, 92);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(59, 13);
            this.label11.TabIndex = 0;
            this.label11.Text = "Query Key:";
            // 
            // menuStrip2
            // 
            this.menuStrip2.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.menuStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.helpToolStripMenuItem,
            this.settingsToolStripMenuItem});
            this.menuStrip2.Location = new System.Drawing.Point(0, 0);
            this.menuStrip2.Name = "menuStrip2";
            this.menuStrip2.Size = new System.Drawing.Size(1237, 24);
            this.menuStrip2.TabIndex = 51;
            this.menuStrip2.Text = "menuStrip2";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadSessionHistoryToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // loadSessionHistoryToolStripMenuItem
            // 
            this.loadSessionHistoryToolStripMenuItem.Name = "loadSessionHistoryToolStripMenuItem";
            this.loadSessionHistoryToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.loadSessionHistoryToolStripMenuItem.Text = "&Load Session History...";
            this.loadSessionHistoryToolStripMenuItem.Click += new System.EventHandler(this.LoadSessionHistoryToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem,
            this.sendFeedbackToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.aboutToolStripMenuItem.Text = "A&bout...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.AboutToolStripMenuItem_Click);
            // 
            // sendFeedbackToolStripMenuItem
            // 
            this.sendFeedbackToolStripMenuItem.Name = "sendFeedbackToolStripMenuItem";
            this.sendFeedbackToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.sendFeedbackToolStripMenuItem.Text = "Send &Feedback...";
            this.sendFeedbackToolStripMenuItem.Click += new System.EventHandler(this.SendFeedbackToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.displayDateTimesAsUTCToolStripMenuItem});
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "&Settings";
            // 
            // displayDateTimesAsUTCToolStripMenuItem
            // 
            this.displayDateTimesAsUTCToolStripMenuItem.CheckOnClick = true;
            this.displayDateTimesAsUTCToolStripMenuItem.Name = "displayDateTimesAsUTCToolStripMenuItem";
            this.displayDateTimesAsUTCToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.displayDateTimesAsUTCToolStripMenuItem.Text = "Display DateTimes as &UTC";
            this.displayDateTimesAsUTCToolStripMenuItem.Click += new System.EventHandler(this.DisplayDateTimesAsUTCToolStripMenuItem_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Location = new System.Drawing.Point(12, 298);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.lblChangesHelp);
            this.splitContainer1.Panel1.Controls.Add(this.downloadPanel);
            this.splitContainer1.Panel1.Controls.Add(this.listView2);
            this.splitContainer1.Panel1.Controls.Add(this.lblChangeCount);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.checkboxLockVScroll);
            this.splitContainer1.Panel2.Controls.Add(this.lblChangeRight);
            this.splitContainer1.Panel2.Controls.Add(this.lblChangeLeft);
            this.splitContainer1.Panel2.Controls.Add(this.rtbSnapshotRight);
            this.splitContainer1.Panel2.Controls.Add(this.rtbSnapshotLeft);
            this.splitContainer1.Size = new System.Drawing.Size(1150, 603);
            this.splitContainer1.SplitterDistance = 170;
            this.splitContainer1.TabIndex = 75;
            this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.SplitContainer1_SplitterMoved);
            // 
            // lblChangesHelp
            // 
            this.lblChangesHelp.AutoSize = true;
            this.lblChangesHelp.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblChangesHelp.Location = new System.Drawing.Point(572, 16);
            this.lblChangesHelp.Name = "lblChangesHelp";
            this.lblChangesHelp.Size = new System.Drawing.Size(0, 13);
            this.lblChangesHelp.TabIndex = 74;
            // 
            // downloadPanel
            // 
            this.downloadPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.downloadPanel.Controls.Add(this.lblExplanation);
            this.downloadPanel.Controls.Add(this.btnCancel);
            this.downloadPanel.Controls.Add(this.pictureBox1);
            this.downloadPanel.Location = new System.Drawing.Point(374, 31);
            this.downloadPanel.Name = "downloadPanel";
            this.downloadPanel.Size = new System.Drawing.Size(431, 221);
            this.downloadPanel.TabIndex = 76;
            this.downloadPanel.Visible = false;
            this.downloadPanel.VisibleChanged += new System.EventHandler(this.DownloadPanel_VisibleChanged);
            // 
            // lblExplanation
            // 
            this.lblExplanation.AutoSize = true;
            this.lblExplanation.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblExplanation.Location = new System.Drawing.Point(41, 35);
            this.lblExplanation.Name = "lblExplanation";
            this.lblExplanation.Size = new System.Drawing.Size(0, 19);
            this.lblExplanation.TabIndex = 6;
            this.lblExplanation.UseWaitCursor = true;
            this.lblExplanation.TextChanged += new System.EventHandler(this.LblExplanation_TextChanged);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Location = new System.Drawing.Point(263, 150);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(149, 43);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.UseWaitCursor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(45, 129);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(64, 64);
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.UseWaitCursor = true;
            // 
            // listView2
            // 
            this.listView2.FullRowSelect = true;
            this.listView2.GridLines = true;
            this.listView2.Location = new System.Drawing.Point(4, 31);
            this.listView2.Name = "listView2";
            this.listView2.Size = new System.Drawing.Size(1125, 134);
            this.listView2.TabIndex = 72;
            this.listView2.UseCompatibleStateImageBehavior = false;
            this.listView2.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.ListView2_ColumnClick);
            this.listView2.SelectedIndexChanged += new System.EventHandler(this.ListView2_SelectedIndexChanged);
            // 
            // lblChangeCount
            // 
            this.lblChangeCount.AutoSize = true;
            this.lblChangeCount.Location = new System.Drawing.Point(7, 15);
            this.lblChangeCount.Name = "lblChangeCount";
            this.lblChangeCount.Size = new System.Drawing.Size(0, 13);
            this.lblChangeCount.TabIndex = 73;
            this.lblChangeCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // checkboxLockVScroll
            // 
            this.checkboxLockVScroll.AutoSize = true;
            this.checkboxLockVScroll.Location = new System.Drawing.Point(431, 316);
            this.checkboxLockVScroll.Name = "checkboxLockVScroll";
            this.checkboxLockVScroll.Size = new System.Drawing.Size(322, 17);
            this.checkboxLockVScroll.TabIndex = 79;
            this.checkboxLockVScroll.Text = "Lock Scrolling (you can right-click on either snapshot to toggle)";
            this.checkboxLockVScroll.UseVisualStyleBackColor = true;
            // 
            // lblChangeRight
            // 
            this.lblChangeRight.AutoSize = true;
            this.lblChangeRight.Location = new System.Drawing.Point(578, 14);
            this.lblChangeRight.Name = "lblChangeRight";
            this.lblChangeRight.Size = new System.Drawing.Size(0, 13);
            this.lblChangeRight.TabIndex = 78;
            this.lblChangeRight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblChangeLeft
            // 
            this.lblChangeLeft.AutoSize = true;
            this.lblChangeLeft.Location = new System.Drawing.Point(8, 14);
            this.lblChangeLeft.Name = "lblChangeLeft";
            this.lblChangeLeft.Size = new System.Drawing.Size(0, 13);
            this.lblChangeLeft.TabIndex = 77;
            this.lblChangeLeft.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // rtbSnapshotRight
            // 
            this.rtbSnapshotRight.Location = new System.Drawing.Point(584, 31);
            this.rtbSnapshotRight.Name = "rtbSnapshotRight";
            this.rtbSnapshotRight.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtbSnapshotRight.Size = new System.Drawing.Size(545, 279);
            this.rtbSnapshotRight.TabIndex = 76;
            this.rtbSnapshotRight.Text = "";
            this.rtbSnapshotRight.VScroll += new System.EventHandler(this.RtbSnapshotRight_VScroll);
            this.rtbSnapshotRight.MouseDown += new System.Windows.Forms.MouseEventHandler(this.RtbSnapshotRight_MouseDown);
            // 
            // rtbSnapshotLeft
            // 
            this.rtbSnapshotLeft.Location = new System.Drawing.Point(4, 31);
            this.rtbSnapshotLeft.Name = "rtbSnapshotLeft";
            this.rtbSnapshotLeft.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtbSnapshotLeft.Size = new System.Drawing.Size(574, 279);
            this.rtbSnapshotLeft.TabIndex = 75;
            this.rtbSnapshotLeft.Text = "";
            this.rtbSnapshotLeft.VScroll += new System.EventHandler(this.RtbSnapshotLeft_VScroll);
            this.rtbSnapshotLeft.MouseDown += new System.Windows.Forms.MouseEventHandler(this.RtbSnapshotLeft_MouseDown);
            // 
            // SessionDocumentContextMenuStrip
            // 
            this.SessionDocumentContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveSessionHistoryToolStripMenuItem});
            this.SessionDocumentContextMenuStrip.Name = "SessionDocumentContextMenuStrip";
            this.SessionDocumentContextMenuStrip.Size = new System.Drawing.Size(191, 26);
            // 
            // saveSessionHistoryToolStripMenuItem
            // 
            this.saveSessionHistoryToolStripMenuItem.Name = "saveSessionHistoryToolStripMenuItem";
            this.saveSessionHistoryToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.saveSessionHistoryToolStripMenuItem.Text = "&Save Session History...";
            this.saveSessionHistoryToolStripMenuItem.Click += new System.EventHandler(this.SaveSessionHistoryToolStripMenuItem_Click);
            // 
            // listView1
            // 
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(18, 171);
            this.listView1.MultiSelect = false;
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(1125, 121);
            this.listView1.TabIndex = 72;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.ListView1_ColumnClick);
            this.listView1.SelectedIndexChanged += new System.EventHandler(this.ListView1_SelectedIndexChanged);
            this.listView1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ListView1_MouseDown);
            // 
            // lblDocCount
            // 
            this.lblDocCount.AutoSize = true;
            this.lblDocCount.Location = new System.Drawing.Point(21, 155);
            this.lblDocCount.Name = "lblDocCount";
            this.lblDocCount.Size = new System.Drawing.Size(0, 13);
            this.lblDocCount.TabIndex = 73;
            this.lblDocCount.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.cmbAccountSource);
            this.groupBox2.Controls.Add(this.labelUserName);
            this.groupBox2.Controls.Add(this.btnSingInout);
            this.groupBox2.Location = new System.Drawing.Point(16, 27);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(241, 118);
            this.groupBox2.TabIndex = 76;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Sign In";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(87, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Account Source:";
            // 
            // cmbAccountSource
            // 
            this.cmbAccountSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAccountSource.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.cmbAccountSource.Items.AddRange(new object[] {
            "Windows Developer Center",
            "Xbox Developer Portal"});
            this.cmbAccountSource.Location = new System.Drawing.Point(19, 63);
            this.cmbAccountSource.Name = "cmbAccountSource";
            this.cmbAccountSource.Size = new System.Drawing.Size(161, 21);
            this.cmbAccountSource.TabIndex = 2;
            // 
            // labelUserName
            // 
            this.labelUserName.AutoSize = true;
            this.labelUserName.Location = new System.Drawing.Point(17, 21);
            this.labelUserName.Name = "labelUserName";
            this.labelUserName.Size = new System.Drawing.Size(0, 13);
            this.labelUserName.TabIndex = 1;
            // 
            // btnSingInout
            // 
            this.btnSingInout.Location = new System.Drawing.Point(158, 88);
            this.btnSingInout.Name = "btnSingInout";
            this.btnSingInout.Size = new System.Drawing.Size(75, 23);
            this.btnSingInout.TabIndex = 0;
            this.btnSingInout.Text = "Sign In";
            this.btnSingInout.UseVisualStyleBackColor = true;
            this.btnSingInout.Click += new System.EventHandler(this.SingInout_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1237, 912);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.lblDocCount);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Intro);
            this.Controls.Add(this.menuStrip2);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "MPSD Session History Viewer";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ClientSizeChanged += new System.EventHandler(this.Form1_ClientSizeChanged);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.menuStrip2.ResumeLayout(false);
            this.menuStrip2.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.downloadPanel.ResumeLayout(false);
            this.downloadPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.SessionDocumentContextMenuStrip.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Intro;
        private System.Windows.Forms.Label lblScid;
        private System.Windows.Forms.TextBox tbScid;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbTemplateName;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnQuery;
        private System.Windows.Forms.TextBox tbQueryKey;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.DateTimePicker dateTimePicker2;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.TextBox tbSandbox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cmbQueryType;
        private System.Windows.Forms.MenuStrip menuStrip2;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sendFeedbackToolStripMenuItem;
        private System.Windows.Forms.Button btnPriorQuery;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Label lblChangesHelp;
        private System.Windows.Forms.ListView listView2;
        private System.Windows.Forms.Label lblChangeCount;
        private System.Windows.Forms.CheckBox checkboxLockVScroll;
        private System.Windows.Forms.Label lblChangeRight;
        private System.Windows.Forms.Label lblChangeLeft;
        private System.Windows.Forms.RichTextBox rtbSnapshotRight;
        private System.Windows.Forms.RichTextBox rtbSnapshotLeft;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem displayDateTimesAsUTCToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadSessionHistoryToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip SessionDocumentContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem saveSessionHistoryToolStripMenuItem;
        private System.Windows.Forms.Panel downloadPanel;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblExplanation;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.Label lblDocCount;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnSingInout;
        private System.Windows.Forms.Label labelUserName;
        private System.Windows.Forms.ComboBox cmbAccountSource;
        private System.Windows.Forms.Label label2;
    }
}

