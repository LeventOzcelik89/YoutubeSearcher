namespace YoutubeSearcher.Controls
{
    partial class VideoItemControl
    {
        private System.ComponentModel.IContainer components = null;
        private PictureBox pictureBoxThumbnail;
        private Label lblTitle;
        private Label lblAuthor;
        private Label lblDuration;
        private Button btnDownload;
        private Button btnPreview;
        private CheckBox checkBoxSelect;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pictureBoxThumbnail = new PictureBox();
            this.lblTitle = new Label();
            this.lblAuthor = new Label();
            this.lblDuration = new Label();
            this.btnDownload = new Button();
            this.btnPreview = new Button();
            this.checkBoxSelect = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxThumbnail)).BeginInit();
            this.SuspendLayout();
            
            // pictureBoxThumbnail
            this.pictureBoxThumbnail.Location = new Point(5, 5);
            this.pictureBoxThumbnail.Size = new Size(120, 90);
            this.pictureBoxThumbnail.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBoxThumbnail.TabIndex = 0;
            this.pictureBoxThumbnail.TabStop = false;
            
            // lblTitle
            this.lblTitle.AutoSize = false;
            this.lblTitle.Location = new Point(130, 5);
            this.lblTitle.Size = new Size(300, 40);
            this.lblTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblTitle.TabIndex = 1;
            
            // lblAuthor
            this.lblAuthor.AutoSize = false;
            this.lblAuthor.Location = new Point(130, 45);
            this.lblAuthor.Size = new Size(200, 20);
            this.lblAuthor.TabIndex = 2;
            
            // lblDuration
            this.lblDuration.Location = new Point(130, 65);
            this.lblDuration.Size = new Size(100, 20);
            this.lblDuration.TabIndex = 3;
            
            // btnPreview
            this.btnPreview.Location = new Point(340, 35);
            this.btnPreview.Size = new Size(80, 25);
            this.btnPreview.Text = "Önizle";
            this.btnPreview.UseVisualStyleBackColor = true;
            this.btnPreview.Click += btnPreview_Click;
            
            // btnDownload
            this.btnDownload.Location = new Point(340, 60);
            this.btnDownload.Size = new Size(80, 25);
            this.btnDownload.Text = "İndir";
            this.btnDownload.UseVisualStyleBackColor = true;
            this.btnDownload.Click += btnDownload_Click;
            
            // checkBoxSelect
            this.checkBoxSelect.Location = new Point(430, 40);
            this.checkBoxSelect.Size = new Size(20, 20);
            this.checkBoxSelect.CheckedChanged += checkBoxSelect_CheckedChanged;
            
            // VideoItemControl
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Size = new Size(460, 100);
            this.Controls.Add(this.pictureBoxThumbnail);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblAuthor);
            this.Controls.Add(this.lblDuration);
            this.Controls.Add(this.btnPreview);
            this.Controls.Add(this.btnDownload);
            this.Controls.Add(this.checkBoxSelect);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxThumbnail)).EndInit();
            this.ResumeLayout(false);
        }
    }
}

