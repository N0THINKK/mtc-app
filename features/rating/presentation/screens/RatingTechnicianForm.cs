using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mtc_app.features.rating.presentation.controllers;
using mtc_app.features.technician.data.dtos;
using mtc_app.features.technician.data.repositories;
using mtc_app.shared.infrastructure;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.rating.presentation.screens
{
    public class RatingTechnicianForm : AppBaseForm, IRatingTechnicianView
    {
        private readonly RatingTechnicianController _controller;
        
        // Input Components
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
        
        // GL Rating Display (Read Only)
        private AppLabel _lblGlNote;
        
        // Sparepart Requests
        private AppLabel _lblSparepartRequests;

        public RatingTechnicianForm(long ticketId, PatrolNgDto patrolDto = null)
        {
            _controller = new RatingTechnicianController(this, ServiceLocator.CreateTechnicianRepository(), ticketId, patrolDto);
            
            InitializeCustomComponent();
            _ = LoadTicketDataAsync();
        }

        // ==========================================
        // IRatingTechnicianView Implementation
        // ==========================================
        
        public string RatingNote => _inputNote.InputValue;

        public void DisplayTicketData(TechnicianTicketDetailDto data)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => DisplayTicketData(data)));
                return;
            }

            _lblOperatorName.Text = data.OperatorName ?? "-";
            _lblMachineName.Text = data.MachineName ?? "-";
            _lblTechnicianName.Text = data.TechnicianName ?? "-";
            
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

            if (!string.IsNullOrEmpty(data.GlRatingNote))
                _lblGlNote.Text = data.GlRatingNote;
            else
                _lblGlNote.Text = "(Belum dinilai oleh GL)";
            
            if (!string.IsNullOrEmpty(data.TechRatingNote))
            {
                _inputNote.InputValue = data.TechRatingNote;
            }

            if (!string.IsNullOrEmpty(data.SparepartRequests))
            {
                var parts = data.SparepartRequests.Split(new[] { ", " }, StringSplitOptions.None);
                _lblSparepartRequests.Text = string.Join(Environment.NewLine, parts.Select((p, i) => $"{i + 1}. {p}"));
            }
            else
            {
                _lblSparepartRequests.Text = "(Tidak ada permintaan sparepart)";
            }
        }

        public void DisplayPatrolData(PatrolNgDto patrolDto)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => DisplayPatrolData(patrolDto)));
                return;
            }

            _lblOperatorName.Text = patrolDto.RoleTarget ?? "-";
            _lblMachineName.Text = patrolDto.MachineName ?? "-";
            _lblTechnicianName.Text = "-";
            
            _lblFailureDetails.Text = patrolDto.ItemName ?? "-";
            _lblActionDetails.Text = patrolDto.ActionNote ?? "-";
            
            _lblArrivalDuration.Text = "-";
            _lblRepairDuration.Text = "-";
            
            _lblGlNote.Text = "(Belum dinilai oleh GL)";
            _lblSparepartRequests.Text = "(Tidak ada permintaan sparepart)";
            
            _inputNote.Visible = false;
            _btnSubmit.Text = "Tutup";
        }

        public void SetReadOnlyMode()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(SetReadOnlyMode));
                return;
            }
            _inputNote.Enabled = false;
            _btnSubmit.Text = "Tutup";
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
            this.Text = "Rating Dari Teknisi";
            this.Size = new Size(500, 850); 
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
                Text = "Penilaian Operator", 
                Type = AppLabel.LabelType.Header2,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, AppDimens.MarginLarge)
            });

            AddSectionHeader(mainLayout, "Informasi Umum");
            _lblOperatorName = AddInfoRow(mainLayout, "Operator:");
            _lblMachineName = AddInfoRow(mainLayout, "Mesin:");
            _lblTechnicianName = AddInfoRow(mainLayout, "Teknisi:");

            AddSectionHeader(mainLayout, "Detail Laporan");
            _lblFailureDetails = AddDetailRow(mainLayout, "Kerusakan:");
            _lblActionDetails = AddDetailRow(mainLayout, "Tindakan:");

            AddSectionHeader(mainLayout, "Durasi Pengerjaan");
            _lblReportTime = AddInfoRow(mainLayout, "Mulai Lapor:");
            _lblFinishTime = AddInfoRow(mainLayout, "Selesai Perbaikan:");
            _lblArrivalDuration = AddInfoRow(mainLayout, "Respon (Arrival):");
            _lblRepairDuration = AddInfoRow(mainLayout, "Pengerjaan (Repair):");

            AddSectionHeader(mainLayout, "Permintaan Sparepart");
            _lblSparepartRequests = AddDetailRow(mainLayout, "Part yang diminta:");

            AddSectionHeader(mainLayout, "Penilaian GL");
            
            mainLayout.Controls.Add(new AppLabel 
            { 
                Text = "Catatan dari GL:", 
                Type = AppLabel.LabelType.BodySmall, 
                Margin = new Padding(0, 0, 0, AppDimens.MarginXS)
            });

            _lblGlNote = new AppLabel 
            { 
                Text = "-", 
                Type = AppLabel.LabelType.Body, 
                AutoSize = true,
                MaximumSize = new Size(440, 0),
                Margin = new Padding(0, 0, 0, AppDimens.MarginLarge)
            };
            mainLayout.Controls.Add(_lblGlNote);

            AddSectionHeader(mainLayout, "Penilaian dari Teknisi");
            
            _inputNote = new AppInput
            {
                LabelText = "Catatan : ",
                InputType = AppInput.InputTypeEnum.Text,
                Multiline = true,
                Width = 440,
                Margin = new Padding(0, 0, 0, AppDimens.MarginLarge)
            };
            mainLayout.Controls.Add(_inputNote);

            _btnSubmit = new AppButton
            {
                Text = "Simpan Penilaian",
                Type = AppButton.ButtonType.Primary,
                Width = 440,
                Height = AppDimens.InputHeight,
                Margin = new Padding(0, AppDimens.GapStandard, 0, AppDimens.MarginLarge)
            };
            _btnSubmit.Click += async (s, e) => await _controller.SubmitRatingAsync(_btnSubmit.Text == "Tutup");
            mainLayout.Controls.Add(_btnSubmit);
            mainLayout.Controls.Add(new Panel { Height = 60, BackColor = Color.Transparent });
        }

        private void AddSectionHeader(Control parent, string text)
        {
            parent.Controls.Add(new AppLabel 
            { 
                Text = text, 
                Type = AppLabel.LabelType.Title,
                AutoSize = true,
                Margin = new Padding(0, AppDimens.GapStandard, 0, AppDimens.GapStandard)
            });
            
            Panel divider = new Panel
            {
                Height = 1,
                Width = 440,
                BackColor = AppColors.Separator,
                Margin = new Padding(0, 0, 0, AppDimens.GapStandard)
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
                Margin = new Padding(0, 0, 0, AppDimens.MarginSmall)
            };

            row.Controls.Add(new AppLabel 
            { 
                Text = label, 
                Type = AppLabel.LabelType.BodySmall, 
                Width = 120, 
                Margin = new Padding(0, AppDimens.MarginXS, 0, 0)
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
                Margin = new Padding(0, AppDimens.MarginSmall, 0, AppDimens.MarginXS)
            });

            AppLabel valueLabel = new AppLabel 
            { 
                Text = "-", 
                Type = AppLabel.LabelType.Body, 
                AutoSize = true,
                MaximumSize = new Size(440, 0),
                Margin = new Padding(0, 0, 0, AppDimens.GapStandard)
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
