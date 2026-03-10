using System;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public partial class OperatorRatingForm : Form
    {
        private long _ticketId;
        private Label lblTitle;
        private Label lblSubtitle;
        private AppStarRating ratingControl;
        private AppButton btnSubmit;
        private Panel mainPanel;

        public OperatorRatingForm(long ticketId)
        {
            _ticketId = ticketId;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = AppColors.PrimaryDark;
            this.TopMost = true;

            // Main Panel to center content
            mainPanel = new Panel();
            mainPanel.Size = new Size(600, 500);
            mainPanel.BackColor = Color.White;
            mainPanel.Padding = new Padding(40);
            // Center logic in OnLoad or Resize
            
            // Title
            lblTitle = new Label();
            lblTitle.Text = "Beri Rating Layanan";
            lblTitle.Font = AppFonts.Header1;
            lblTitle.ForeColor = AppColors.TextPrimary;
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Height = 60;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;

            // Subtitle
            lblSubtitle = new Label();
            lblSubtitle.Text = "Bagaimana kinerja teknisi dalam perbaikan ini?";
            lblSubtitle.Font = AppFonts.Body;
            lblSubtitle.ForeColor = AppColors.TextSecondary;
            lblSubtitle.Dock = DockStyle.Top;
            lblSubtitle.Height = 40;
            lblSubtitle.TextAlign = ContentAlignment.TopCenter;

            // Rating Control
            ratingControl = new AppStarRating();
            ratingControl.StarSize = 50;
            ratingControl.Rating = 0;
            // We'll center this manually in layout
            
            // Submit Button
            btnSubmit = new AppButton();
            btnSubmit.Text = "Selesai";
            btnSubmit.Type = AppButton.ButtonType.Primary;
            btnSubmit.Size = new Size(200, 50);
            btnSubmit.Click += BtnSubmit_Click;

            // Add to Main Panel
            mainPanel.Controls.Add(btnSubmit);
            mainPanel.Controls.Add(ratingControl);
            mainPanel.Controls.Add(lblSubtitle);
            mainPanel.Controls.Add(lblTitle);

            this.Controls.Add(mainPanel);

            this.Load += (s, e) => {
                CenterContent();
                LoadExistingData();
            };
            this.Resize += (s, e) => {
                CenterContent();
            };
        }

        private void LoadExistingData()
        {
            if (_ticketId > 0)
            {
                try 
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        int? existingRating = conn.QueryFirstOrDefault<int?>("SELECT gl_rating_score FROM tickets WHERE ticket_id = @Id", new { Id = _ticketId });
                        if (existingRating.HasValue && existingRating.Value > 0)
                        {
                            ratingControl.Rating = existingRating.Value;
                            btnSubmit.Text = "Tutup";
                        }
                    }
                }
                catch { }
            }
        }

        private void CenterContent()
        {
            if (mainPanel != null)
            {
                mainPanel.Location = new Point(
                    (this.Width - mainPanel.Width) / 2,
                    (this.Height - mainPanel.Height) / 2
                );
                
                // Center items within mainPanel
                int centerX = mainPanel.Width / 2;
                
                ratingControl.Location = new Point(
                    centerX - (ratingControl.Width / 2),
                    lblSubtitle.Bottom + 40
                );

                btnSubmit.Location = new Point(
                    centerX - (btnSubmit.Width / 2),
                    ratingControl.Bottom + 60
                );
            }
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (btnSubmit.Text == "Tutup")
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            if (ratingControl.Rating == 0)
            {
                MessageBox.Show("Silakan pilih rating terlebih dahulu.", "Info");
                return;
            }

            SaveRating();
        }

        private void SaveRating()
        {
            // Jika tiket tidak ada (contoh: bypass dari Dashboard Patroli NG), 
            // rating sekadar formalitas UI, tidak dicatat di DB.
            if (_ticketId == 0)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            // Offline Mode
            if (_ticketId < 0)
            {
                try
                {
                    int pendingId = (int)Math.Abs(_ticketId);
                    var request = mtc_app.shared.infrastructure.ServiceLocator.OfflineRepo.GetPendingTicketById(pendingId);
                    if (request != null)
                    {
                        // Use GlRatingScore for operator/GL rating
                        request.GlRatingScore = ratingControl.Rating; 
                        
                        mtc_app.shared.infrastructure.ServiceLocator.OfflineRepo.UpdatePendingTicket(pendingId, request);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving offline: {ex.Message}");
                }
            }
            // Online Mode
            else
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        string sql = "UPDATE tickets SET gl_rating_score = @Score WHERE ticket_id = @Id";
                        conn.Execute(sql, new { Id = _ticketId, Score = ratingControl.Rating });
                    }
                }
                catch (Exception ex)
                {
                     MessageBox.Show($"Error saving rating: {ex.Message}");
                     return;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
