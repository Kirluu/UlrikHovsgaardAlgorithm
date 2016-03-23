namespace UlrikHovsgaardGui
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
            this.label1 = new System.Windows.Forms.Label();
            this.listboxInputLog = new System.Windows.Forms.ListBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.btnBrowseLog = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioHospital = new System.Windows.Forms.RadioButton();
            this.btnAddLog = new System.Windows.Forms.Button();
            this.radioBpiChallenge = new System.Windows.Forms.RadioButton();
            this.radioBrowsedFile = new System.Windows.Forms.RadioButton();
            this.btnAutoLog = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.dataAlphabet = new System.Windows.Forms.DataGridView();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnFinishTrace = new System.Windows.Forms.Button();
            this.btnSaveGraph = new System.Windows.Forms.Button();
            this.btnLoadGraph = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.flowPnlActivities = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSaveLog = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.activityBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.Id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ActivityName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataAlphabet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.activityBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(101, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Current log input";
            // 
            // listboxInputLog
            // 
            this.listboxInputLog.FormattingEnabled = true;
            this.listboxInputLog.Location = new System.Drawing.Point(12, 39);
            this.listboxInputLog.Name = "listboxInputLog";
            this.listboxInputLog.Size = new System.Drawing.Size(148, 615);
            this.listboxInputLog.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.Location = new System.Drawing.Point(167, 39);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1085, 516);
            this.panel1.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(164, 23);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(124, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Current output graph";
            // 
            // btnBrowseLog
            // 
            this.btnBrowseLog.Location = new System.Drawing.Point(121, 86);
            this.btnBrowseLog.Name = "btnBrowseLog";
            this.btnBrowseLog.Size = new System.Drawing.Size(73, 23);
            this.btnBrowseLog.TabIndex = 4;
            this.btnBrowseLog.Text = "Browse...";
            this.btnBrowseLog.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioBrowsedFile);
            this.groupBox1.Controls.Add(this.radioBpiChallenge);
            this.groupBox1.Controls.Add(this.btnAddLog);
            this.groupBox1.Controls.Add(this.radioHospital);
            this.groupBox1.Controls.Add(this.btnBrowseLog);
            this.groupBox1.Location = new System.Drawing.Point(167, 565);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 144);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Add a log";
            // 
            // radioHospital
            // 
            this.radioHospital.AutoSize = true;
            this.radioHospital.Location = new System.Drawing.Point(17, 19);
            this.radioHospital.Name = "radioHospital";
            this.radioHospital.Size = new System.Drawing.Size(80, 17);
            this.radioHospital.TabIndex = 5;
            this.radioHospital.TabStop = true;
            this.radioHospital.Text = "Hospital log";
            this.radioHospital.UseVisualStyleBackColor = true;
            // 
            // btnAddLog
            // 
            this.btnAddLog.Location = new System.Drawing.Point(6, 115);
            this.btnAddLog.Name = "btnAddLog";
            this.btnAddLog.Size = new System.Drawing.Size(188, 23);
            this.btnAddLog.TabIndex = 6;
            this.btnAddLog.Text = "Add";
            this.btnAddLog.UseVisualStyleBackColor = true;
            // 
            // radioBpiChallenge
            // 
            this.radioBpiChallenge.AutoSize = true;
            this.radioBpiChallenge.Location = new System.Drawing.Point(17, 42);
            this.radioBpiChallenge.Name = "radioBpiChallenge";
            this.radioBpiChallenge.Size = new System.Drawing.Size(119, 17);
            this.radioBpiChallenge.TabIndex = 7;
            this.radioBpiChallenge.TabStop = true;
            this.radioBpiChallenge.Text = "BPI Challenge 2015";
            this.radioBpiChallenge.UseVisualStyleBackColor = true;
            // 
            // radioBrowsedFile
            // 
            this.radioBrowsedFile.AutoSize = true;
            this.radioBrowsedFile.Location = new System.Drawing.Point(17, 89);
            this.radioBrowsedFile.Name = "radioBrowsedFile";
            this.radioBrowsedFile.Size = new System.Drawing.Size(88, 17);
            this.radioBrowsedFile.TabIndex = 8;
            this.radioBrowsedFile.TabStop = true;
            this.radioBrowsedFile.Text = "name of file...";
            this.radioBrowsedFile.UseVisualStyleBackColor = true;
            // 
            // btnAutoLog
            // 
            this.btnAutoLog.Location = new System.Drawing.Point(12, 657);
            this.btnAutoLog.Name = "btnAutoLog";
            this.btnAutoLog.Size = new System.Drawing.Size(148, 23);
            this.btnAutoLog.TabIndex = 6;
            this.btnAutoLog.Text = "Automatically generate log";
            this.btnAutoLog.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.dataAlphabet);
            this.groupBox2.Location = new System.Drawing.Point(373, 565);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(236, 144);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Alphabet";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.textBox1);
            this.groupBox3.Controls.Add(this.flowPnlActivities);
            this.groupBox3.Controls.Add(this.button2);
            this.groupBox3.Controls.Add(this.btnFinishTrace);
            this.groupBox3.Location = new System.Drawing.Point(615, 565);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(637, 144);
            this.groupBox3.TabIndex = 8;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Add new traces (Click Activity IDs to add them to the trace)";
            // 
            // dataAlphabet
            // 
            this.dataAlphabet.AllowUserToDeleteRows = false;
            this.dataAlphabet.AutoGenerateColumns = false;
            this.dataAlphabet.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataAlphabet.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Id,
            this.ActivityName});
            this.dataAlphabet.DataSource = this.activityBindingSource;
            this.dataAlphabet.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataAlphabet.Location = new System.Drawing.Point(3, 16);
            this.dataAlphabet.Name = "dataAlphabet";
            this.dataAlphabet.ReadOnly = true;
            this.dataAlphabet.Size = new System.Drawing.Size(230, 125);
            this.dataAlphabet.TabIndex = 5;
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(1177, 10);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(75, 23);
            this.btnReset.TabIndex = 9;
            this.btnReset.Text = "Reset";
            this.btnReset.UseVisualStyleBackColor = true;
            // 
            // btnFinishTrace
            // 
            this.btnFinishTrace.Location = new System.Drawing.Point(556, 18);
            this.btnFinishTrace.Name = "btnFinishTrace";
            this.btnFinishTrace.Size = new System.Drawing.Size(75, 23);
            this.btnFinishTrace.TabIndex = 0;
            this.btnFinishTrace.Text = "End trace";
            this.btnFinishTrace.UseVisualStyleBackColor = true;
            // 
            // btnSaveGraph
            // 
            this.btnSaveGraph.Location = new System.Drawing.Point(1059, 10);
            this.btnSaveGraph.Name = "btnSaveGraph";
            this.btnSaveGraph.Size = new System.Drawing.Size(112, 23);
            this.btnSaveGraph.TabIndex = 10;
            this.btnSaveGraph.Text = "Save DCR Graph";
            this.btnSaveGraph.UseVisualStyleBackColor = true;
            // 
            // btnLoadGraph
            // 
            this.btnLoadGraph.Location = new System.Drawing.Point(941, 10);
            this.btnLoadGraph.Name = "btnLoadGraph";
            this.btnLoadGraph.Size = new System.Drawing.Size(112, 23);
            this.btnLoadGraph.TabIndex = 11;
            this.btnLoadGraph.Text = "Load DCR Graph";
            this.btnLoadGraph.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(475, 18);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "New trace";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // flowPnlActivities
            // 
            this.flowPnlActivities.AutoScroll = true;
            this.flowPnlActivities.Location = new System.Drawing.Point(7, 47);
            this.flowPnlActivities.Name = "flowPnlActivities";
            this.flowPnlActivities.Size = new System.Drawing.Size(624, 91);
            this.flowPnlActivities.TabIndex = 3;
            // 
            // btnSaveLog
            // 
            this.btnSaveLog.Location = new System.Drawing.Point(13, 686);
            this.btnSaveLog.Name = "btnSaveLog";
            this.btnSaveLog.Size = new System.Drawing.Size(148, 23);
            this.btnSaveLog.TabIndex = 12;
            this.btnSaveLog.Text = "Save current log...";
            this.btnSaveLog.UseVisualStyleBackColor = true;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(7, 20);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(462, 20);
            this.textBox1.TabIndex = 4;
            // 
            // activityBindingSource
            // 
            this.activityBindingSource.DataSource = typeof(UlrikHovsgaardAlgorithm.Data.Activity);
            // 
            // Id
            // 
            this.Id.HeaderText = "Id";
            this.Id.Name = "Id";
            this.Id.ReadOnly = true;
            this.Id.Width = 70;
            // 
            // ActivityName
            // 
            this.ActivityName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ActivityName.HeaderText = "Name";
            this.ActivityName.Name = "ActivityName";
            this.ActivityName.ReadOnly = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1264, 721);
            this.Controls.Add(this.btnSaveLog);
            this.Controls.Add(this.btnLoadGraph);
            this.Controls.Add(this.btnSaveGraph);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.btnAutoLog);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.listboxInputLog);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "UlrikHovsgaard Algorithm";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataAlphabet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.activityBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox listboxInputLog;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnBrowseLog;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioBrowsedFile;
        private System.Windows.Forms.RadioButton radioBpiChallenge;
        private System.Windows.Forms.Button btnAddLog;
        private System.Windows.Forms.RadioButton radioHospital;
        private System.Windows.Forms.Button btnAutoLog;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.DataGridView dataAlphabet;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.FlowLayoutPanel flowPnlActivities;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnFinishTrace;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnSaveGraph;
        private System.Windows.Forms.Button btnLoadGraph;
        private System.Windows.Forms.Button btnSaveLog;
        private System.Windows.Forms.BindingSource activityBindingSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn Id;
        private System.Windows.Forms.DataGridViewTextBoxColumn ActivityName;
    }
}

