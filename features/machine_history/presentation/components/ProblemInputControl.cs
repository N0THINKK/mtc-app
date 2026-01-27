using System;
using System.Drawing;
using System.Windows.Forms;
using Dapper;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.components
{
    public class ProblemInputControl : UserControl
    {
        public AppInput InputType { get; private set; }
        public AppInput InputFailure { get; private set; }
        private Button btnRemove;
        
        public event EventHandler RemoveRequested;

        public ProblemInputControl(int index)
        {
            InitializeComponent(index);
            LoadData();
        }

        private void InitializeComponent(int index)
        {
            this.AutoSize = true;
            this.Width = 450;
            this.Padding = new Padding(0, 0, 0, 10); 
            this.Margin = new Padding(-20, 0, 0, 0); // Force Left Alignment
            this.BackColor = Color.White;

            var layout = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Width = 450,
                WrapContents = false
            };

            // Header
            var pnlHeader = new Panel { Width = 440, Height = 30 };
                        var lblTitle = new Label 
                        {
                            Text = $"Masalah #{index + 1}", 
                            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                            AutoSize = true,
                            Location = new Point(0, 5)
                        };
                        btnRemove = new Button
                        {
                            Text = "Hapus",
                            ForeColor = AppColors.Danger,
                            FlatStyle = FlatStyle.Flat,
                            FlatAppearance = { BorderSize = 0 },
                            Cursor = Cursors.Hand,
                            AutoSize = true,
                            Location = new Point(380, 0),
                            Visible = index > 0 // First item cannot be removed usually
                        };
                        btnRemove.Click += (s, e) => RemoveRequested?.Invoke(this, EventArgs.Empty);
                        
                        pnlHeader.Controls.Add(lblTitle);
                        pnlHeader.Controls.Add(btnRemove);
                        layout.Controls.Add(pnlHeader);
            
                        // Inputs
                        InputType = new AppInput 
                        {
                            LabelText = "Jenis Problem", 
                            InputType = AppInput.InputTypeEnum.Dropdown,
                            Width = 440,
                            AllowCustomText = true,
                            IsRequired = true,
                            Margin = new Padding(0)
                        };
                        
                        InputFailure = new AppInput 
                        {
                            LabelText = "Detail Masalah", 
                            InputType = AppInput.InputTypeEnum.Dropdown, 
                            Width = 440,
                            AllowCustomText = true,
                            IsRequired = true,
                            Margin = new Padding(0)
                        };
            layout.Controls.Add(InputType);
            layout.Controls.Add(InputFailure);

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
                    
                    // Header Panel special handling for button
                    if (c is Panel pnlHeader)
                    {
                        if (btnRemove != null)
                            btnRemove.Location = new Point(pnlHeader.Width - 70, 0); // Align Right
                    }
                }
            }
        }

        private void LoadData()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    // Load Types
                    var types = conn.Query<string>("SELECT type_name FROM problem_types ORDER BY type_name");
                    InputType.SetDropdownItems(types.AsList().ToArray());

                    // Load Failures
                    var failures = conn.Query<string>("SELECT failure_name FROM failures ORDER BY failure_name");
                    InputFailure.SetDropdownItems(failures.AsList().ToArray());
                }
            }
            catch { /* Ignore loading errors */ }
        }
    }
}
