using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace mtc_app.shared.presentation.components
{
    public class ModernInputControl : UserControl
    {
        public enum InputTypeEnum { Text, Dropdown }

        private Label labelTitle;
        private TextBox textInput;
        private ComboBox comboInput;
        private Label labelError;
        private Panel panelContainer;

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
            get { return comboInput.DropDownStyle == ComboBoxStyle.DropDown; }
            set
            {
                if (value)
                {
                    // Mode Pintar: Bisa ketik, bisa pilih, ada saran otomatis
                    comboInput.DropDownStyle = ComboBoxStyle.DropDown;
                    comboInput.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    comboInput.AutoCompleteSource = AutoCompleteSource.ListItems;
                }
                else
                {
                    // Mode Kaku: Hanya bisa pilih dari list
                    comboInput.DropDownStyle = ComboBoxStyle.DropDownList;
                    comboInput.AutoCompleteMode = AutoCompleteMode.None;
                }
            }
        }

        [Category("Custom Properties")]
        public bool Multiline
        {
            get { return textInput.Multiline; }
            set
            {
                textInput.Multiline = value;
                if (value)
                {
                    this.Height = 120; // Taller for multiline
                    panelContainer.Height = 70;
                    textInput.Height = 60;
                    textInput.ScrollBars = ScrollBars.Vertical;
                }
                else
                {
                    this.Height = 85; // Default
                    panelContainer.Height = 35;
                    textInput.Height = 20;
                    textInput.ScrollBars = ScrollBars.None;
                }
            }
        }

        [Category("Custom Properties")]
        public string LabelText
        {
            get { return labelTitle.Text; }
            set { labelTitle.Text = value; }
        }

        [Category("Custom Properties")]
        public string InputValue
        {
            get
            {
                if (_inputType == InputTypeEnum.Dropdown)
                    return comboInput.Text;
                return textInput.Text;
            }
            set
            {
                if (_inputType == InputTypeEnum.Dropdown)
                {
                    if (comboInput.Items.Contains(value))
                        comboInput.SelectedItem = value;
                }
                else
                {
                    textInput.Text = value;
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
            comboInput.Items.Clear();
            if (items != null)
                comboInput.Items.AddRange(items);
        }

        public bool ValidateInput()
        {
            labelError.Visible = false;
            panelContainer.Invalidate(); // Redraw border

            if (_isRequired && string.IsNullOrWhiteSpace(InputValue))
            {
                labelError.Text = $"{LabelText} is required.";
                labelError.Visible = true;
                return false;
            }

            return true;
        }

        private void InitializeCustomComponents()
        {
            // Title Label
            labelTitle = new Label();
            labelTitle.AutoSize = true;
            labelTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            labelTitle.ForeColor = Color.FromArgb(64, 64, 64);
            labelTitle.Location = new Point(5, 5);
            this.Controls.Add(labelTitle);

            // Container Panel for Input (for rounded border effect)
            panelContainer = new Panel();
            panelContainer.Location = new Point(5, 25);
            panelContainer.Size = new Size(this.Width - 10, 35);
            panelContainer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelContainer.Paint += PanelContainer_Paint;
            panelContainer.BackColor = Color.White;
            this.Controls.Add(panelContainer);

            // TextBox
            textInput = new TextBox();
            textInput.BorderStyle = BorderStyle.None;
            textInput.Font = new Font("Segoe UI", 10F);
            textInput.Location = new Point(10, 8);
            textInput.Width = panelContainer.Width - 20;
            textInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelContainer.Controls.Add(textInput);

            // ComboBox
            comboInput = new ComboBox();
            comboInput.FlatStyle = FlatStyle.Flat;
            comboInput.Font = new Font("Segoe UI", 10F);
            comboInput.Location = new Point(10, 6);
            comboInput.Width = panelContainer.Width - 20;
            comboInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboInput.DropDownStyle = ComboBoxStyle.DropDownList;
            comboInput.Visible = false;
            panelContainer.Controls.Add(comboInput);

            // Error Label
            labelError = new Label();
            labelError.AutoSize = true;
            labelError.Font = new Font("Segoe UI", 8F);
            labelError.ForeColor = Color.Red;
            labelError.Location = new Point(5, 63);
            labelError.Visible = false;
            this.Controls.Add(labelError);
        }

        private void UpdateInputVisibility()
        {
            textInput.Visible = _inputType == InputTypeEnum.Text;
            comboInput.Visible = _inputType == InputTypeEnum.Dropdown;
        }

        private void PanelContainer_Paint(object sender, PaintEventArgs e)
        {
            // Draw rounded border for the input container
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            Rectangle rect = panelContainer.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            using (GraphicsPath path = GetRoundedPath(rect, 8))
            using (Pen pen = new Pen(labelError.Visible ? Color.Red : Color.FromArgb(200, 200, 200), 1))
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
            if (panelContainer != null)
            {
                panelContainer.Invalidate(); // Redraw on resize
            }
        }
    }
}