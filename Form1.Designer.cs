namespace YoutubeSearcher
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private TextBox txtSearch;
        private Button btnSearch;
        private Panel panelMainVideo;
        private Label lblMainVideoTitle;
        private PictureBox pictureBoxMainThumbnail;
        private Button btnDownloadMain;
        private Button btnPreviewMain;
        private GroupBox groupBoxRelated;
        private FlowLayoutPanel flowPanelRelated;
        private Button btnDownloadSelected;
        private ProgressBar progressBar;
        private Label lblStatus;
        private GroupBox groupBoxSearchResults;
        private FlowLayoutPanel flowPanelSearchResults;
        private RadioButton radioVideo;
        private RadioButton radioMp3;
        private Label lblFormat;

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
            this.txtSearch = new TextBox();
            this.btnSearch = new Button();
            this.panelMainVideo = new Panel();
            this.lblMainVideoTitle = new Label();
            this.pictureBoxMainThumbnail = new PictureBox();
            this.btnDownloadMain = new Button();
            this.btnPreviewMain = new Button();
            this.groupBoxRelated = new GroupBox();
            this.flowPanelRelated = new FlowLayoutPanel();
            this.btnDownloadSelected = new Button();
            this.progressBar = new ProgressBar();
            this.lblStatus = new Label();
            this.groupBoxSearchResults = new GroupBox();
            this.flowPanelSearchResults = new FlowLayoutPanel();
            this.radioVideo = new RadioButton();
            this.radioMp3 = new RadioButton();
            this.lblFormat = new Label();
            this.panelMainVideo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMainThumbnail)).BeginInit();
            this.groupBoxRelated.SuspendLayout();
            this.groupBoxSearchResults.SuspendLayout();
            this.SuspendLayout();
            
            // txtSearch
            this.txtSearch.Location = new Point(12, 12);
            this.txtSearch.Size = new Size(500, 23);
            this.txtSearch.TabIndex = 0;
            this.txtSearch.KeyDown += TxtSearch_KeyDown;
            
            // btnSearch
            this.btnSearch.Location = new Point(518, 12);
            this.btnSearch.Size = new Size(100, 23);
            this.btnSearch.Text = "Ara";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += BtnSearch_Click;
            
            // lblFormat
            this.lblFormat.Location = new Point(624, 12);
            this.lblFormat.Size = new Size(50, 23);
            this.lblFormat.Text = "Format:";
            this.lblFormat.TextAlign = ContentAlignment.MiddleLeft;
            
            // radioMp3
            this.radioMp3.Location = new Point(680, 12);
            this.radioMp3.Size = new Size(50, 23);
            this.radioMp3.Text = "MP3";
            this.radioMp3.Checked = true;
            
            // radioVideo
            this.radioVideo.Location = new Point(730, 12);
            this.radioVideo.Size = new Size(60, 23);
            this.radioVideo.Text = "Video";
            
            // panelMainVideo
            this.panelMainVideo.BorderStyle = BorderStyle.FixedSingle;
            this.panelMainVideo.Location = new Point(12, 50);
            this.panelMainVideo.Size = new Size(400, 200);
            this.panelMainVideo.Controls.Add(this.pictureBoxMainThumbnail);
            this.panelMainVideo.Controls.Add(this.lblMainVideoTitle);
            this.panelMainVideo.Controls.Add(this.btnPreviewMain);
            this.panelMainVideo.Controls.Add(this.btnDownloadMain);
            
            // pictureBoxMainThumbnail
            this.pictureBoxMainThumbnail.Location = new Point(10, 10);
            this.pictureBoxMainThumbnail.Size = new Size(180, 135);
            this.pictureBoxMainThumbnail.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBoxMainThumbnail.TabIndex = 0;
            this.pictureBoxMainThumbnail.TabStop = false;
            
            // lblMainVideoTitle
            this.lblMainVideoTitle.Location = new Point(200, 10);
            this.lblMainVideoTitle.Size = new Size(190, 100);
            this.lblMainVideoTitle.AutoSize = false;
            this.lblMainVideoTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            
            // btnPreviewMain
            this.btnPreviewMain.Location = new Point(200, 120);
            this.btnPreviewMain.Size = new Size(90, 30);
            this.btnPreviewMain.Text = "Önizle";
            this.btnPreviewMain.UseVisualStyleBackColor = true;
            this.btnPreviewMain.Click += BtnPreviewMain_Click;
            
            // btnDownloadMain
            this.btnDownloadMain.Location = new Point(295, 120);
            this.btnDownloadMain.Size = new Size(90, 30);
            this.btnDownloadMain.Text = "İndir";
            this.btnDownloadMain.UseVisualStyleBackColor = true;
            this.btnDownloadMain.Click += BtnDownloadMain_Click;
            
            // groupBoxRelated
            this.groupBoxRelated.Location = new Point(418, 50);
            this.groupBoxRelated.Size = new Size(380, 400);
            this.groupBoxRelated.Text = "Benzer Şarkılar";
            this.groupBoxRelated.Controls.Add(this.flowPanelRelated);
            this.groupBoxRelated.Controls.Add(this.btnDownloadSelected);
            
            // flowPanelRelated
            this.flowPanelRelated.AutoScroll = true;
            this.flowPanelRelated.Location = new Point(6, 22);
            this.flowPanelRelated.Size = new Size(368, 340);
            this.flowPanelRelated.FlowDirection = FlowDirection.TopDown;
            this.flowPanelRelated.WrapContents = false;
            
            // btnDownloadSelected
            this.btnDownloadSelected.Location = new Point(6, 368);
            this.btnDownloadSelected.Size = new Size(368, 30);
            this.btnDownloadSelected.Text = "Seçilenleri İndir";
            this.btnDownloadSelected.UseVisualStyleBackColor = true;
            this.btnDownloadSelected.Click += BtnDownloadSelected_Click;
            
            // groupBoxSearchResults
            this.groupBoxSearchResults.Location = new Point(12, 260);
            this.groupBoxSearchResults.Size = new Size(400, 190);
            this.groupBoxSearchResults.Text = "Arama Sonuçları";
            this.groupBoxSearchResults.Controls.Add(this.flowPanelSearchResults);
            
            // flowPanelSearchResults
            this.flowPanelSearchResults.AutoScroll = true;
            this.flowPanelSearchResults.Location = new Point(6, 22);
            this.flowPanelSearchResults.Size = new Size(388, 162);
            this.flowPanelSearchResults.FlowDirection = FlowDirection.TopDown;
            this.flowPanelSearchResults.WrapContents = false;
            
            // progressBar
            this.progressBar.Location = new Point(12, 456);
            this.progressBar.Size = new Size(786, 23);
            this.progressBar.Style = ProgressBarStyle.Continuous;
            
            // lblStatus
            this.lblStatus.Location = new Point(12, 483);
            this.lblStatus.Size = new Size(786, 20);
            this.lblStatus.Text = "Hazır";
            
            // Form1
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(810, 510);
            this.Text = "YouTube Müzik İndirici";
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.lblFormat);
            this.Controls.Add(this.radioMp3);
            this.Controls.Add(this.radioVideo);
            this.Controls.Add(this.panelMainVideo);
            this.Controls.Add(this.groupBoxRelated);
            this.Controls.Add(this.groupBoxSearchResults);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblStatus);
            this.panelMainVideo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMainThumbnail)).EndInit();
            this.groupBoxRelated.ResumeLayout(false);
            this.groupBoxSearchResults.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
