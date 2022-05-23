namespace GB.WinForms
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _soundOutput.Stop();
            }

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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openROMToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.emulatorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pauseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.soundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enableBootROMToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toggleChannel1 = new System.Windows.Forms.ToolStripMenuItem();
            this._toggleChannel2 = new System.Windows.Forms.ToolStripMenuItem();
            this._toggleChannel3 = new System.Windows.Forms.ToolStripMenuItem();
            this._toggleChannel4 = new System.Windows.Forms.ToolStripMenuItem();
            this._display = new GB.WinForms.OsSpecific.BitmapDisplay();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.emulatorToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(12, 4, 0, 4);
            this.menuStrip1.Size = new System.Drawing.Size(1600, 46);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openROMToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(71, 38);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openROMToolStripMenuItem
            // 
            this.openROMToolStripMenuItem.Name = "openROMToolStripMenuItem";
            this.openROMToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openROMToolStripMenuItem.Size = new System.Drawing.Size(367, 44);
            this.openROMToolStripMenuItem.Text = "&Open ROM...";
            this.openROMToolStripMenuItem.Click += new System.EventHandler(this.OpenRom);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(364, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(367, 44);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.OnExit);
            // 
            // emulatorToolStripMenuItem
            // 
            this.emulatorToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pauseToolStripMenuItem,
            this.soundToolStripMenuItem,
            this.enableBootROMToolStripMenuItem});
            this.emulatorToolStripMenuItem.Name = "emulatorToolStripMenuItem";
            this.emulatorToolStripMenuItem.Size = new System.Drawing.Size(129, 38);
            this.emulatorToolStripMenuItem.Text = "&Emulator";
            // 
            // pauseToolStripMenuItem
            // 
            this.pauseToolStripMenuItem.CheckOnClick = true;
            this.pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
            this.pauseToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
            this.pauseToolStripMenuItem.Size = new System.Drawing.Size(359, 44);
            this.pauseToolStripMenuItem.Text = "&Pause";
            this.pauseToolStripMenuItem.CheckedChanged += new System.EventHandler(this.TogglePause);
            // 
            // soundToolStripMenuItem
            // 
            this.soundToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toggleChannel1,
            this._toggleChannel2,
            this._toggleChannel3,
            this._toggleChannel4});
            this.soundToolStripMenuItem.Name = "soundToolStripMenuItem";
            this.soundToolStripMenuItem.Size = new System.Drawing.Size(359, 44);
            this.soundToolStripMenuItem.Text = "&Sound";
            // 
            // enableBootROMToolStripMenuItem
            // 
            this.enableBootROMToolStripMenuItem.Checked = true;
            this.enableBootROMToolStripMenuItem.CheckOnClick = true;
            this.enableBootROMToolStripMenuItem.CheckState = CheckState.Checked;
            this.enableBootROMToolStripMenuItem.Name = "enableBootROMToolStripMenuItem";
            this.enableBootROMToolStripMenuItem.Size = new System.Drawing.Size(359, 44);
            this.enableBootROMToolStripMenuItem.Text = "Enable &Boot ROM";
            // 
            // _toggleChannel1
            // 
            this._toggleChannel1.Checked = true;
            this._toggleChannel1.CheckOnClick = true;
            this._toggleChannel1.CheckState = System.Windows.Forms.CheckState.Checked;
            this._toggleChannel1.Name = "_toggleChannel1";
            this._toggleChannel1.Size = new System.Drawing.Size(255, 44);
            this._toggleChannel1.Text = "Channel &1";
            this._toggleChannel1.CheckedChanged += new System.EventHandler(this.ToggleSoundChannel);
            // 
            // _toggleChannel2
            // 
            this._toggleChannel2.Checked = true;
            this._toggleChannel2.CheckOnClick = true;
            this._toggleChannel2.CheckState = System.Windows.Forms.CheckState.Checked;
            this._toggleChannel2.Name = "_toggleChannel2";
            this._toggleChannel2.Size = new System.Drawing.Size(255, 44);
            this._toggleChannel2.Text = "Channel &2";
            this._toggleChannel2.CheckedChanged += new System.EventHandler(this.ToggleSoundChannel);
            // 
            // _toggleChannel3
            // 
            this._toggleChannel3.Checked = true;
            this._toggleChannel3.CheckOnClick = true;
            this._toggleChannel3.CheckState = System.Windows.Forms.CheckState.Checked;
            this._toggleChannel3.Name = "_toggleChannel3";
            this._toggleChannel3.Size = new System.Drawing.Size(255, 44);
            this._toggleChannel3.Text = "Channel &3";
            this._toggleChannel3.CheckedChanged += new System.EventHandler(this.ToggleSoundChannel);
            // 
            // _toggleChannel4
            // 
            this._toggleChannel4.Checked = true;
            this._toggleChannel4.CheckOnClick = true;
            this._toggleChannel4.CheckState = System.Windows.Forms.CheckState.Checked;
            this._toggleChannel4.Name = "_toggleChannel4";
            this._toggleChannel4.Size = new System.Drawing.Size(255, 44);
            this._toggleChannel4.Text = "Channel &4";
            this._toggleChannel4.CheckedChanged += new System.EventHandler(this.ToggleSoundChannel);
            // 
            // _display
            // 
            this._display.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(230)))), ((int)(((byte)(248)))), ((int)(((byte)(218)))));
            this._display.DisplayEnabled = false;
            this._display.Dock = System.Windows.Forms.DockStyle.Fill;
            this._display.Location = new System.Drawing.Point(0, 44);
            this._display.Name = "_display";
            this._display.Size = new System.Drawing.Size(1600, 1296);
            this._display.TabIndex = 1;
            this._display.TabStop = false;
            this._display.Text = "Game Boy Display";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(192F, 192F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1600, 1340);
            this.Controls.Add(this._display);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "MainForm";
            this.Text = "GB.Net6";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openROMToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem emulatorToolStripMenuItem;
        private ToolStripMenuItem pauseToolStripMenuItem;
        private OsSpecific.BitmapDisplay _display;
        private ToolStripMenuItem soundToolStripMenuItem;
        private ToolStripMenuItem _toggleChannel1;
        private ToolStripMenuItem _toggleChannel2;
        private ToolStripMenuItem _toggleChannel3;
        private ToolStripMenuItem _toggleChannel4;
        private ToolStripMenuItem enableBootROMToolStripMenuItem;
    }
}