using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.group_leader.data.dtos;
using mtc_app.features.group_leader.data.repositories;
using mtc_app.features.rating.presentation.controllers;
using mtc_app.shared.infrastructure;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.rating.presentation.screens
{
    public class RatingGlForm : AppBaseForm, IRatingGlView
    {
        private readonly RatingGlController _controller;
        
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
        private AppLabel _lblReportTime;
        private AppLabel _lblFinishTime;
        private AppStarRating _techRatingControl;
        private AppLabel _lblTechNote;

        public RatingGlForm(long ticketId)
        {
            throw new NotSupportedException("This form now requires a Guid TicketId. use RatingGlForm(Guid ticketId)");
        }

        public RatingGlForm(Guid ticketId) : this(ServiceLocator.CreateGroupLeaderRepository(), ticketId) 
        {
        }

        public RatingGlForm(IGroupLeaderRepository repository, Guid ticketId)
        {
            _controller = new RatingGlController(this, repository, ticketId);
            
            InitializeCustomComponent();
            _ = LoadTicketDataAsync();
        }

        // ==========================================
        // IRatingGlView Implementation
        // ==========================================
        
        public int RatingScore => _starRating.Rating;
        public string RatingNote => _inputNote.InputValue;

        public void DisplayTicketData(GroupLeaderTicketDetailDto data)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => DisplayTicketData(data)));
                return;
            }

            _lblOperatorName.Text = data.OperatorName;
            _lblMachineName.Text = data.MachineName;
            _lblTechnicianName.Text = data.TechnicianName;

            if (!string.IsNullOrEmpty(data.FailureDetails))
            {
                var problems = data.FailureDetails.Split(new[] { " | " }, StringSplitOptions.None);
                _lblFailureDetails.Text = string.Join(Environment.NewLine, problems.Select((p, i) => $"{i + 1}. {p}"));
            }
            else
            {
                _lblFailureDetails.Text = "-";
            }

            if (!string.IsNullOrEmpty(data.ActionDetails))
            {
                var actions = data.ActionDetails.Split(new[] { " | " }, StringSplitOptions.None);
                _lblActionDetails.Text = string.Join(Environment.NewLine, actions.Select((a, i) => $"{i + 1}. {a}"));
            }
            else
            {
                _lblActionDetails.Text = "-";
            }

            _lblReportTime.Text = data.CreatedAt.ToString("HH:mm");
            _lblFinishTime.Text = data.FinishedAt?.ToString("HH:mm") ?? "-";

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
            
            if (data.TechRatingScore.HasValue)
                _techRatingControl.Rating = data.TechRatingScore.Value;
            
            if (!string.IsNullOrEmpty(data.TechRatingNote))
                _lblTechNote.Text = data.TechRatingNote;
            else
                _lblTechNote.Text = "(Tidak ada catatan)";

            if (data.GlRatingScore.HasValue)
                _starRating.Rating = data.GlRatingScore.Value;
            
            if (!string.IsNullOrEmpty(data.GlRatingNote))
                _inputNote.InputValue = data.GlRatingNote;
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

        public void ShowSuccess(string message, string title = "Sukses")
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowSuccess(message, title)));
                return;
            }
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        // ==========================================
        // UI Construction
        // ==========================================

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
            mainLayout.Padding = new Padding(AppDimens.MarginLarge);
            mainLayout.AutoScroll = true;
            this.Controls.Add(mainLayout);

            mainLayout.Controls.Add(new AppLabel 
            { 
                Text = "Validasi & Rating Perbaikan", 
                Type = AppLabel.LabelType.Header2,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 5)
            });

            AddSectionHeader(mainLayout, "Informasi Umum");
            _lblOperatorName = AddInfoRow(mainLayout, "Operator:");
            _lblMachineName = AddInfoRow(mainLayout, "Mesin:");
            _lblTechnicianName = AddInfoRow(mainLayout, "Teknisi:");

            AddSectionHeader(mainLayout, "Detail Laporan");
            _lblFailureDetails = AddDetailRow(mainLayout, "Kerusakan:");
            _lblActionDetails = AddDetailRow(mainLayout, "Tindakan Perbaikan:");

            AddSectionHeader(mainLayout, "Durasi Pengerjaan");
            _lblReportTime = AddInfoRow(mainLayout, "Mulai Lapor:");
            _lblFinishTime = AddInfoRow(mainLayout, "Selesai Perbaikan:");
            _lblArrivalDuration = AddInfoRow(mainLayout, "Respon (Arrival):");
            _lblRepairDuration = AddInfoRow(mainLayout, "Pengerjaan (Repair):");

            AddSectionHeader(mainLayout, "Catatan Teknisi");
            
            mainLayout.Controls.Add(new AppLabel 
            { 
                Text = "Rating Dari Teknisi:", 
                Type = AppLabel.LabelType.Subtitle,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 2)
            });

            var techRating = new AppStarRating { IsReadOnly = true };
            _techRatingControl = techRating;
            techRating.Margin = new Padding(0, 0, 0, 3);
            mainLayout.Controls.Add(techRating);

            mainLayout.Controls.Add(new AppLabel 
            { 
                Text = "Catatan:", 
                Type = AppLabel.LabelType.BodySmall, 
                Margin = new Padding(0, 0, 0, 1)
            });

            _lblTechNote = new AppLabel 
            { 
                Text = "-", 
                Type = AppLabel.LabelType.Body, 
                AutoSize = true,
                MaximumSize = new Size(440, 0),
                Margin = new Padding(0, 0, 0, 5)
            };
            mainLayout.Controls.Add(_lblTechNote);

            AddSectionHeader(mainLayout, "Penilaian GL");
            
            mainLayout.Controls.Add(new AppLabel 
            { 
                Text = "Rating Score (1-5):", 
                Type = AppLabel.LabelType.Subtitle,
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 2)
            });

            _starRating = new AppStarRating();
            _starRating.Rating = 5; 
            _starRating.Margin = new Padding(0, 0, 0, 5);
            mainLayout.Controls.Add(_starRating);

            _inputNote = new AppInput
            {
                LabelText = "Catatan Rating (Note)",
                InputType = AppInput.InputTypeEnum.Text,
                Multiline = true,
                Width = 440,
                Margin = new Padding(0, 0, 0, 5)
            };
            mainLayout.Controls.Add(_inputNote);

            _btnSubmit = new AppButton
            {
                Text = "Validasi Selesai",
                Type = AppButton.ButtonType.Primary,
                Width = 440,
                Height = AppDimens.InputHeight,
                Margin = new Padding(0, 5, 0, 5)
            };
            _btnSubmit.Click += async (s, e) => await _controller.SubmitRatingAsync();
            mainLayout.Controls.Add(_btnSubmit);
            mainLayout.Controls.Add(new Panel { Height = 40, BackColor = Color.Transparent });
        }

        private void AddSectionHeader(Control parent, string text)
        {
            parent.Controls.Add(new AppLabel 
            { 
                Text = text, 
                Type = AppLabel.LabelType.Title,
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 2)
            });
            
            Panel divider = new Panel
            {
                Height = 1,
                Width = 440,
                BackColor = AppColors.Separator,
                Margin = new Padding(0, 0, 0, 3)
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
                Margin = new Padding(0, 0, 0, 2)
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
                Margin = new Padding(0, 0, 0, 3)
            };
            parent.Controls.Add(valueLabel);
            return valueLabel;
        }

        // ==========================================
        // UI Events
        // ==========================================

        private async Task LoadTicketDataAsync()
        {
            await _controller.LoadTicketDataAsync();
        }
    }
}
