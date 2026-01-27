using System;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.components
{
    public class TechnicianProblemItemControl : UserControl
    {
        public long ProblemId { get; private set; }
        
        public AppInput InputCause { get; private set; }
        public AppInput InputAction { get; private set; }
        
        private Label lblProblemInfo;

        public TechnicianProblemItemControl(long problemId, string problemInfo, bool isEnabled)
        {
            ProblemId = problemId;
            InitializeComponent(problemInfo, isEnabled);
            LoadData();
        }

        private void InitializeComponent(string problemInfo, bool isEnabled)
        {
            this.AutoSize = true;
            this.Width = 450;
            this.Padding = new Padding(0, 0, 0, 15);
            this.Margin = new Padding(-5, 0, 0, 0); // Force Left Alignment
            this.BackColor = Color.White;

            var layout = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Width = 450,
                WrapContents = false
            };

            // Read-only Info from Operator
            lblProblemInfo = new Label
            {
                Text = $"Laporan: {problemInfo}",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                MaximumSize = new Size(440, 0),
                Margin = new Padding(0, 0, 0, 5)
            };
            layout.Controls.Add(lblProblemInfo);

            // Inputs for Technician
            InputCause = new AppInput 
            { 
                LabelText = "Penyebab Masalah", 
                InputType = AppInput.InputTypeEnum.Dropdown,
                Width = 440,
                AllowCustomText = true,
                IsRequired = true,
                Enabled = isEnabled,
                Margin = new Padding(-5, 0, 0, 0) // Shift Input Left
            };
            
            InputAction = new AppInput 
            { 
                LabelText = "Tindakan Perbaikan", 
                InputType = AppInput.InputTypeEnum.Dropdown, 
                Width = 440,
                AllowCustomText = true,
                IsRequired = true,
                Enabled = isEnabled,
                Margin = new Padding(-5, 0, 0, 0) // Shift Input Left
            };

            layout.Controls.Add(InputCause);
            layout.Controls.Add(InputAction);

            this.Controls.Add(layout);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.Controls.Count > 0 && this.Controls[0] is FlowLayoutPanel layout)
            {
                layout.Width = this.Width;
                
                foreach (Control c in layout.Controls)
                {
                    c.Width = this.Width - 10;
                }
            }
        }

        public void SetEnabled(bool enabled)
        {
            InputCause.Enabled = enabled;
            InputAction.Enabled = enabled;
        }

        private void LoadData()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    // Load Causes
                    var causes = conn.Query<string>("SELECT cause_name FROM failure_causes ORDER BY cause_name");
                    InputCause.SetDropdownItems(causes.AsList().ToArray());

                    // Load Actions
                    var actions = conn.Query<string>("SELECT action_name FROM actions ORDER BY action_name");
                    InputAction.SetDropdownItems(actions.AsList().ToArray());
                }
            }
            catch { /* Ignore */ }
        }
    }
}
