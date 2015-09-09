using System;

namespace HaMusicServer
{
    partial class MainForm
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
            this.broadcastTimer = new System.Windows.Forms.Timer(this.components);
            this.saveDbTimer = new System.Windows.Forms.Timer(this.components);
            this.console = new ConsoleControl.ConsoleControl();
            this.SuspendLayout();
            // 
            // broadcastTimer
            // 
            this.broadcastTimer.Interval = 500;
            this.broadcastTimer.Tick += new System.EventHandler(this.broadcastTimer_Tick);
            // 
            // saveDbTimer
            // 
            this.saveDbTimer.Enabled = true;
            this.saveDbTimer.Interval = 60000;
            this.saveDbTimer.Tick += new System.EventHandler(this.saveDbTimer_Tick);
            // 
            // console
            // 
            this.console.Dock = System.Windows.Forms.DockStyle.Fill;
            this.console.IsInputEnabled = true;
            this.console.Location = new System.Drawing.Point(0, 0);
            this.console.Name = "console";
            this.console.SendKeyboardCommandsToProcess = false;
            this.console.ShowDiagnostics = false;
            this.console.Size = new System.Drawing.Size(784, 562);
            this.console.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 562);
            this.Controls.Add(this.console);
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "HaMusic Server";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer broadcastTimer;
        private ConsoleControl.ConsoleControl console;
        private System.Windows.Forms.Timer saveDbTimer;
    }
}

