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
        private Panel panelContainer;

        private InputTypeEnum _inputType = InputTypeEnum.Text;
        private bool _isRequired = false;
        private bool _isFocused = false;
        
        // For custom autocomplete
        private List<string> _originalItems;
        private bool _isFiltering = false;

        // Public Event for Dropdown Opened (Real-time data loading)
        public event EventHandler DropdownOpened;

        public AppInput()
        {
            InitializeCustomComponents();
            this.Padding = new Padding(AppDimens.SpacingXS);
            this.Size = new Size(300, 85); 
            this.BackColor = Color.Transparent;
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
                }
                else
                {
                    comboInput.DropDownStyle = ComboBoxStyle.DropDownList;
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
                    textInput.Height = 20;
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
                try
                {
                    if (_inputType == InputTypeEnum.Dropdown)
                        return comboInput.Text ?? "";
                    return textInput.Text ?? "";
                }
                catch (ArgumentOutOfRangeException)
                {
                    // ComboBox in invalid state, return empty
                    return "";
                }
                catch
                {
                    return "";
                }
            }
            set
            {
                try
                {
                    if (_inputType == InputTypeEnum.Dropdown)
                    {
                        _isFiltering = true; // Prevent TextChanged during programmatic set
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
                catch
                {
                    _isFiltering = false;
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
            {
                _originalItems = new List<string>(items);
                comboInput.Items.AddRange(items);
            } else 
            {
                _originalItems = new List<string>();
            }
        }

        public bool ValidateInput()
        {
            labelError.Visible = false;
            
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
            textInput.Location = new Point(10, 8);
            textInput.Width = panelContainer.Width - 20;
            textInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textInput.BackColor = AppColors.Surface;
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
            comboInput.Enter += (s, e) => { _isFocused = true; panelContainer.Invalidate(); };
            comboInput.Leave += (s, e) => { _isFocused = false; panelContainer.Invalidate(); };
            comboInput.TextChanged += ComboInput_TextChanged; // Subscribe to event
            comboInput.DropDown += (s, e) => DropdownOpened?.Invoke(this, EventArgs.Empty); // Trigger event
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

        private void ComboInput_TextChanged(object sender, EventArgs e)
        {
            if (_isFiltering || !AllowCustomText) return;

            _isFiltering = true;

            try
            {
                string typedText = comboInput.Text ?? "";
                int cursorPos = 0;
                
                try
                {
                    cursorPos = comboInput.SelectionStart;
                }
                catch
                {
                    cursorPos = typedText.Length;
                }

                // Safely clamp cursor position
                int selectionStart = Math.Max(0, Math.Min(cursorPos, typedText.Length));

                comboInput.BeginUpdate();
                
                try
                {
                    comboInput.Items.Clear();

                    if (string.IsNullOrEmpty(typedText) || _originalItems == null)
                    {
                        if (_originalItems != null)
                            comboInput.Items.AddRange(_originalItems.ToArray());
                    }
                    else
                    {
                        var filteredItems = _originalItems
                            .Where(item => item.IndexOf(typedText, StringComparison.OrdinalIgnoreCase) >= 0)
                            .ToArray();

                        if (filteredItems.Length > 0)
                        {
                            comboInput.Items.AddRange(filteredItems);
                        }
                    }
                }
                finally
                {
                    comboInput.EndUpdate();
                }

                // Restore text and cursor position AFTER EndUpdate
                try
                {
                    comboInput.Text = typedText;
                    
                    // Bounds check to prevent ArgumentOutOfRangeException
                    int safePosition = Math.Max(0, Math.Min(selectionStart, comboInput.Text.Length));
                    comboInput.SelectionStart = safePosition;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // If position is still invalid, just set to end
                    try { comboInput.SelectionStart = comboInput.Text.Length; } catch { }
                }

                // Show dropdown only if we have items and control is focused
                try
                {
                    if (comboInput.Items.Count > 0 && this.ContainsFocus && !string.IsNullOrEmpty(typedText))
                    {
                        comboInput.DroppedDown = true;
                    }
                }
                catch { }

                Cursor.Current = Cursors.Default;
            }
            catch (ArgumentOutOfRangeException)
            {
                // Handle Ctrl+Backspace and other edge cases silently
                try { comboInput.SelectionStart = 0; } catch { }
            }
            catch (Exception)
            {
                // Silently handle any remaining edge cases
            }
            finally
            {
                _isFiltering = false;
            }
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