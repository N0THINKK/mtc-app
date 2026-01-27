using System;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.components
{
    /// <summary>
    /// Control for operator to input a single problem (type + failure detail).
    /// </summary>
    public class ProblemInputControl : UserControl
    {
        public AppInput InputType { get; private set; }
        public AppInput InputFailure { get; private set; }
        
        private Label lblTitle;
        private AppButton btnRemove;
        
        public event EventHandler RemoveRequested;

        public ProblemInputControl(int index)
        {
            InitializeComponent(index);
            LoadData();
        }

        private void InitializeComponent(int index)
        {
            this.AutoSize = false;
            this.Height = 200;
            this.Margin = new Padding(0, 0, 0, 10);
            this.BackColor = Color.Transparent;

            // Title Label
            lblTitle = new Label 
            {
                Text = $"Masalah #{index + 1}", 
                Font = AppFonts.Subtitle,
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            this.Controls.Add(lblTitle);

            // Remove Button
            btnRemove = new AppButton
            {
                Text = "Hapus",
                Type = AppButton.ButtonType.Danger,
                Width = 70,
                Height = 26,
                Visible = index > 0
            };
            btnRemove.Click += (s, e) => RemoveRequested?.Invoke(this, EventArgs.Empty);
            this.Controls.Add(btnRemove);

            // Jenis Problem Input
            InputType = new AppInput 
            {
                LabelText = "Jenis Problem", 
                InputType = AppInput.InputTypeEnum.Dropdown,
                AllowCustomText = true,
                IsRequired = true,
                Location = new Point(0, 30)
            };
            this.Controls.Add(InputType);
            
            // Detail Masalah Input
            InputFailure = new AppInput 
            {
                LabelText = "Detail Masalah", 
                InputType = AppInput.InputTypeEnum.Dropdown, 
                AllowCustomText = true,
                IsRequired = true,
                Location = new Point(0, 115)
            };
            this.Controls.Add(InputFailure);
        }

        /// <summary>
        /// Updates the problem index label and delete button visibility.
        /// </summary>
        public void UpdateIndex(int index)
        {
            lblTitle.Text = $"Masalah #{index + 1}";
            btnRemove.Visible = index > 0;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            int inputWidth = this.Width - 10;
            
            // Position delete button at top-right
            if (btnRemove != null)
                btnRemove.Location = new Point(this.Width - btnRemove.Width, 0);
            
            // Resize inputs to fill width
            if (InputType != null) InputType.Width = inputWidth;
            if (InputFailure != null) InputFailure.Width = inputWidth;
        }

        private void LoadData()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    var types = conn.Query<string>("SELECT type_name FROM problem_types ORDER BY type_name");
                    InputType.SetDropdownItems(types.AsList().ToArray());

                    var failures = conn.Query<string>("SELECT failure_name FROM failures ORDER BY failure_name");
                    InputFailure.SetDropdownItems(failures.AsList().ToArray());
                }
            }
            catch { /* Ignore DB errors on load */ }
        }
    }
}
