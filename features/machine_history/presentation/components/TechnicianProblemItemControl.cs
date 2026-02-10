using System;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using System.Linq;

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
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Margin = new Padding(0, 0, 0, AppDimens.SpacingLarge);
            this.BackColor = Color.Transparent;

            // Main Layout
            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 5,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0)
            };
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Header Label
            lblHeader = new Label
            {
                Text = "Masalah Dilaporkan:",
                Font = AppFonts.Subtitle,
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            tableLayout.Controls.Add(lblHeader, 0, 0);

            // Problem Type Input
            InputProblemType = new AppInput 
            { 
                LabelText = "Jenis Problem", 
                InputType = AppInput.InputTypeEnum.Dropdown,
                AllowCustomText = true,
                IsRequired = true,
                Enabled = isEnabled,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 15)
            };
            tableLayout.Controls.Add(InputProblemType, 0, 1);

            // Problem Detail Input
            InputProblemDetail = new AppInput 
            { 
                LabelText = "Detail Masalah", 
                InputType = AppInput.InputTypeEnum.Dropdown,
                AllowCustomText = true,
                IsRequired = true,
                Enabled = isEnabled,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 15)
            };
            tableLayout.Controls.Add(InputProblemDetail, 0, 2);

            // Cause Input
            InputCause = new AppInput 
            { 
                LabelText = "Penyebab Masalah", 
                InputType = AppInput.InputTypeEnum.Dropdown,
                AllowCustomText = true,
                IsRequired = true,
                Enabled = isEnabled,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 15)
            };
            tableLayout.Controls.Add(InputCause, 0, 3);
            
            // Action Input
            InputAction = new AppInput 
            { 
                LabelText = "Tindakan Perbaikan", 
                InputType = AppInput.InputTypeEnum.Dropdown, 
                AllowCustomText = true,
                IsRequired = true,
                Enabled = isEnabled,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 15)
            };
            tableLayout.Controls.Add(InputAction, 0, 4);

            this.Controls.Add(tableLayout);

            // Set pre-filled values AFTER controls are added
            InputProblemType.InputValue = problemType ?? "";
            InputProblemDetail.InputValue = problemDetail ?? "";
        }

        // Removed OnResize - Layout handled by Dock/Table
        /*
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            int inputWidth = this.Width - 10;
            
            if (InputProblemType != null) InputProblemType.Width = inputWidth;
            if (InputProblemDetail != null) InputProblemDetail.Width = inputWidth;
            if (InputCause != null) InputCause.Width = inputWidth;
            if (InputAction != null) InputAction.Width = inputWidth;
        }
        */

        public void SetEnabled(bool enabled)
        {
            // [MODIFIED] Problem Type and Detail are open for everyone (Operator/Technician)
            // They don't require technician verification to edit.
            InputProblemType.Enabled = true;
            InputProblemDetail.Enabled = true;
            
            // Cause and Action strictly for Verified Technician
            InputCause.Enabled = enabled;
            InputAction.Enabled = enabled;
        }

        private async void LoadDropdownData()
        {
            try
            {
                var repo = mtc_app.shared.infrastructure.ServiceLocator.CreateMasterDataRepository();

                // Problem Types
                var types = await repo.GetProblemTypesAsync();
                InputProblemType.SetDropdownItems(types.Select(x => x.TypeName).ToArray());

                // Failures
                var failures = await repo.GetFailuresAsync();
                InputProblemDetail.SetDropdownItems(failures.Select(x => x.FailureName).ToArray());

                // Causes
                var causes = await repo.GetCausesAsync();
                InputCause.SetDropdownItems(causes.Select(x => x.CauseName).ToArray());

                // Actions
                var actions = await repo.GetActionsAsync();
                InputAction.SetDropdownItems(actions.Select(x => x.ActionName).ToArray());
            }
            catch (Exception ex) 
            {
                // Last resort fallback or log
                System.Diagnostics.Debug.WriteLine($"[TechnicianProblemItem] Error loading dropdowns: {ex.Message}");
            }
        }
    }
}
