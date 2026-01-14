using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.features.rating.presentation.screens;

namespace mtc_app.features.group_leader.presentation.screens
{
    public partial class GroupLeaderDashboardForm : AppBaseForm
    {
        private List<TicketDto> _allTickets = new List<TicketDto>();

        public GroupLeaderDashboardForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string sql = @"
                        SELECT 
                            t.ticket_id,
                            m.machine_name,
                            u.full_name AS technician_name,
                            t.gl_rating_score,
                            t.created_at,
                            t.gl_validated_at
                        FROM tickets t
                        LEFT JOIN machines m ON t.machine_id = m.machine_id
                        LEFT JOIN users u ON t.technician_id = u.user_id
                        WHERE t.status_id >= 2 
                        ORDER BY t.created_at DESC"; 
                        // Assuming status_id 2 means 'Finished by Technician' so it appears for GL. 
                        // Or just show all? Prompt says "shows all the tickets". 
                        // I'll stick to a broader query but usually GL only sees relevant ones.
                        // Removing WHERE clause to comply with "all the tickets".
                    
                    sql = @"
                        SELECT 
                            t.ticket_id,
                            m.machine_name,
                            u.full_name AS technician_name,
                            t.gl_rating_score,
                            t.created_at,
                            t.gl_validated_at
                        FROM tickets t
                        LEFT JOIN machines m ON t.machine_id = m.machine_id
                        LEFT JOIN users u ON t.technician_id = u.user_id";

                    _allTickets = connection.Query<TicketDto>(sql).ToList();
                    RenderTickets();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            RenderTickets();
        }

        private void RenderTickets()
        {
            flowTickets.SuspendLayout();
            flowTickets.Controls.Clear();

            var filtered = _allTickets.AsEnumerable();

            // 1. Status Filter
            // "Semua", "Sudah Direview", "Belum Direview"
            int statusIndex = cmbFilterStatus.SelectedIndex;
            if (statusIndex == 1) // Sudah Direview
            {
                filtered = filtered.Where(t => t.gl_validated_at.HasValue || (t.gl_rating_score.HasValue && t.gl_rating_score > 0));
            }
            else if (statusIndex == 2) // Belum Direview
            {
                filtered = filtered.Where(t => !t.gl_validated_at.HasValue && (!t.gl_rating_score.HasValue || t.gl_rating_score == 0));
            }

            // 2. Sort Time
            // "Terbaru", "Terlama"
            int sortIndex = cmbSortTime.SelectedIndex;
            if (sortIndex == 0) // Terbaru (Desc)
            {
                filtered = filtered.OrderByDescending(t => t.created_at);
            }
            else // Terlama (Asc)
            {
                filtered = filtered.OrderBy(t => t.created_at);
            }

            foreach (var ticket in filtered)
            {
                flowTickets.Controls.Add(CreateTicketCard(ticket));
            }

            flowTickets.ResumeLayout();
        }

        private Control CreateTicketCard(TicketDto ticket)
        {
            AppCard card = new AppCard();
            card.Size = new Size(300, 160);
            card.Margin = new Padding(10);
            
            // Layout inside card
            // Machine Name
            Label lblMachine = new Label
            {
                Text = ticket.machine_name ?? "Unknown Machine",
                Font = new Font(AppFonts.Body.FontFamily, 12, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                Location = new Point(15, 15),
                AutoSize = true
            };
            card.Controls.Add(lblMachine);

            // Technician Name
            Label lblTech = new Label
            {
                Text = $"Teknisi: {ticket.technician_name ?? "-"}",
                Font = AppFonts.Body,
                ForeColor = AppColors.TextSecondary,
                Location = new Point(15, 45),
                AutoSize = true
            };
            card.Controls.Add(lblTech);

            // Time
            Label lblTime = new Label
            {
                Text = ticket.created_at.ToString("dd MMM yyyy HH:mm"),
                Font = AppFonts.BodySmall,
                ForeColor = AppColors.TextSecondary,
                Location = new Point(15, 70),
                AutoSize = true
            };
            card.Controls.Add(lblTime);

            // Rating (Stars)
            AppStarRating stars = new AppStarRating();
            stars.ReadOnly = true;
            stars.Location = new Point(12, 100);
            if (ticket.gl_rating_score.HasValue)
            {
                stars.Rating = ticket.gl_rating_score.Value;
            }
            else
            {
                stars.Rating = 0;
            }
            card.Controls.Add(stars);

            // Status Badge (Optional but helpful based on 'reviewed or not')
            Label lblStatus = new Label
            {
                AutoSize = true,
                Font = new Font(AppFonts.BodySmall.FontFamily, 8, FontStyle.Bold),
                Location = new Point(200, 15),
                TextAlign = ContentAlignment.TopRight
            };

            if (ticket.gl_validated_at.HasValue || (ticket.gl_rating_score.HasValue && ticket.gl_rating_score > 0))
            {
                lblStatus.Text = "Sudah Direview";
                lblStatus.ForeColor = AppColors.Success;
            }
            else
            {
                lblStatus.Text = "Belum Direview";
                lblStatus.ForeColor = AppColors.Warning;
            }
            // Align right
            lblStatus.Left = card.Width - lblStatus.PreferredWidth - 15;
            card.Controls.Add(lblStatus);

            // Click Event
            void HandleClick(object sender, EventArgs e)
            {
                using (var form = new RatingGlForm(ticket.ticket_id))
                {
                    form.ShowDialog();
                    LoadData(); // Refresh after closing
                }
            }

            card.Click += HandleClick;
            lblMachine.Click += HandleClick;
            lblTech.Click += HandleClick;
            lblTime.Click += HandleClick;
            stars.Click += HandleClick;
            lblStatus.Click += HandleClick;
            
            // Add Cursor Hand to indicate clickable
            card.Cursor = Cursors.Hand;
            foreach (Control c in card.Controls) c.Cursor = Cursors.Hand;

            return card;
        }

        private class TicketDto
        {
            public long ticket_id { get; set; }
            public string machine_name { get; set; }
            public string technician_name { get; set; }
            public int? gl_rating_score { get; set; }
            public DateTime created_at { get; set; }
            public DateTime? gl_validated_at { get; set; }
        }
    }
}
