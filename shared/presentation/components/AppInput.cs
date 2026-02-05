using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
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

        private InputTypeEnum _inputType = InputTypeEnum.Text;
        private bool _isRequired = false;
        private bool _isFocused = false;
        
        // Custom AutoComplete & Cache
        private List<string> _originalItems;
        private bool _isFiltering = false;
        private string _cachedText = ""; 

        public event EventHandler DropdownOpened;

        // Visual Constants
        private int _inputAreaTop => labelTitle.Height + 6; // Dynamic: Use Height not Bottom to handle Font scaling
        private int _inputHeight => Math.Max(AppDimens.ControlHeight, textInput.Height + 16); // Dynamic: Scale with TextBox

        public AppInput()
        {
            InitializeCustomComponents();
            
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            this.BackColor = Color.Transparent; // Important for rounded corners
            
            // Calculate Initial Size
            this.Size = new Size(300, 90); 
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (labelTitle != null) labelTitle.Font = AppFonts.Subtitle;
            if (textInput != null) textInput.Font = AppFonts.Body;
            if (comboInput != null) comboInput.Font = AppFonts.Body;
            RepositionControls();
        }

        [Category("App Properties")]
        public bool AllowCustomText
        {
            get { return comboInput.DropDownStyle == ComboBoxStyle.DropDown; }
            set
            {
                if (value)
                    comboInput.DropDownStyle = ComboBoxStyle.DropDown;
                else
                    comboInput.DropDownStyle = ComboBoxStyle.DropDownList;
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
                    this.Height = 160;
                    textInput.Height = 84;
                    textInput.ScrollBars = ScrollBars.Vertical;
                }
                else
                {
                    this.Height = 90;
                    textInput.Height = 24; 
                    textInput.ScrollBars = ScrollBars.None;
                }
                RepositionControls();
                this.Invalidate();
            }
        }

        [Category("App Properties")]
        public string LabelText
        {
            get { return labelTitle.Text; }
            set 
            { 
                labelTitle.Text = value; 
                RepositionControls(); // Label height might change? unlikely for single line
            }
        }

        [Category("App Properties")]
        public string InputValue
        {
            get
            {
                if (_inputType == InputTypeEnum.Dropdown)
                {
                    try
                    {
                        return comboInput.Text ?? _cachedText;
                    }
                    catch { return _cachedText; }
                }
                return textInput.Text ?? "";
            }
            set
            {
                try
                {
                    _cachedText = value ?? "";
                    
                    if (_inputType == InputTypeEnum.Dropdown)
                    {
                        _isFiltering = true;
                        if (comboInput.Items.Contains(value))
                            comboInput.SelectedItem = value;
                        else if (AllowCustomText)
                            comboInput.Text = value ?? "";
                        _isFiltering = false;
                    }
                    else
                    {
                        textInput.Text = value ?? "";
                    }
                }
                catch { _isFiltering = false; }
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
        public CharacterCasing CharacterCasing
        {
            get { return textInput.CharacterCasing; }
            set { textInput.CharacterCasing = value; }
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
            {
                _originalItems = new List<string>(items);
                comboInput.Items.AddRange(items);
            } 
            else 
            {
                _originalItems = new List<string>();
            }
        }

        public bool ValidateInput()
        {
            labelError.Visible = false;
            string val = InputValue; 
            
            if (_isValidRequired(val))
            {
               this.Invalidate(); // Refresh border color
               return true;
            }
            
            SetError($"{LabelText} wajib diisi.");
            return false;
        }

        private bool _isValidRequired(string val)
        {
            if (!_isRequired) return true;
            return !string.IsNullOrWhiteSpace(val);
        }

        public void SetError(string message)
        {
            labelError.Text = message;
            labelError.Visible = true;
            this.Invalidate();
        }

        private void InitializeCustomComponents()
        {
            // Title Label
            labelTitle = new Label();
            labelTitle.AutoSize = true;
            labelTitle.Font = AppFonts.Subtitle;
            labelTitle.ForeColor = AppColors.TextPrimary;
            labelTitle.Location = new Point(0, 0);
            labelTitle.SizeChanged += (s, e) => { RepositionControls(); this.Invalidate(); };
            this.Controls.Add(labelTitle);

            // TextBox
            textInput = new TextBox();
            textInput.BorderStyle = BorderStyle.None;
            textInput.Font = AppFonts.Body;
            textInput.BackColor = AppColors.Surface; 
            textInput.Enter += (s, e) => { _isFocused = true; this.Invalidate(); };
            textInput.Leave += (s, e) => { _isFocused = false; this.Invalidate(); };
            textInput.SizeChanged += (s, e) => { RepositionControls(); this.Invalidate(); };
            this.Controls.Add(textInput);

            // ComboBox
            comboInput = new ComboBox();
            comboInput.FlatStyle = FlatStyle.Flat;
            comboInput.Font = AppFonts.Body;
            comboInput.BackColor = AppColors.Surface;
            comboInput.DropDownStyle = ComboBoxStyle.DropDownList;
            comboInput.Enter += (s, e) => { _isFocused = true; this.Invalidate(); };
            comboInput.Leave += (s, e) => { _isFocused = false; this.Invalidate(); };
            
            comboInput.TextChanged += (s, e) => { try { _cachedText = comboInput.Text; } catch { } };
            // Simple filtering logic embedded
            comboInput.TextChanged += ComboInput_TextChanged; 
            comboInput.DropDown += (s, e) => DropdownOpened?.Invoke(this, EventArgs.Empty);
            comboInput.Visible = false;
            comboInput.SizeChanged += (s, e) => { RepositionControls(); this.Invalidate(); };
            this.Controls.Add(comboInput);

            // Error Label
            labelError = new Label();
            labelError.AutoSize = true;
            labelError.Font = AppFonts.Caption;
            labelError.ForeColor = AppColors.Error;
            labelError.Visible = false;
            this.Controls.Add(labelError);

            RepositionControls();
        }

        private void RepositionControls()
        {
            // Input Box Rect (Visual Only) is calculated in OnPaint:
            // Y = _inputAreaTop, Height = _inputHeight
            if (textInput == null || comboInput == null) return;

            // Center Controls vertically inside the _inputHeight area
            int inputMiddleY = _inputAreaTop + (_inputHeight / 2);
            
            // TextBox
            // Height is usually 20-23 (or scaled). Center it.
            int textY = inputMiddleY - (textInput.Height / 2);
            textInput.Location = new Point(10, textY); 
            textInput.Width = this.Width - 20;

            // ComboBox
            int comboY = inputMiddleY - (comboInput.Height / 2);
            comboInput.Location = new Point(10, comboY);
            comboInput.Width = this.Width - 20;

            // Error Label
            labelError.Location = new Point(0, _inputAreaTop + _inputHeight + 4);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RepositionControls();
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Define the visual Input Area
            Rectangle inputRect = new Rectangle(0, _inputAreaTop, this.Width - 1, _inputHeight);
            
            // Background
            using (var brush = new SolidBrush(AppColors.Surface))
            {
                e.Graphics.FillPath(brush, GetRoundedPath(inputRect, AppDimens.CornerRadius));
            }

            // Border
            Color borderColor = _isFocused ? AppColors.BorderFocus : AppColors.Border;
            if (labelError.Visible) borderColor = AppColors.Error;

            int penWidth = _isFocused ? 2 : 1;
            
            // Inset DrawRect for Border to prevent clipping
            if (_isFocused) inputRect.Inflate(-1, -1);

            using (var pen = new Pen(borderColor, penWidth))
            {
                e.Graphics.DrawPath(pen, GetRoundedPath(inputRect, AppDimens.CornerRadius));
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

        // ... ComboBox Filtering Logic (Kept mostly same but cleaner) ...
        private void ComboInput_TextChanged(object sender, EventArgs e)
        {
            if (_isFiltering || !AllowCustomText) return;
            _isFiltering = true;
            try
            {
                string typedText = comboInput.Text ?? "";
                _cachedText = typedText;
                
                // Perform simple filter if original items exist
                if (_originalItems != null && _originalItems.Count > 0)
                {
                    var filtered = _originalItems.Where(x => x.IndexOf(typedText, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                    
                    comboInput.BeginUpdate();
                    comboInput.Items.Clear();
                    if (string.IsNullOrEmpty(typedText))
                        comboInput.Items.AddRange(_originalItems.ToArray());
                    else if (filtered.Length > 0)
                        comboInput.Items.AddRange(filtered);
                    comboInput.EndUpdate();

                    // Restore text and cursor
                    comboInput.Text = typedText;
                    comboInput.SelectionStart = typedText.Length;
                    
                    if (comboInput.Items.Count > 0 && this.ContainsFocus && !string.IsNullOrEmpty(typedText))
                         comboInput.DroppedDown = true;
                }
            }
            catch { }
            finally { _isFiltering = false; Cursor.Current = Cursors.Default; }
        }

        private void UpdateInputVisibility()
        {
            if (_inputType == InputTypeEnum.Text || _inputType == InputTypeEnum.Password)
            {
                textInput.Visible = true;
                comboInput.Visible = false;
                textInput.UseSystemPasswordChar = (_inputType == InputTypeEnum.Password);
            }
            else
            {
                textInput.Visible = false;
                comboInput.Visible = true;
            }
        }
    }
}