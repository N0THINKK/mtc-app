using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.group_leader.data.repositories;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.rating.presentation.screens
{
    public class RatingGlForm : AppBaseForm
    {
        private readonly IGroupLeaderRepository _repository;
        private Guid _ticketId;
        
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
        private AppStarRating _techRatingControl;
        private AppLabel _lblTechNote;

        // Default constructor for Designer if needed (though DI preferred)
        public RatingGlForm(long ticketId) : this(new GroupLeaderRepository(), Guid.Empty) 
        {
            // Legacy signature support if callers still pass long - throw or handle?
            // Ideally we change callers. But for now, let's assume we might need to fetch Guid by long ID if forced.
            // Or just crash. Let's provide the proper constructor.
            throw new NotSupportedException("This form now requires a Guid TicketId. use RatingGlForm(Guid ticketId)");
        }

        public RatingGlForm(Guid ticketId) : this(new GroupLeaderRepository(), ticketId) 
        {
        }

        public RatingGlForm(IGroupLeaderRepository repository, Guid ticketId)
        {
            _repository = repository;
            _ticketId = ticketId;
            
            InitializeCustomComponent();
            _ = LoadTicketDataAsync();
        }

        private void InitializeCustomComponent()
        {
            this.Text = "GL Validation";
            this.Size = new Size(500, 800);
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

            // 3.5 Technician Rating (Read Only)
            AddSectionHeader(mainLayout, "Catatan Teknisi");
            
            // Tech Rating
            mainLayout.Controls.Add(new AppLabel 
            { 
                Text = "Rating Mandiri:", 
                Type = AppLabel.LabelType.Subtitle,
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 2)
            });

            var techRating = new AppStarRating { IsReadOnly = true };
            _techRatingControl = techRating; // Store ref to populate later
            techRating.Margin = new Padding(0, 0, 0, 10);
            mainLayout.Controls.Add(techRating);

            // Tech Note
            mainLayout.Controls.Add(new AppLabel 
            { 
                Text = "Catatan:", 
                Type = AppLabel.LabelType.BodySmall, 
                Margin = new Padding(0, 0, 0, 2)
            });

            _lblTechNote = new AppLabel 
            { 
                Text = "-", 
                Type = AppLabel.LabelType.Body, 
                AutoSize = true,
                MaximumSize = new Size(440, 0),
                Margin = new Padding(0, 0, 0, 20)
            };
            mainLayout.Controls.Add(_lblTechNote);

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

        private async Task LoadTicketDataAsync()
        {
            try
            {
                var data = await _repository.GetTicketDetailAsync(_ticketId);

                if (data != null)
                {
                    _lblOperatorName.Text = data.OperatorName;
                    _lblMachineName.Text = data.MachineName;
                    _lblTechnicianName.Text = data.TechnicianName;
                    _lblFailureDetails.Text = data.FailureDetails;
                    _lblActionDetails.Text = data.ActionDetails;

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
                    
                    // Populate Tech Rating
                    if (data.TechRatingScore.HasValue)
                        _techRatingControl.Rating = data.TechRatingScore.Value;
                    
                    if (!string.IsNullOrEmpty(data.TechRatingNote))
                        _lblTechNote.Text = data.TechRatingNote;
                    else
                        _lblTechNote.Text = "(Tidak ada catatan)";

                    // Populate Existing Rating
                    if (data.GlRatingScore.HasValue)
                        _starRating.Rating = data.GlRatingScore.Value;
                    
                    if (!string.IsNullOrEmpty(data.GlRatingNote))
                        _inputNote.InputValue = data.GlRatingNote;
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
                // Note: ValidateTicketAsync currently expects long ticket_id based on interface.
                // We have _ticketId as Guid.
                // This is a logic gap. If the Repository expects LONG, we can't pass GUID.
                // But the user insisted we use Guid for the Form.
                // I will update the Repository logic or assume I should look up the LONG ID first?
                // OR, I can update the Repository to accept Guid for ValidateTicketAsync too (overload).
                // Since I cannot change the Repo interface easily blindly, I'll rely on the repo having handled it 
                // OR I assume the repository implementation handles Guid->Long conversion implicitly if I pass dynamic?
                // No, static typing.
                // Constraint: "The 'Submit' button must call _groupLeaderRepo.ValidateTicketAsync(ticketId, rating, note)."
                // TicketId is Guid here.
                // So ValidateTicketAsync MUST accept Guid.
                // I will cast it or rely on the overload I should have added. 
                // Wait, in Step 980 I decided NOT to change ValidateTicketAsync to Guid in the repo implementation because interface was Long.
                // This creates a compile error here: ValidateTicketAsync(Guid, int, string).
                // I MUST fix the Repository Interface and Implementation to support Guid or this form fails build.
                // For now, I will call it, and then immediately go fix the Repo/Interface.
                
                // Assuming overload exists (I will add it in next step)
                 await _repository.ValidateTicketAsync(_ticketId, _starRating.Rating, _inputNote.InputValue);
                 
                // Wait, casting to concrete class breaks DI principle, but saves me from Interface change right now.
                // Better: Update Interface in next step. For now, write the call.
                
                // Oops, ValidateTicketAsync in Repo (Step 980) takes LONG.
                // I need to add `ValidateTicketAsync(Guid ticketId...)` to the Repo.
                // I will do that immediately after this file write.
                
                MessageBox.Show("Validasi berhasil disimpan.", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan validasi: {ex.Message}", "Error Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
