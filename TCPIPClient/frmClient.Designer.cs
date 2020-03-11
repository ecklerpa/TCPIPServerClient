namespace TCPIPClient
{
    partial class frmClient
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmClient));
            this.buttonConnectToServer = new System.Windows.Forms.Button();
            this.textBoxServer = new System.Windows.Forms.TextBox();
            this.labelStatusInfo = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxClientName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxServerListeningPort = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.imageListStatusLights = new System.Windows.Forms.ImageList(this.components);
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.buttonDisconnect = new System.Windows.Forms.Button();
            this.labelConnectionStuff = new System.Windows.Forms.Label();
            this.buttonSendDataToServer = new System.Windows.Forms.Button();
            this.textBoxText = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxNum1 = new System.Windows.Forms.TextBox();
            this.textBoxNum2 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.textBoxRcv = new System.Windows.Forms.TextBox();
            this.buttonSendToClients = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label8 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonConnectToServer
            // 
            this.buttonConnectToServer.Location = new System.Drawing.Point(37, 79);
            this.buttonConnectToServer.Name = "buttonConnectToServer";
            this.buttonConnectToServer.Size = new System.Drawing.Size(327, 23);
            this.buttonConnectToServer.TabIndex = 0;
            this.buttonConnectToServer.Text = "Connect To Server";
            this.buttonConnectToServer.UseVisualStyleBackColor = true;
            this.buttonConnectToServer.Click += new System.EventHandler(this.buttonConnectToServer_Click);
            // 
            // textBoxServer
            // 
            this.textBoxServer.Location = new System.Drawing.Point(15, 25);
            this.textBoxServer.Name = "textBoxServer";
            this.textBoxServer.Size = new System.Drawing.Size(173, 20);
            this.textBoxServer.TabIndex = 1;
            this.textBoxServer.Text = "localhost";
            // 
            // labelStatusInfo
            // 
            this.labelStatusInfo.AutoSize = true;
            this.labelStatusInfo.Location = new System.Drawing.Point(174, 108);
            this.labelStatusInfo.Name = "labelStatusInfo";
            this.labelStatusInfo.Size = new System.Drawing.Size(159, 13);
            this.labelStatusInfo.TabIndex = 2;
            this.labelStatusInfo.Text = "Click \'Connect to Server\'  button";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(166, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Address to the Server(name or IP)";
            // 
            // textBoxClientName
            // 
            this.textBoxClientName.Location = new System.Drawing.Point(233, 26);
            this.textBoxClientName.Name = "textBoxClientName";
            this.textBoxClientName.Size = new System.Drawing.Size(100, 20);
            this.textBoxClientName.TabIndex = 4;
            this.textBoxClientName.Text = "John Smith";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(230, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Client Name";
            // 
            // textBoxServerListeningPort
            // 
            this.textBoxServerListeningPort.Location = new System.Drawing.Point(129, 48);
            this.textBoxServerListeningPort.Name = "textBoxServerListeningPort";
            this.textBoxServerListeningPort.Size = new System.Drawing.Size(50, 20);
            this.textBoxServerListeningPort.TabIndex = 6;
            this.textBoxServerListeningPort.Text = "9999";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 51);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(115, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Server\'s Listening Port:";
            // 
            // imageListStatusLights
            // 
            this.imageListStatusLights.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListStatusLights.ImageStream")));
            this.imageListStatusLights.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListStatusLights.Images.SetKeyName(0, "RED");
            this.imageListStatusLights.Images.SetKeyName(1, "GREEN");
            this.imageListStatusLights.Images.SetKeyName(2, "BLUE");
            this.imageListStatusLights.Images.SetKeyName(3, "PURPLE");
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(147, 108);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(24, 24);
            this.pictureBox1.TabIndex = 8;
            this.pictureBox1.TabStop = false;
            // 
            // buttonDisconnect
            // 
            this.buttonDisconnect.Enabled = false;
            this.buttonDisconnect.Location = new System.Drawing.Point(370, 79);
            this.buttonDisconnect.Name = "buttonDisconnect";
            this.buttonDisconnect.Size = new System.Drawing.Size(75, 23);
            this.buttonDisconnect.TabIndex = 9;
            this.buttonDisconnect.Text = "Disconnect";
            this.buttonDisconnect.UseVisualStyleBackColor = true;
            this.buttonDisconnect.Click += new System.EventHandler(this.buttonDisconnect_Click);
            // 
            // labelConnectionStuff
            // 
            this.labelConnectionStuff.AutoSize = true;
            this.labelConnectionStuff.Location = new System.Drawing.Point(12, 245);
            this.labelConnectionStuff.Name = "labelConnectionStuff";
            this.labelConnectionStuff.Size = new System.Drawing.Size(16, 13);
            this.labelConnectionStuff.TabIndex = 10;
            this.labelConnectionStuff.Text = "...";
            // 
            // buttonSendDataToServer
            // 
            this.buttonSendDataToServer.Enabled = false;
            this.buttonSendDataToServer.Location = new System.Drawing.Point(15, 137);
            this.buttonSendDataToServer.Name = "buttonSendDataToServer";
            this.buttonSendDataToServer.Size = new System.Drawing.Size(185, 23);
            this.buttonSendDataToServer.TabIndex = 11;
            this.buttonSendDataToServer.Text = "Send Data To Server";
            this.buttonSendDataToServer.UseVisualStyleBackColor = true;
            this.buttonSendDataToServer.Click += new System.EventHandler(this.buttonSendDataToServer_Click);
            // 
            // textBoxText
            // 
            this.textBoxText.Location = new System.Drawing.Point(12, 163);
            this.textBoxText.Multiline = true;
            this.textBoxText.Name = "textBoxText";
            this.textBoxText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxText.Size = new System.Drawing.Size(692, 78);
            this.textBoxText.TabIndex = 12;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(585, 147);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(69, 13);
            this.label4.TabIndex = 13;
            this.label4.Text = "Text to send:";
            // 
            // textBoxNum1
            // 
            this.textBoxNum1.Location = new System.Drawing.Point(710, 163);
            this.textBoxNum1.MaxLength = 10;
            this.textBoxNum1.Name = "textBoxNum1";
            this.textBoxNum1.Size = new System.Drawing.Size(81, 20);
            this.textBoxNum1.TabIndex = 14;
            this.textBoxNum1.Text = "123456";
            // 
            // textBoxNum2
            // 
            this.textBoxNum2.Location = new System.Drawing.Point(710, 221);
            this.textBoxNum2.MaxLength = 10;
            this.textBoxNum2.Name = "textBoxNum2";
            this.textBoxNum2.Size = new System.Drawing.Size(81, 20);
            this.textBoxNum2.TabIndex = 15;
            this.textBoxNum2.Text = "54321";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(709, 147);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(85, 13);
            this.label5.TabIndex = 16;
            this.label5.Text = "Number to send:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(707, 205);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(82, 13);
            this.label6.TabIndex = 17;
            this.label6.Text = "Another number";
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.groupBox1.Controls.Add(this.listBox1);
            this.groupBox1.Location = new System.Drawing.Point(12, 277);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(150, 203);
            this.groupBox1.TabIndex = 18;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Other Clients";
            // 
            // listBox1
            // 
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(3, 16);
            this.listBox1.Name = "listBox1";
            this.listBox1.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.listBox1.Size = new System.Drawing.Size(144, 184);
            this.listBox1.TabIndex = 0;
            // 
            // textBoxRcv
            // 
            this.textBoxRcv.Location = new System.Drawing.Point(169, 333);
            this.textBoxRcv.Multiline = true;
            this.textBoxRcv.Name = "textBoxRcv";
            this.textBoxRcv.ReadOnly = true;
            this.textBoxRcv.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxRcv.Size = new System.Drawing.Size(620, 144);
            this.textBoxRcv.TabIndex = 19;
            // 
            // buttonSendToClients
            // 
            this.buttonSendToClients.Enabled = false;
            this.buttonSendToClients.Location = new System.Drawing.Point(169, 280);
            this.buttonSendToClients.Name = "buttonSendToClients";
            this.buttonSendToClients.Size = new System.Drawing.Size(235, 23);
            this.buttonSendToClients.TabIndex = 21;
            this.buttonSendToClients.Text = "Send Text to Selected Clients";
            this.buttonSendToClients.UseVisualStyleBackColor = true;
            this.buttonSendToClients.Click += new System.EventHandler(this.buttonSendToClients_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(174, 317);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(74, 13);
            this.label7.TabIndex = 22;
            this.label7.Text = "Incoming Text";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.panel1.Location = new System.Drawing.Point(586, 271);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(203, 56);
            this.panel1.TabIndex = 23;
            this.panel1.DragDrop += new System.Windows.Forms.DragEventHandler(this.panelFileDropArea_DragDrop);
            this.panel1.DragEnter += new System.Windows.Forms.DragEventHandler(this.panelFileDropArea_DragEnter);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(586, 254);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(49, 13);
            this.label8.TabIndex = 24;
            this.label8.Text = "File Drop";
            // 
            // frmClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(803, 488);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.buttonSendToClients);
            this.Controls.Add(this.textBoxRcv);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBoxNum2);
            this.Controls.Add(this.textBoxNum1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBoxText);
            this.Controls.Add(this.buttonSendDataToServer);
            this.Controls.Add(this.labelConnectionStuff);
            this.Controls.Add(this.buttonDisconnect);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxServerListeningPort);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxClientName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.labelStatusInfo);
            this.Controls.Add(this.textBoxServer);
            this.Controls.Add(this.buttonConnectToServer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "frmClient";
            this.Text = "TCPIP Client";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmClient_FormClosing);
            this.Load += new System.EventHandler(this.frmClient_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonConnectToServer;
        private System.Windows.Forms.TextBox textBoxServer;
        private System.Windows.Forms.Label labelStatusInfo;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxClientName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxServerListeningPort;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ImageList imageListStatusLights;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button buttonDisconnect;
        private System.Windows.Forms.Label labelConnectionStuff;
        private System.Windows.Forms.Button buttonSendDataToServer;
        private System.Windows.Forms.TextBox textBoxText;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxNum1;
        private System.Windows.Forms.TextBox textBoxNum2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.TextBox textBoxRcv;
        private System.Windows.Forms.Button buttonSendToClients;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label8;
    }
}

