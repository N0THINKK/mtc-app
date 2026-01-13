using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    public class AppInput : UserControl
    {
        public enum InputTypeEnum { Text, Dropdown, Password }

        private Label lblTitle;
        private TextBox txtInput;
        private ComboBox cmbInput;
        private Label lblError;
        private Panel pnlContainer;

        private InputTypeEnum _inputType = InputTypeEnum.Text;
        private bool _isRequired = false;
        private bool _isFocused = false;

        public AppInput()
        {
            InitializeCustomComponents();
            this.Padding = new Padding(AppDimens.SpacingXS);
            this.Size = new Size(300, 85); 
            this.BackColor = Color.Transparent; // Let parent background show
        }

        [Category("App Properties")]
        public bool AllowCustomText
        {
            get { return cmbInput.DropDownStyle == ComboBoxStyle.DropDown; }
            set
            {
                if (value)
                {
                    cmbInput.DropDownStyle = ComboBoxStyle.DropDown;
                    cmbInput.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    cmbInput.AutoCompleteSource = AutoCompleteSource.ListItems;
                }
                else
                {
                    cmbInput.DropDownStyle = ComboBoxStyle.DropDownList;
                    cmbInput.AutoCompleteMode = AutoCompleteMode.None;
                }
            }
        }

        [Category("App Properties")]
        public bool Multiline
        {
            get { return txtInput.Multiline; }
            set
            {
                txtInput.Multiline = value;
                if (value)
                {
                    this.Height = 130;
                    pnlContainer.Height = 80;
                    txtInput.Height = 70;
                    txtInput.ScrollBars = ScrollBars.Vertical;
                }
                else
                {
                    this.Height = 85; 
                    pnlContainer.Height = AppDimens.ControlHeight;
                    txtInput.Height = 20; // Internal height doesn't matter much for single line
                    txtInput.ScrollBars = ScrollBars.None;
                }
            }
        }

        [Category("App Properties")]
        public string LabelText
        {
            get { return lblTitle.Text; }
            set { lblTitle.Text = value; }
        }

        [Category("App Properties")]
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
                    // Check if item exists, if so select it, else text (if allowed)
                    if (cmbInput.Items.Contains(value))
                        cmbInput.SelectedItem = value;
                    else if (AllowCustomText)
                        cmbInput.Text = value;
                }
                else
                {
                    txtInput.Text = value;
                }
            }
        }

        [Category("App Properties")]
        public InputTypeEnum InputType
        {
            get { return _inputType; }
            set
            {
                _inputType = value;
                UpdateInputVisibility();
            }
        }

        [Category("App Properties")]
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
            
            // Check Required
            if (_isRequired && string.IsNullOrWhiteSpace(InputValue))
            {
                SetError($"{LabelText} is required.");
                return false;
            }

            pnlContainer.Invalidate();
            return true;
        }

        public void SetError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
            pnlContainer.Invalidate();
        }

        private void InitializeCustomComponents()
        {
            // Title Label
            lblTitle = new Label();
            lblTitle.AutoSize = true;
            lblTitle.Font = AppFonts.Subtitle;
            lblTitle.ForeColor = AppColors.TextPrimary;
            lblTitle.Location = new Point(AppDimens.SpacingXS, 0);
            this.Controls.Add(lblTitle);

            // Container Panel
            pnlContainer = new Panel();
            pnlContainer.Location = new Point(AppDimens.SpacingXS, 25);
            pnlContainer.Size = new Size(this.Width - (AppDimens.SpacingXS * 2), AppDimens.ControlHeight);
            pnlContainer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlContainer.Paint += PnlContainer_Paint;
            pnlContainer.BackColor = AppColors.Surface;
            this.Controls.Add(pnlContainer);

            // TextBox
            txtInput = new TextBox();
            txtInput.BorderStyle = BorderStyle.None;
            txtInput.Font = AppFonts.Body;
            txtInput.Location = new Point(10, 8); // Centered vertically approx
            txtInput.Width = pnlContainer.Width - 20;
            txtInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtInput.BackColor = AppColors.Surface;
            
            // Focus Events
            txtInput.Enter += (s, e) => { _isFocused = true; pnlContainer.Invalidate(); };
            txtInput.Leave += (s, e) => { _isFocused = false; pnlContainer.Invalidate(); };
            
            pnlContainer.Controls.Add(txtInput);

            // ComboBox
            cmbInput = new ComboBox();
            cmbInput.FlatStyle = FlatStyle.Flat;
            cmbInput.Font = AppFonts.Body;
            cmbInput.Location = new Point(10, 6);
            cmbInput.Width = pnlContainer.Width - 20;
            cmbInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbInput.BackColor = AppColors.Surface;
            cmbInput.DropDownStyle = ComboBoxStyle.DropDownList;
            
            // Focus Events
            cmbInput.Enter += (s, e) => { _isFocused = true; pnlContainer.Invalidate(); };
            cmbInput.Leave += (s, e) => { _isFocused = false; pnlContainer.Invalidate(); };
            
            cmbInput.Visible = false;
            pnlContainer.Controls.Add(cmbInput);

            // Error Label
            lblError = new Label();
            lblError.AutoSize = true;
            lblError.Font = AppFonts.Caption;
            lblError.ForeColor = AppColors.Error;
            lblError.Location = new Point(AppDimens.SpacingXS, 65);
            lblError.Visible = false;
            this.Controls.Add(lblError);
        }

        private void UpdateInputVisibility()
        {
            if (_inputType == InputTypeEnum.Password)
            {
                txtInput.Visible = true;
                txtInput.UseSystemPasswordChar = true;
                cmbInput.Visible = false;
            }
            else if (_inputType == InputTypeEnum.Text)
            {
                txtInput.Visible = true;
                txtInput.UseSystemPasswordChar = false;
                cmbInput.Visible = false;
            }
            else // Dropdown
            {
                txtInput.Visible = false;
                cmbInput.Visible = true;
            }
        }

        private void PnlContainer_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            Rectangle rect = pnlContainer.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            Color borderColor;
            if (lblError.Visible)
                borderColor = AppColors.Error;
            else if (_isFocused)
                borderColor = AppColors.BorderFocus;
            else
                borderColor = AppColors.Border;

            int penWidth = _isFocused ? 2 : 1;

            using (GraphicsPath path = GetRoundedPath(rect, AppDimens.CornerRadius))
            using (Pen pen = new Pen(borderColor, penWidth))
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
    }
}
