using System;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.components
{
    /// <summary>
    /// Control for technician to input cause and action for a problem reported by operator.
    /// </summary>
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
            this.AutoSize = false;
            this.Height = 210;
            this.Margin = new Padding(0, 0, 0, 15);
            this.BackColor = Color.Transparent;

            // Problem Info Label (read-only from operator)
            lblProblemInfo = new Label
            {
                Text = $"Laporan: {problemInfo}",
                Font = AppFonts.Subtitle,
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                MaximumSize = new Size(500, 0),
                Location = new Point(0, 0)
            };
            this.Controls.Add(lblProblemInfo);

            // Cause Input
            InputCause = new AppInput 
            { 
                LabelText = "Penyebab Masalah", 
                InputType = AppInput.InputTypeEnum.Dropdown,
                AllowCustomText = true,
                IsRequired = true,
                Enabled = isEnabled,
                Location = new Point(0, 30)
            };
            this.Controls.Add(InputCause);
            
            // Action Input
            InputAction = new AppInput 
            { 
                LabelText = "Tindakan Perbaikan", 
                InputType = AppInput.InputTypeEnum.Dropdown, 
                AllowCustomText = true,
                IsRequired = true,
                Enabled = isEnabled,
                Location = new Point(0, 115)
            };
            this.Controls.Add(InputAction);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            int inputWidth = this.Width - 10;
            
            if (lblProblemInfo != null) lblProblemInfo.MaximumSize = new Size(inputWidth, 0);
            if (InputCause != null) InputCause.Width = inputWidth;
            if (InputAction != null) InputAction.Width = inputWidth;
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
                    var causes = conn.Query<string>("SELECT cause_name FROM failure_causes ORDER BY cause_name");
                    InputCause.SetDropdownItems(causes.AsList().ToArray());

                    var actions = conn.Query<string>("SELECT action_name FROM actions ORDER BY action_name");
                    InputAction.SetDropdownItems(actions.AsList().ToArray());
                }
            }
            catch { /* Ignore DB errors on load */ }
        }
    }
}
