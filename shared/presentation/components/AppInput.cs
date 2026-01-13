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

        private Label labelTitle;
        private TextBox textInput;
        private ComboBox comboInput;
        private Label labelError;
        private Panel panelContainer;

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
            get { return comboInput.DropDownStyle == ComboBoxStyle.DropDown; }
            set
            {
                if (value)
                {
                    comboInput.DropDownStyle = ComboBoxStyle.DropDown;
                    comboInput.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    comboInput.AutoCompleteSource = AutoCompleteSource.ListItems;
                }
                else
                {
                    comboInput.DropDownStyle = ComboBoxStyle.DropDownList;
                    comboInput.AutoCompleteMode = AutoCompleteMode.None;
                }
            }
        }

        [Category("App Properties")]
        public bool Multiline
        {
            get { return textInput.Multiline; }
            set
            {
                textInput.Multiline = value;
                if (value)
                {
                    this.Height = 130;
                    panelContainer.Height = 80;
                    textInput.Height = 70;
                    textInput.ScrollBars = ScrollBars.Vertical;
                }
                else
                {
                    this.Height = 85; 
                    panelContainer.Height = AppDimens.ControlHeight;
                    textInput.Height = 20; // Internal height doesn't matter much for single line
                    textInput.ScrollBars = ScrollBars.None;
                }
            }
        }

        [Category("App Properties")]
        public string LabelText
        {
            get { return labelTitle.Text; }
            set { labelTitle.Text = value; }
        }

        [Category("App Properties")]
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
                    // Check if item exists, if so select it, else text (if allowed)
                    if (comboInput.Items.Contains(value))
                        comboInput.SelectedItem = value;
                    else if (AllowCustomText)
                        comboInput.Text = value;
                }
                else
                {
                    textInput.Text = value;
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
            comboInput.Items.Clear();
            if (items != null)
                comboInput.Items.AddRange(items);
        }

        public bool ValidateInput()
        {
            labelError.Visible = false;
            
            // Check Required
            if (_isRequired && string.IsNullOrWhiteSpace(InputValue))
            {
                SetError($"{LabelText} is required.");
                return false;
            }

            panelContainer.Invalidate();
            return true;
        }

        public void SetError(string message)
        {
            labelError.Text = message;
            labelError.Visible = true;
            panelContainer.Invalidate();
        }

        private void InitializeCustomComponents()
        {
            // Title Label
            labelTitle = new Label();
            labelTitle.AutoSize = true;
            labelTitle.Font = AppFonts.Subtitle;
            labelTitle.ForeColor = AppColors.TextPrimary;
            labelTitle.Location = new Point(AppDimens.SpacingXS, 0);
            this.Controls.Add(labelTitle);

            // Container Panel
            panelContainer = new Panel();
            panelContainer.Location = new Point(AppDimens.SpacingXS, 25);
            panelContainer.Size = new Size(this.Width - (AppDimens.SpacingXS * 2), AppDimens.ControlHeight);
            panelContainer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelContainer.Paint += PanelContainer_Paint;
            panelContainer.BackColor = AppColors.Surface;
            this.Controls.Add(panelContainer);

            // TextBox
            textInput = new TextBox();
            textInput.BorderStyle = BorderStyle.None;
            textInput.Font = AppFonts.Body;
            textInput.Location = new Point(10, 8); // Centered vertically approx
            textInput.Width = panelContainer.Width - 20;
            textInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textInput.BackColor = AppColors.Surface;
            
            // Focus Events
            textInput.Enter += (s, e) => { _isFocused = true; panelContainer.Invalidate(); };
            textInput.Leave += (s, e) => { _isFocused = false; panelContainer.Invalidate(); };
            
            panelContainer.Controls.Add(textInput);

            // ComboBox
            comboInput = new ComboBox();
            comboInput.FlatStyle = FlatStyle.Flat;
            comboInput.Font = AppFonts.Body;
            comboInput.Location = new Point(10, 6);
            comboInput.Width = panelContainer.Width - 20;
            comboInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboInput.BackColor = AppColors.Surface;
            comboInput.DropDownStyle = ComboBoxStyle.DropDownList;
            
            // Focus Events
            comboInput.Enter += (s, e) => { _isFocused = true; panelContainer.Invalidate(); };
            comboInput.Leave += (s, e) => { _isFocused = false; panelContainer.Invalidate(); };
            
            comboInput.Visible = false;
            panelContainer.Controls.Add(comboInput);

            // Error Label
            labelError = new Label();
            labelError.AutoSize = true;
            labelError.Font = AppFonts.Caption;
            labelError.ForeColor = AppColors.Error;
            labelError.Location = new Point(AppDimens.SpacingXS, 65);
            labelError.Visible = false;
            this.Controls.Add(labelError);
        }

        private void UpdateInputVisibility()
        {
            if (_inputType == InputTypeEnum.Password)
            {
                textInput.Visible = true;
                textInput.UseSystemPasswordChar = true;
                comboInput.Visible = false;
            }
            else if (_inputType == InputTypeEnum.Text)
            {
                textInput.Visible = true;
                textInput.UseSystemPasswordChar = false;
                comboInput.Visible = false;
            }
            else // Dropdown
            {
                textInput.Visible = false;
                comboInput.Visible = true;
            }
        }

        private void PanelContainer_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            Rectangle rect = panelContainer.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            Color borderColor;
            if (labelError.Visible)
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
