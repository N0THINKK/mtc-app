using System;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.rating.presentation.screens
{
    public class RatingGlForm : AppBaseForm
    {
        private long _ticketId;
        
        // Input Components
        private AppStarRating _starRating;
        private AppInput _inputNote;
        private AppButton _btnSubmit;
        
        // Display Labels
        private AppLabel _lblOperatorName;
        private AppLabel _lblMachineName;
        private AppLabel _lblTechnicianName;
        private AppLabel _lblFailureDetails;
        private AppLabel _lblActionDetails;
        private AppLabel _lblArrivalDuration;
        private AppLabel _lblRepairDuration;

        public RatingGlForm(long ticketId)
        {
            _ticketId = ticketId;
            
            InitializeCustomComponent();
            LoadTicketData();
        }

        private void InitializeCustomComponent()
        {
            this.Text = "GL Validation";
            this.Size = new Size(500, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AutoScroll = true;

            FlowLayoutPanel mainLayout = new FlowLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.FlowDirection = FlowDirection.TopDown;
            mainLayout.WrapContents = false;
            mainLayout.Padding = new Padding(20);
            mainLayout.AutoScroll = true;
            this.Controls.Add(mainLayout);

            // Title
            mainLayout.Controls.Add(new AppLabel 
            { 
                Text = "Validasi & Rating Perbaikan", 
                Type = AppLabel.LabelType.Header2,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20)
            });

            // 1. General Info
            AddSectionHeader(mainLayout, "Informasi Umum");
            _lblOperatorName = AddInfoRow(mainLayout, "Operator:");
            _lblMachineName = AddInfoRow(mainLayout, "Mesin:");
            _lblTechnicianName = AddInfoRow(mainLayout, "Teknisi:");

            // 2. Report Details
            AddSectionHeader(mainLayout, "Detail Laporan");
            _lblFailureDetails = AddDetailRow(mainLayout, "Kerusakan:");
            _lblActionDetails = AddDetailRow(mainLayout, "Tindakan Perbaikan:");

            // 3. Time Metrics
            AddSectionHeader(mainLayout, "Durasi Pengerjaan");
            _lblArrivalDuration = AddInfoRow(mainLayout, "Respon (Arrival):");
            _lblRepairDuration = AddInfoRow(mainLayout, "Pengerjaan (Repair):");

            // 4. Rating Input
            AddSectionHeader(mainLayout, "Penilaian GL");
            
            mainLayout.Controls.Add(new AppLabel 
            { 
                Text = "Rating Score (1-5):", 
                Type = AppLabel.LabelType.Subtitle,
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 2)
            });

            _starRating = new AppStarRating();
            _starRating.Rating = 5; // Default
            _starRating.Margin = new Padding(0, 0, 0, 15);
            mainLayout.Controls.Add(_starRating);

            _inputNote = new AppInput
            {
                LabelText = "Catatan Rating (Note)",
                InputType = AppInput.InputTypeEnum.Text,
                Multiline = true,
                Width = 440,
                Margin = new Padding(0, 0, 0, 20)
            };
            mainLayout.Controls.Add(_inputNote);

            // Submit Button
            _btnSubmit = new AppButton
            {
                Text = "Validasi Selesai",
                Type = AppButton.ButtonType.Primary,
                Width = 440,
                Height = 45,
                Margin = new Padding(0, 10, 0, 20)
            };
            _btnSubmit.Click += BtnSubmit_Click;
            mainLayout.Controls.Add(_btnSubmit);
        }

        private void AddSectionHeader(Control parent, string text)
        {
            parent.Controls.Add(new AppLabel 
            { 
                Text = text, 
                Type = AppLabel.LabelType.Title,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 10)
            });
            
            // Divider
            Panel divider = new Panel
            {
                Height = 1,
                Width = 440,
                BackColor = AppColors.Separator,
                Margin = new Padding(0, 0, 0, 10)
            };
            parent.Controls.Add(divider);
        }

        private AppLabel AddInfoRow(Control parent, string label)
        {
            FlowLayoutPanel row = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Width = 440,
                Margin = new Padding(0, 0, 0, 5)
            };

            row.Controls.Add(new AppLabel 
            { 
                Text = label, 
                Type = AppLabel.LabelType.BodySmall, 
                Width = 120,
                Margin = new Padding(0, 2, 0, 0)
            });

            AppLabel valueLabel = new AppLabel 
            { 
                Text = "-", 
                Type = AppLabel.LabelType.Body, 
                AutoSize = true,
                MaximumSize = new Size(300, 0)
            };
            row.Controls.Add(valueLabel);

            parent.Controls.Add(row);
            return valueLabel;
        }

        private AppLabel AddDetailRow(Control parent, string label)
        {
            parent.Controls.Add(new AppLabel 
            { 
                Text = label, 
                Type = AppLabel.LabelType.BodySmall, 
                Margin = new Padding(0, 5, 0, 2)
            });

            AppLabel valueLabel = new AppLabel 
            { 
                Text = "-", 
                Type = AppLabel.LabelType.Body, 
                AutoSize = true,
                MaximumSize = new Size(440, 0),
                Margin = new Padding(0, 0, 0, 10)
            };
            parent.Controls.Add(valueLabel);
            return valueLabel;
        }

        private void LoadTicketData()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    string sql = @"
                        SELECT 
                            t.*,
                            m.machine_name,
                            op.full_name as operator_name,
                            tech.full_name as technician_name
                        FROM tickets t
                        LEFT JOIN machines m ON t.machine_id = m.machine_id
                        LEFT JOIN users op ON t.operator_id = op.user_id
                        LEFT JOIN users tech ON t.technician_id = tech.user_id
                        WHERE t.ticket_id = @Id";

                    var data = connection.QueryFirstOrDefault(sql, new { Id = _ticketId });

                    if (data != null)
                    {
                        _lblOperatorName.Text = data.operator_name;
                        _lblMachineName.Text = data.machine_name;
                        _lblTechnicianName.Text = data.technician_name;
                        _lblFailureDetails.Text = data.failure_details;
                        _lblActionDetails.Text = data.action_details;

                        // Calculate Durations
                        DateTime created = data.created_at;
                        DateTime? started = data.started_at; // Nullable check
                        DateTime? finished = data.technician_finished_at;

                        if (started.HasValue)
                        {
                            TimeSpan arrival = started.Value - created;
                            _lblArrivalDuration.Text = arrival.ToString(@"hh\:mm\:ss");
                        }

                        if (started.HasValue && finished.HasValue)
                        {
                            TimeSpan repair = finished.Value - started.Value;
                            _lblRepairDuration.Text = repair.ToString(@"hh\:mm\:ss");
                        }
                        
                        // If already rated, populate (optional, but good for editing)
                        // Assuming columns exist and might be filled
                        try 
                        {
                            if (data.gl_rating_score != null)
                                _starRating.Rating = Convert.ToInt32(data.gl_rating_score);
                            
                            if (data.gl_rating_note != null)
                                _inputNote.InputValue = data.gl_rating_note;
                        } 
                        catch { /* Ignore if columns don't exist yet in dynamic object */ }
                    }
                    else
                    {
                        MessageBox.Show("Data tiket tidak ditemukan!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data: {ex.Message}", "Error Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (_starRating.Rating == 0)
            {
                MessageBox.Show("Mohon berikan rating (bintang).", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    
                    // Update Ticket
                    // gl_validated_at applied here
                    string sql = @"
                        UPDATE tickets 
                        SET gl_rating_score = @Score,
                            gl_rating_note = @Note,
                            gl_validated_at = NOW(),
                            status_id = 4 
                        WHERE ticket_id = @Id";
                    // Assuming status_id 4 is 'Closed' or 'Validated'

                    connection.Execute(sql, new 
                    { 
                        Score = _starRating.Rating,
                        Note = _inputNote.InputValue,
                        Id = _ticketId
                    });

                    MessageBox.Show("Validasi berhasil disimpan.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan validasi: {ex.Message}", "Error Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
