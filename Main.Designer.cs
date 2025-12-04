namespace YoutubeSearcher
{
    partial class Main
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
            groupBox1 = new GroupBox();
            radioMp3 = new RadioButton();
            radioVideo = new RadioButton();
            groupBox2 = new GroupBox();
            txtSearch = new TextBox();
            btnSearch = new Button();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(radioMp3);
            groupBox1.Controls.Add(radioVideo);
            groupBox1.Location = new Point(641, 18);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(120, 56);
            groupBox1.TabIndex = 10;
            groupBox1.TabStop = false;
            groupBox1.Text = "Format";
            groupBox1.Enter += groupBox1_Enter;
            // 
            // radioMp3
            // 
            radioMp3.Checked = true;
            radioMp3.Location = new Point(6, 22);
            radioMp3.Name = "radioMp3";
            radioMp3.Size = new Size(50, 23);
            radioMp3.TabIndex = 11;
            radioMp3.TabStop = true;
            radioMp3.Text = "MP3";
            // 
            // radioVideo
            // 
            radioVideo.Location = new Point(56, 22);
            radioVideo.Name = "radioVideo";
            radioVideo.Size = new Size(60, 23);
            radioVideo.TabIndex = 12;
            radioVideo.Text = "Video";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(txtSearch);
            groupBox2.Controls.Add(btnSearch);
            groupBox2.Location = new Point(12, 12);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(623, 62);
            groupBox2.TabIndex = 11;
            groupBox2.TabStop = false;
            groupBox2.Text = "Youtube URL";
            // 
            // txtSearch
            // 
            txtSearch.Location = new Point(6, 22);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(500, 23);
            txtSearch.TabIndex = 7;
            // 
            // btnSearch
            // 
            btnSearch.Location = new Point(512, 22);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(100, 23);
            btnSearch.TabIndex = 8;
            btnSearch.Text = "Ara";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(775, 450);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Name = "Main";
            Text = "Main";
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private GroupBox groupBox1;
        private RadioButton radioMp3;
        private RadioButton radioVideo;
        private GroupBox groupBox2;
        private TextBox txtSearch;
        private Button btnSearch;
    }
}