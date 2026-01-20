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
        private AppLabel _lblFailureDetails;
        private AppLabel _lblActionDetails;

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
            this.Size = new Size(500, 700);
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
            AddSectionHeader(mainLayout, "Informasi Tiket");
            _lblOperatorName = AddInfoRow(mainLayout, "Operator:");
            _lblMachineName = AddInfoRow(mainLayout, "Mesin:");
            _lblFailureDetails = AddInfoRow(mainLayout, "Kerusakan:");
            _lblActionDetails = AddInfoRow(mainLayout, "Tindakan:");

            // 2. Rating Input
            AddSectionHeader(mainLayout, "Rating & Catatan");
            
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
                Width = 100,
                Margin = new Padding(0, 2, 0, 0)
            });

            AppLabel valueLabel = new AppLabel 
            { 
                Text = "-", 
                Type = AppLabel.LabelType.Body, 
                AutoSize = true,
                MaximumSize = new Size(320, 0)
            };
            row.Controls.Add(valueLabel);

            parent.Controls.Add(row);
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
                    _lblFailureDetails.Text = data.FailureDetails ?? "-";
                    _lblActionDetails.Text = data.ActionDetails ?? "-";

                    // Populate Existing Rating
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
                MessageBox.Show("Mohon berikan rating (bintang).", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
