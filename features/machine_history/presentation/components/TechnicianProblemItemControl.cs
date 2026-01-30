using System;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.components
{
    /// <summary>
    /// Control for technician to view/edit problem details and input cause and action.
    /// </summary>
    public class TechnicianProblemItemControl : UserControl
    {
        public long ProblemId { get; private set; }
        
        // Operator's problem info (editable by technician)
        public AppInput InputProblemType { get; private set; }
        public AppInput InputProblemDetail { get; private set; }
        
        // Technician's analysis
        public AppInput InputCause { get; private set; }
        public AppInput InputAction { get; private set; }
        
        private Label lblHeader;

        public TechnicianProblemItemControl(long problemId, string problemType, string problemDetail, bool isEnabled)
        {
            ProblemId = problemId;
            InitializeComponent(problemType, problemDetail, isEnabled);
            LoadDropdownData();
        }

        private void InitializeComponent(string problemType, string problemDetail, bool isEnabled)
        {
            int inputHeight = AppDimens.ControlHeight + 55;
            int spacing = AppDimens.SpacingLarge; // 32

            this.AutoSize = false;
            // Header(30) + 4 * (Input + Spacing) + Extra Buffer
            this.Height = 40 + (4 * (inputHeight + spacing)); 
            this.Margin = new Padding(0, 0, 0, AppDimens.SpacingLarge);
            this.BackColor = Color.Transparent;

            int yPos = 0;

            // Header Label
            lblHeader = new Label
            {
                Text = "Masalah Dilaporkan:",
                Font = AppFonts.Subtitle,
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(0, yPos)
            };
            this.Controls.Add(lblHeader);
            yPos += 35; // Header height + gap

            // Problem Type Input (pre-filled, editable)
            InputProblemType = new AppInput 
            { 
                LabelText = "Jenis Problem", 
                InputType = AppInput.InputTypeEnum.Dropdown,
                AllowCustomText = true,
                IsRequired = true,
                Enabled = isEnabled,
                Location = new Point(0, yPos)
            };
            this.Controls.Add(InputProblemType);
            yPos += inputHeight + spacing;

            // Problem Detail Input (pre-filled, editable)
            InputProblemDetail = new AppInput 
            { 
                LabelText = "Detail Masalah", 
                InputType = AppInput.InputTypeEnum.Dropdown,
                AllowCustomText = true,
                IsRequired = true,
                Enabled = isEnabled,
                Location = new Point(0, yPos)
            };
            this.Controls.Add(InputProblemDetail);
            yPos += inputHeight + spacing ;

            // Cause Input
            InputCause = new AppInput 
            { 
                LabelText = "Penyebab Masalah", 
                InputType = AppInput.InputTypeEnum.Dropdown,
                AllowCustomText = true,
                IsRequired = true,
                Enabled = isEnabled,
                Location = new Point(0, yPos)
            };
            this.Controls.Add(InputCause);
            yPos += inputHeight + spacing;
            
            // Action Input
            InputAction = new AppInput 
            { 
                LabelText = "Tindakan Perbaikan", 
                InputType = AppInput.InputTypeEnum.Dropdown, 
                AllowCustomText = true,
                IsRequired = true,
                Enabled = isEnabled,
                Location = new Point(0, yPos)
            };
            this.Controls.Add(InputAction);

            // Set pre-filled values AFTER controls are added
            InputProblemType.InputValue = problemType ?? "";
            InputProblemDetail.InputValue = problemDetail ?? "";
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            int inputWidth = this.Width - 10;
            
            if (InputProblemType != null) InputProblemType.Width = inputWidth;
            if (InputProblemDetail != null) InputProblemDetail.Width = inputWidth;
            if (InputCause != null) InputCause.Width = inputWidth;
            if (InputAction != null) InputAction.Width = inputWidth;
        }

        public void SetEnabled(bool enabled)
        {
            InputProblemType.Enabled = enabled;
            InputProblemDetail.Enabled = enabled;
            InputCause.Enabled = enabled;
            InputAction.Enabled = enabled;
        }

        private void LoadDropdownData()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    // Problem Types
                    var types = conn.Query<string>("SELECT type_name FROM problem_types ORDER BY type_name");
                    InputProblemType.SetDropdownItems(types.AsList().ToArray());

                    // Failures/Details
                    var failures = conn.Query<string>("SELECT failure_name FROM failures ORDER BY failure_name");
                    InputProblemDetail.SetDropdownItems(failures.AsList().ToArray());

                    // Causes
                    var causes = conn.Query<string>("SELECT cause_name FROM failure_causes ORDER BY cause_name");
                    InputCause.SetDropdownItems(causes.AsList().ToArray());

                    // Actions
                    var actions = conn.Query<string>("SELECT action_name FROM actions ORDER BY action_name");
                    InputAction.SetDropdownItems(actions.AsList().ToArray());
                }
            }
            catch { /* Ignore DB errors on load */ }
        }
    }
}
