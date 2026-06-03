using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.features.machine_history.presentation.controllers;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public partial class MachineRunForm : Form, IMachineRunView
    {
        private readonly MachineRunController _controller;
        private System.ComponentModel.IContainer components = null;
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblStopwatch;
        private Panel panelButton;
        private AppButton btnRun;
        private AppButton btnBack;

        public MachineRunForm(long ticketId)
        {
            _controller = new MachineRunController(this, ticketId);
            InitializeComponent();
            this.Shown += (s, e) => _controller.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            _controller.Cleanup();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblTitle = new Label();
            this.lblSubtitle = new Label();
            this.lblStopwatch = new Label();
            this.panelButton = new Panel();
            this.btnRun = new AppButton();
            this.btnBack = new AppButton();
            this.SuspendLayout();

            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = AppColors.PrimaryDark;
            this.TopMost = true;

            this.lblTitle.AutoSize = false;
            this.lblTitle.Dock = DockStyle.Top;
            this.lblTitle.Height = 150;
            this.lblTitle.TextAlign = ContentAlignment.BottomCenter;
            this.lblTitle.Font = AppFonts.MetricLarge;
            this.lblTitle.ForeColor = AppColors.TextInverse;
            this.lblTitle.Text = "PERBAIKAN SELESAI, MENUNGGU OPERATOR";

            this.lblSubtitle.AutoSize = false;
            this.lblSubtitle.Dock = DockStyle.Top;
            this.lblSubtitle.Height = 100;
            this.lblSubtitle.TextAlign = ContentAlignment.TopCenter;
            this.lblSubtitle.Font = AppFonts.Header2;
            this.lblSubtitle.ForeColor = Color.LightGray;
            this.lblSubtitle.Text = "Silakan validasi kondisi mesin.\nJika mesin sudah siap produksi, tekan tombol di bawah.";

            this.lblStopwatch.AutoSize = false;
            this.lblStopwatch.Dock = DockStyle.Top;
            this.lblStopwatch.Height = 120;
            this.lblStopwatch.TextAlign = ContentAlignment.MiddleCenter;
            this.lblStopwatch.Font = new Font("Segoe UI", 60F, FontStyle.Bold);
            this.lblStopwatch.ForeColor = Color.FromArgb(255, 193, 7);
            this.lblStopwatch.Text = "00:00:00";

            this.panelButton.Dock = DockStyle.Fill;
            this.panelButton.BackColor = Color.Transparent;

            this.btnRun.Anchor = AnchorStyles.None;
            this.btnRun.Text = "RUN MESIN (PRODUKSI)";
            this.btnRun.Type = AppButton.ButtonType.Primary;
            this.btnRun.BackColor = AppColors.Success;
            this.btnRun.Font = AppFonts.MetricMedium;
            this.btnRun.Size = new Size(500, 100);
            this.btnRun.Click += (s, e) => _controller.RunMachine();
            
            this.btnBack.Anchor = AnchorStyles.None;
            this.btnBack.Text = "KEMBALI KE PERBAIKAN";
            this.btnBack.Type = AppButton.ButtonType.Secondary;
            this.btnBack.Font = AppFonts.Header2;
            this.btnBack.Size = new Size(500, 80);
            this.btnBack.Click += (s, e) => _controller.BackToRepair();
            
            this.panelButton.Resize += (s, e) => {
                 int totalHeight = this.btnRun.Height + 20 + this.btnBack.Height;
                 int startY = (this.panelButton.Height - totalHeight) / 2;
                 
                 this.btnRun.Location = new Point((this.panelButton.Width - this.btnRun.Width) / 2, startY);
                 this.btnBack.Location = new Point((this.panelButton.Width - this.btnBack.Width) / 2, startY + this.btnRun.Height + 20);
            };
            
            this.panelButton.Controls.Add(this.btnRun);
            this.panelButton.Controls.Add(this.btnBack);

            this.Controls.Add(this.panelButton);
            this.Controls.Add(this.lblStopwatch);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblTitle);
            
            this.ResumeLayout(false);
        }

        public void UpdateStopwatchDisplay(string timeString)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStopwatchDisplay(timeString)));
                return;
            }
            lblStopwatch.Text = timeString;
        }

        public void ShowError(string message, string title = "Error")
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowError(message, title)));
                return;
            }
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void CloseForm(bool success)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => CloseForm(success)));
                return;
            }
            this.DialogResult = success ? DialogResult.OK : DialogResult.Cancel;
            this.Close();
        }

        public void OpenRatingForm(long ticketId)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OpenRatingForm(ticketId)));
                return;
            }
            using (var ratingForm = new OperatorRatingForm(ticketId))
            {
                ratingForm.ShowDialog();
            }
        }
    }
}
