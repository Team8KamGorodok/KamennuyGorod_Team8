namespace KamGorodPrototype
{
    partial class MainForm
    {

        private System.ComponentModel.IContainer components = null;

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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            toolStripMain = new ToolStrip();
            btnAddFile = new ToolStripButton();
            btnRemove = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            btnRun = new ToolStripButton();
            splitContainerMain = new SplitContainer();
            listViewFiles = new ListView();
            colFile = new ColumnHeader();
            colSize = new ColumnHeader();
            colDuration = new ColumnHeader();
            colStatus = new ColumnHeader();
            tableTranscript = new TableLayoutPanel();
            lblTranscriptTitle = new Label();
            richTextTranscript = new RichTextBox();
            toolStripMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            tableTranscript.SuspendLayout();
            SuspendLayout();
            // 
            // toolStripMain
            // 
            toolStripMain.GripStyle = ToolStripGripStyle.Hidden;
            toolStripMain.ImageScalingSize = new Size(20, 20);
            toolStripMain.Items.AddRange(new ToolStripItem[] { btnAddFile, btnRemove, toolStripSeparator1, btnRun });
            toolStripMain.Location = new Point(0, 0);
            toolStripMain.Name = "toolStripMain";
            toolStripMain.Padding = new Padding(6, 2, 6, 2);
            toolStripMain.Size = new Size(1097, 27);
            toolStripMain.TabIndex = 0;
            // 
            // btnAddFile
            // 
            btnAddFile.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnAddFile.Enabled = false;
            btnAddFile.Name = "btnAddFile";
            btnAddFile.Size = new Size(95, 20);
            btnAddFile.Text = "Добавить файл";
            // 
            // btnRemove
            // 
            btnRemove.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnRemove.Enabled = false;
            btnRemove.Name = "btnRemove";
            btnRemove.Size = new Size(55, 20);
            btnRemove.Text = "Удалить";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 23);
            // 
            // btnRun
            // 
            btnRun.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnRun.Enabled = false;
            btnRun.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(68, 20);
            btnRun.Text = "Запустить";
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.Location = new Point(0, 27);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(listViewFiles);
            splitContainerMain.Panel1.Padding = new Padding(6, 6, 0, 6);
            splitContainerMain.Panel1MinSize = 300;
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(tableTranscript);
            splitContainerMain.Panel2.Padding = new Padding(0, 6, 6, 6);
            splitContainerMain.Panel2MinSize = 300;
            splitContainerMain.Size = new Size(1097, 548);
            splitContainerMain.SplitterDistance = 558;
            splitContainerMain.SplitterWidth = 5;
            splitContainerMain.TabIndex = 1;
            // 
            // listViewFiles
            // 
            listViewFiles.Columns.AddRange(new ColumnHeader[] { colFile, colSize, colDuration, colStatus });
            listViewFiles.Dock = DockStyle.Fill;
            listViewFiles.FullRowSelect = true;
            listViewFiles.GridLines = true;
            listViewFiles.Location = new Point(6, 6);
            listViewFiles.Name = "listViewFiles";
            listViewFiles.Size = new Size(552, 536);
            listViewFiles.TabIndex = 0;
            listViewFiles.UseCompatibleStateImageBehavior = false;
            listViewFiles.View = View.Details;
            // 
            // colFile
            // 
            colFile.Text = "Файл";
            colFile.Width = 260;
            // 
            // colSize
            // 
            colSize.Text = "Размер";
            colSize.TextAlign = HorizontalAlignment.Right;
            colSize.Width = 80;
            // 
            // colDuration
            // 
            colDuration.Text = "Длительность";
            colDuration.TextAlign = HorizontalAlignment.Right;
            colDuration.Width = 100;
            // 
            // colStatus
            // 
            colStatus.Text = "Статус";
            colStatus.Width = 110;
            // 
            // tableTranscript
            // 
            tableTranscript.ColumnCount = 1;
            tableTranscript.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableTranscript.Controls.Add(lblTranscriptTitle, 0, 0);
            tableTranscript.Controls.Add(richTextTranscript, 0, 2);
            tableTranscript.Dock = DockStyle.Fill;
            tableTranscript.Location = new Point(0, 6);
            tableTranscript.Name = "tableTranscript";
            tableTranscript.RowCount = 3;
            tableTranscript.RowStyles.Add(new RowStyle());
            tableTranscript.RowStyles.Add(new RowStyle());
            tableTranscript.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableTranscript.Size = new Size(528, 536);
            tableTranscript.TabIndex = 0;
            // 
            // lblTranscriptTitle
            // 
            lblTranscriptTitle.AutoSize = true;
            lblTranscriptTitle.Dock = DockStyle.Fill;
            lblTranscriptTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblTranscriptTitle.Location = new Point(3, 0);
            lblTranscriptTitle.Name = "lblTranscriptTitle";
            lblTranscriptTitle.Padding = new Padding(0, 2, 0, 4);
            lblTranscriptTitle.Size = new Size(522, 25);
            lblTranscriptTitle.TabIndex = 0;
            lblTranscriptTitle.Text = "Транскрипт: файл не выбран";
            // 
            // richTextTranscript
            // 
            richTextTranscript.BackColor = SystemColors.Window;
            richTextTranscript.Dock = DockStyle.Fill;
            richTextTranscript.Font = new Font("Segoe UI", 10F);
            richTextTranscript.Location = new Point(3, 28);
            richTextTranscript.Name = "richTextTranscript";
            richTextTranscript.ReadOnly = true;
            richTextTranscript.Size = new Size(522, 505);
            richTextTranscript.TabIndex = 2;
            richTextTranscript.Text = "";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1097, 575);
            Controls.Add(splitContainerMain);
            Controls.Add(toolStripMain);
            MinimumSize = new Size(820, 400);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Прототип";
            toolStripMain.ResumeLayout(false);
            toolStripMain.PerformLayout();
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            tableTranscript.ResumeLayout(false);
            tableTranscript.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip toolStripMain;
        private ToolStripButton btnAddFile;
        private ToolStripButton btnRemove;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton btnRun;
        private SplitContainer splitContainerMain;
        private ListView listViewFiles;
        private ColumnHeader colFile;
        private ColumnHeader colSize;
        private ColumnHeader colDuration;
        private ColumnHeader colStatus;
        private TableLayoutPanel tableTranscript;
        private Label lblTranscriptTitle;
        private RichTextBox richTextTranscript;
    }
}
