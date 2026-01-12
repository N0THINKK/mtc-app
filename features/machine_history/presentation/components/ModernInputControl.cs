using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace mtc_app.features.machine_history.presentation.components
{
    public class ModernInputControl : UserControl
    {
        public enum InputTypeEnum { Text, Dropdown }

        private Label lblTitle;
        private TextBox txtInput;
        private ComboBox cmbInput;
        private Label lblError;
        private Panel pnlContainer;

        private InputTypeEnum _inputType = InputTypeEnum.Text;
        private bool _isRequired = false;

        public ModernInputControl()
        {
            InitializeCustomComponents();
            this.Padding = new Padding(5);
            this.Size = new Size(300, 85); // Default size
            this.BackColor = Color.White;
        }

        [Category("Custom Properties")]
        public bool AllowCustomText
        {
            get { return cmbInput.DropDownStyle == ComboBoxStyle.DropDown; }
            set
            {
                if (value)
                {
                    // Mode Pintar: Bisa ketik, bisa pilih, ada saran otomatis
                    cmbInput.DropDownStyle = ComboBoxStyle.DropDown;
                    cmbInput.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    cmbInput.AutoCompleteSource = AutoCompleteSource.ListItems;
                }
                else
                {
                    // Mode Kaku: Hanya bisa pilih dari list
                    cmbInput.DropDownStyle = ComboBoxStyle.DropDownList;
                    cmbInput.AutoCompleteMode = AutoCompleteMode.None;
                }
            }
        }

        [Category("Custom Properties")]
        public bool Multiline
        {
            get { return txtInput.Multiline; }
            set
            {
                txtInput.Multiline = value;
                if (value)
                {
                    this.Height = 120; // Taller for multiline
                    pnlContainer.Height = 70;
                    txtInput.Height = 60;
                    txtInput.ScrollBars = ScrollBars.Vertical;
                }
                else
                {
                    this.Height = 85; // Default
                    pnlContainer.Height = 35;
                    txtInput.Height = 20;
                    txtInput.ScrollBars = ScrollBars.None;
                }
            }
        }

        [Category("Custom Properties")]
        public string LabelText
        {
            get { return lblTitle.Text; }
            set { lblTitle.Text = value; }
        }

        [Category("Custom Properties")]
        public string InputValue
        {
            get
            {
                if (_inputType == InputTypeEnum.Dropdown)
                    return cmbInput.Text;
                return txtInput.Text;
            }
            set
            {
                if (_inputType == InputTypeEnum.Dropdown)
                {
                    if (cmbInput.Items.Contains(value))
                        cmbInput.SelectedItem = value;
                }
                else
                {
                    txtInput.Text = value;
                }
            }
        }

        [Category("Custom Properties")]
        public InputTypeEnum InputType
        {
            get { return _inputType; }
            set
            {
                _inputType = value;
                UpdateInputVisibility();
            }
        }

        [Category("Custom Properties")]
        public bool IsRequired
        {
            get { return _isRequired; }
            set { _isRequired = value; }
        }

        public void SetDropdownItems(string[] items)
        {
            cmbInput.Items.Clear();
            if (items != null)
                cmbInput.Items.AddRange(items);
        }

        public bool ValidateInput()
        {
            lblError.Visible = false;
            pnlContainer.Invalidate(); // Redraw border

            if (_isRequired && string.IsNullOrWhiteSpace(InputValue))
            {
                lblError.Text = $"{LabelText} is required.";
                lblError.Visible = true;
                return false;
            }

            return true;
        }

        private void InitializeCustomComponents()
        {
            // Title Label
            lblTitle = new Label();
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(64, 64, 64);
            lblTitle.Location = new Point(5, 5);
            this.Controls.Add(lblTitle);

            // Container Panel for Input (for rounded border effect)
            pnlContainer = new Panel();
            pnlContainer.Location = new Point(5, 25);
            pnlContainer.Size = new Size(this.Width - 10, 35);
            pnlContainer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlContainer.Paint += PnlContainer_Paint;
            pnlContainer.BackColor = Color.White;
            this.Controls.Add(pnlContainer);

            // TextBox
            txtInput = new TextBox();
            txtInput.BorderStyle = BorderStyle.None;
            txtInput.Font = new Font("Segoe UI", 10F);
            txtInput.Location = new Point(10, 8);
            txtInput.Width = pnlContainer.Width - 20;
            txtInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlContainer.Controls.Add(txtInput);

            // ComboBox
            cmbInput = new ComboBox();
            cmbInput.FlatStyle = FlatStyle.Flat;
            cmbInput.Font = new Font("Segoe UI", 10F);
            cmbInput.Location = new Point(10, 6);
            cmbInput.Width = pnlContainer.Width - 20;
            cmbInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbInput.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbInput.Visible = false;
            pnlContainer.Controls.Add(cmbInput);

            // Error Label
            lblError = new Label();
            lblError.AutoSize = true;
            lblError.Font = new Font("Segoe UI", 8F);
            lblError.ForeColor = Color.Red;
            lblError.Location = new Point(5, 63);
            lblError.Visible = false;
            this.Controls.Add(lblError);
        }

        private void UpdateInputVisibility()
        {
            txtInput.Visible = _inputType == InputTypeEnum.Text;
            cmbInput.Visible = _inputType == InputTypeEnum.Dropdown;
        }

        private void PnlContainer_Paint(object sender, PaintEventArgs e)
        {
            // Draw rounded border for the input container
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            Rectangle rect = pnlContainer.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            using (GraphicsPath path = GetRoundedPath(rect, 8))
            using (Pen pen = new Pen(lblError.Visible ? Color.Red : Color.FromArgb(200, 200, 200), 1))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.X + rect.Width - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.X + rect.Width - d, rect.Y + rect.Height - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (pnlContainer != null)
            {
                pnlContainer.Invalidate(); // Redraw on resize
            }
        }
    }
}