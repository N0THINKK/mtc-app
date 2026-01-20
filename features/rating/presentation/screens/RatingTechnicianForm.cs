using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.technician.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.rating.presentation.screens
{
    public class RatingTechnicianForm : AppBaseForm
    {
        private readonly TechnicianRepository _repository;
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
        
        // GL Rating Display (Read Only)
        private AppStarRating _glRatingControl;
        private AppLabel _lblGlNote;

        public RatingTechnicianForm(long ticketId)
        {
            _repository = new TechnicianRepository();
            _ticketId = ticketId;
            
            InitializeCustomComponent();
            _ = LoadTicketDataAsync();
        }

        private void InitializeCustomComponent()
        {
            this.Text = "Rating Operator";
            this.Size = new Size(500, 850); // Increased height
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
                Text = "Penilaian Operator", 
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
            _lblActionDetails = AddDetailRow(mainLayout, "Tindakan:");

            // 3. Time Metrics
            AddSectionHeader(mainLayout, "Durasi Pengerjaan");
            _lblArrivalDuration = AddInfoRow(mainLayout, "Respon (Arrival):");
            _lblRepairDuration = AddInfoRow(mainLayout, "Pengerjaan (Repair):");

            // 4. GL Rating (Read Only)
            AddSectionHeader(mainLayout, "Penilaian GL");
            
            mainLayout.Controls.Add(new AppLabel 
            { 
                Text = "Rating dari GL:", 
                Type = AppLabel.LabelType.Subtitle,
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 2)
            });

            _glRatingControl = new AppStarRating { IsReadOnly = true };
            _glRatingControl.Margin = new Padding(0, 0, 0, 10);
            mainLayout.Controls.Add(_glRatingControl);

            mainLayout.Controls.Add(new AppLabel 
            { 
                Text = "Catatan dari GL:", 
                Type = AppLabel.LabelType.BodySmall, 
                Margin = new Padding(0, 0, 0, 2)
            });

            _lblGlNote = new AppLabel 
            { 
                Text = "-", 
                Type = AppLabel.LabelType.Body, 
                AutoSize = true,
                MaximumSize = new Size(440, 0),
                Margin = new Padding(0, 0, 0, 20)
            };
            mainLayout.Controls.Add(_lblGlNote);

            // 5. Rating Input (Operator)
            AddSectionHeader(mainLayout, "Penilaian Operator");
            
            mainLayout.Controls.Add(new AppLabel 
            { 
                Text = "Rating Operator (1-5):", 
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
                LabelText = "Catatan untuk Operator",
                InputType = AppInput.InputTypeEnum.Text,
                Multiline = true,
                Width = 440,
                Margin = new Padding(0, 0, 0, 20)
            };
            mainLayout.Controls.Add(_inputNote);

            // Submit Button
            _btnSubmit = new AppButton
            {
                Text = "Simpan Penilaian",
                Type = AppButton.ButtonType.Primary,
                Width = 440,
                Height = 45,
                Margin = new Padding(0, 10, 0, 20)
            };
            _btnSubmit.Click += async (s, e) => await BtnSubmit_ClickAsync(s, e);
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
                Width = 120, // Increased width for alignment like GL form
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

        private async Task LoadTicketDataAsync()
        {
            try
            {
                var data = await _repository.GetTicketDetailAsync(_ticketId);

                if (data != null)
                {
                    _lblOperatorName.Text = data.OperatorName ?? "-";
                    _lblMachineName.Text = data.MachineName ?? "-";
                    _lblTechnicianName.Text = data.TechnicianName ?? "-";
                    _lblFailureDetails.Text = data.FailureDetails ?? "-";
                    _lblActionDetails.Text = data.ActionDetails ?? "-";

                    // Calculate Durations
                    if (data.StartedAt.HasValue)
                    {
                        TimeSpan arrival = data.StartedAt.Value - data.CreatedAt;
                        _lblArrivalDuration.Text = arrival.ToString(@"hh\:mm\:ss");
                    }
                    
                    if (data.StartedAt.HasValue && data.FinishedAt.HasValue)
                    {
                        TimeSpan repair = data.FinishedAt.Value - data.StartedAt.Value;
                        _lblRepairDuration.Text = repair.ToString(@"hh\:mm\:ss");
                    }

                    // Populate GL Rating (Read Only)
                    if (data.GlRatingScore.HasValue)
                        _glRatingControl.Rating = data.GlRatingScore.Value;
                    
                    if (!string.IsNullOrEmpty(data.GlRatingNote))
                        _lblGlNote.Text = data.GlRatingNote;
                    else
                        _lblGlNote.Text = "(Belum dinilai oleh GL)";

                    // Populate Operator Rating (Existing Input)
                    if (data.TechRatingScore.HasValue)
                        _starRating.Rating = data.TechRatingScore.Value;
                    
                    if (!string.IsNullOrEmpty(data.TechRatingNote))
                        _inputNote.InputValue = data.TechRatingNote;
                }
                else
                {
                    MessageBox.Show("Data tiket tidak ditemukan!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat data: {ex.Message}", "Error Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task BtnSubmit_ClickAsync(object sender, EventArgs e)
        {
            if (_starRating.Rating == 0)
            {
                MessageBox.Show("Mohon berikan rating untuk operator.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                await _repository.UpdateOperatorRatingAsync(_ticketId, _starRating.Rating, _inputNote.InputValue);
                
                MessageBox.Show("Penilaian operator berhasil disimpan.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan penilaian: {ex.Message}", "Error Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
