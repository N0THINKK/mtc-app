using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.micrometer_patrol.data.repositories;
using mtc_app.features.micrometer_patrol.presentation.controllers;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.micrometer_patrol.presentation.screens
{
    public class MicrometerPatrolHistoryForm : AppBaseForm, IMicrometerPatrolHistoryView
    {
        private readonly MicrometerPatrolHistoryController _controller;
        private DataGridView _dgvHistory;
        private Label _lblStatus;

        public MicrometerPatrolHistoryForm(IMicrometerPatrolRepository repository)
        {
            _controller = new MicrometerPatrolHistoryController(this, repository);
            InitializeUI();
            this.Load += async (s, e) => await _controller.LoadHistoryDataAsync();
        }

        // ==========================================
        // IMicrometerPatrolHistoryView Implementation
        // ==========================================

        public void ShowLoading()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(ShowLoading)); return; }
            _dgvHistory.Visible = false;
            _lblStatus.Visible = true;
            _lblStatus.Text = "Memuat data...";
            _lblStatus.ForeColor = AppColors.TextSecondary;
        }

        public void HideLoading()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(HideLoading)); return; }
            _lblStatus.Visible = false;
        }

        public void SetStatusMessage(string message, bool isError = false)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => SetStatusMessage(message, isError))); return; }
            _lblStatus.Text = message;
            _lblStatus.ForeColor = isError ? AppColors.Danger : AppColors.TextSecondary;
            _lblStatus.Visible = true;
            _dgvHistory.Visible = false;
        }

        public void DisplayData(DataTable data)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => DisplayData(data))); return; }
            
            _dgvHistory.DataSource = data;
            
            _dgvHistory.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            _dgvHistory.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            _dgvHistory.Columns[0].Frozen = true;

            for (int i = 1; i < _dgvHistory.Columns.Count; i++)
            {
                _dgvHistory.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                _dgvHistory.Columns[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                _dgvHistory.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells; 
            }

            _dgvHistory.CellFormatting -= DgvHistory_CellFormatting;
            _dgvHistory.CellFormatting += DgvHistory_CellFormatting;

            _dgvHistory.Visible = true;
            _lblStatus.Visible = false;
        }

        // ==========================================
        // UI Code
        // ==========================================

        private void InitializeUI()
        {
            this.Text = "Riwayat Patroli Mikrometer (Hari Ini)";
            this.Size = new Size(1200, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = this.BackColor };
            var lblTitle = new Label
            {
                Text = "Riwayat Patroli Mikrometer (Hari Ini)",
                Font = AppFonts.Header2,
                ForeColor = AppColors.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(20, 0, 0, 20)
            };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            var pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 20) };
            this.Controls.Add(pnlContent);

            _dgvHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                BackgroundColor = AppColors.CardBackground,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                ColumnHeadersVisible = true,
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                ScrollBars = ScrollBars.Both,
                RowTemplate = { Height = 45 }
            };

            _dgvHistory.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppColors.Surface,
                ForeColor = AppColors.TextPrimary,
                Font = AppFonts.Body,
                Padding = new Padding(5),
                SelectionBackColor = AppColors.Surface,
                SelectionForeColor = AppColors.TextPrimary,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                WrapMode = DataGridViewTriState.True
            };

            _dgvHistory.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = AppColors.TextSecondary,
                Font = AppFonts.Body,
                Padding = new Padding(15, 10, 15, 10),
                SelectionBackColor = AppColors.PrimaryLight,
                SelectionForeColor = AppColors.PrimaryDark,
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            pnlContent.Controls.Add(_dgvHistory);

            _lblStatus = new Label
            {
                Text = "Memuat data...",
                Font = AppFonts.Body,
                ForeColor = AppColors.TextSecondary,
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(_lblStatus);

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = this.BackColor };
            var btnClose = new Button
            {
                Text = "TUTUP",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Crimson,
                Size = new Size(120, 40),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(20, 10)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(btnClose);

            this.Controls.Add(pnlBottom);
            pnlHeader.SendToBack();
            pnlBottom.SendToBack();
            _lblStatus.SendToBack();
            pnlContent.BringToFront();
        }

        private void DgvHistory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex > 0 && e.Value != null)
            {
                string status = e.Value.ToString();
                if (status == "OK")
                {
                    e.CellStyle.ForeColor = AppColors.Success;
                    e.CellStyle.Font = AppFonts.Subtitle;
                }
                else if (status == "NG")
                {
                    e.CellStyle.ForeColor = AppColors.Danger;
                    e.CellStyle.Font = AppFonts.Subtitle;
                }
                else if (status == "NA" || status == "Tidak ada/ Tidak Pakai")
                {
                    e.CellStyle.ForeColor = Color.DimGray;
                    e.CellStyle.Font = AppFonts.Subtitle;
                }
            }
        }
    }
}
