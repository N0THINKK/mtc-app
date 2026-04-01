using System;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using System.Threading.Tasks;
using System.Linq;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.infrastructure;
using mtc_app.shared.data.repositories;

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

        private FlowLayoutPanel _headerRow;

        private void InitializeComponent(int index)
        {
            this.AutoSize = false;
            this.Height = 250;
            this.Margin = new Padding(0, 0, 0, 10);
            this.BackColor = Color.Transparent;

            // Header Row: Title + Remove Button side by side
            _headerRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Height = 36,
                AutoSize = false,
                WrapContents = false,
                Location = new Point(0, 0),
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            // Title Label
            lblTitle = new Label 
            {
                Text = $"Problem #{index + 1}", 
                Font = AppFonts.Subtitle,
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 5, 10, 0)
            };
            _headerRow.Controls.Add(lblTitle);

            // Remove Button
            btnRemove = new AppButton
            {
                Text = "Hapus",
                Type = AppButton.ButtonType.Danger,
                Width = 90,
                Height = 30,
                Visible = index > 0,
                Margin = new Padding(0),
                Padding = new Padding(0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnRemove.Click += (s, e) => RemoveRequested?.Invoke(this, EventArgs.Empty);
            _headerRow.Controls.Add(btnRemove);

            this.Controls.Add(_headerRow);

            // Jenis Problem Input
            InputType = new AppInput 
            {
                LabelText = "Jenis Problem (silahkan pilih jenis problem)", 
                InputType = AppInput.InputTypeEnum.Dropdown,
                AllowCustomText = false,
                IsRequired = true,
                Location = new Point(0, 35)
            };
            this.Controls.Add(InputType);
            
            // Detail Masalah Input
            InputFailure = new AppInput 
            {
                LabelText = "Detail Problem", 
                InputType = AppInput.InputTypeEnum.Dropdown, 
                AllowCustomText = true,
                IsRequired = true,
                Location = new Point(0, 125)
            };
            this.Controls.Add(InputFailure);
        }

        /// <summary>
        /// Updates the problem index label and delete button visibility.
        /// </summary>
        public void UpdateIndex(int index)
        {
            lblTitle.Text = $"Problem #{index + 1}";
            btnRemove.Visible = index > 0;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            int inputWidth = this.Width - 10;
            
            // Resize header row to full width so button isn't clipped
            if (_headerRow != null) _headerRow.Width = this.Width;
            
            // Resize inputs to fill width
            if (InputType != null) InputType.Width = inputWidth;
            if (InputFailure != null) InputFailure.Width = inputWidth;
        }

        private async void LoadData()
        {
            // 1. Load Problem Types (Support Offline)
            try
            {
                var repo = ServiceLocator.CreateMasterDataRepository();
                var types = await repo.GetProblemTypesAsync();
                InputType.SetDropdownItems(types.Select(t => t.TypeName).ToArray());
            }
            catch (Exception ex) 
            {
                System.Diagnostics.Debug.WriteLine($"[ProblemInput] Error loading types: {ex.Message}");
            }

            // 2. Load Failures (Support Offline)
            try
            {
                var repo = ServiceLocator.CreateMasterDataRepository();
                var failures = await repo.GetFailuresAsync();
                InputFailure.SetDropdownItems(failures.Select(f => f.FailureName).ToArray());
            }
            catch (Exception ex)
            { 
               System.Diagnostics.Debug.WriteLine($"[ProblemInput] Error loading failures: {ex.Message}");
            }
        }
    }
}
