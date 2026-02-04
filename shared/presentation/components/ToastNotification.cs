using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    /// <summary>
    /// A floating toast notification that slides in from the bottom-right.
    /// Auto-dismisses after the specified duration.
    /// </summary>
    public class ToastNotification : Form
    {
        private readonly Timer _autoCloseTimer;
        private readonly Timer _animationTimer;
        private readonly int _targetX;
        private readonly int _targetY;
        private int _currentStep;
        private const int ANIMATION_STEPS = 10;

        public enum ToastType { Success, Warning, Error, Info }

        private ToastNotification(string message, ToastType type, int durationMs = 3000)
        {
            // Form setup
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            Size = new Size(320, 70);
            BackColor = GetBackgroundColor(type);

            // Position off-screen initially (will animate in)
            var screen = Screen.PrimaryScreen.WorkingArea;
            _targetX = screen.Right - Width - 20;
            _targetY = screen.Bottom - Height - 20;
            Location = new Point(_targetX, screen.Bottom); // Start below screen

            // Add rounded corners
            Region = CreateRoundedRegion(Width, Height, 8);

            // Icon
            var icon = new Label
            {
                Text = GetIcon(type),
                Font = new Font("Segoe UI", 18f),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(12, 15)
            };
            Controls.Add(icon);

            // Message
            var label = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(260, 50),
                Location = new Point(50, 10),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(label);

            // Auto-close timer
            _autoCloseTimer = new Timer { Interval = durationMs };
            _autoCloseTimer.Tick += (s, e) => SlideOut();

            // Animation timer
            _animationTimer = new Timer { Interval = 20 };
            _animationTimer.Tick += AnimateIn;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _currentStep = 0;
            _animationTimer.Start();
        }

        private void AnimateIn(object sender, EventArgs e)
        {
            _currentStep++;
            if (_currentStep >= ANIMATION_STEPS)
            {
                _animationTimer.Stop();
                Location = new Point(_targetX, _targetY);
                _autoCloseTimer.Start();
                return;
            }

            // Easing function for smooth slide
            double progress = (double)_currentStep / ANIMATION_STEPS;
            double eased = 1 - Math.Pow(1 - progress, 3); // Ease out cubic
            
            int startY = Screen.PrimaryScreen.WorkingArea.Bottom;
            int currentY = startY - (int)((startY - _targetY) * eased);
            Location = new Point(_targetX, currentY);
        }

        private void SlideOut()
        {
            _autoCloseTimer.Stop();
            _animationTimer.Tick -= AnimateIn;
            _animationTimer.Tick += AnimateOut;
            _currentStep = 0;
            _animationTimer.Start();
        }

        private void AnimateOut(object sender, EventArgs e)
        {
            _currentStep++;
            if (_currentStep >= ANIMATION_STEPS / 2)
            {
                _animationTimer.Stop();
                Close();
                return;
            }

            Opacity = 1.0 - ((double)_currentStep / (ANIMATION_STEPS / 2));
        }

        private Color GetBackgroundColor(ToastType type)
        {
            switch (type)
            {
                case ToastType.Success: return AppColors.Success;
                case ToastType.Warning: return AppColors.Warning;
                case ToastType.Error: return AppColors.Error;
                case ToastType.Info: return AppColors.Info;
                default: return AppColors.Surface;
            }
        }

        private string GetIcon(ToastType type)
        {
            switch (type)
            {
                case ToastType.Success: return "✓";
                case ToastType.Warning: return "⚠";
                case ToastType.Error: return "✕";
                case ToastType.Info: return "ℹ";
                default: return "●";
            }
        }

        private Region CreateRoundedRegion(int width, int height, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
            path.AddArc(width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
            path.AddArc(width - radius * 2, height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(0, height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return new Region(path);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            SlideOut(); // Click to dismiss
        }

        // ─────────────────────────────────────────────────────────────────────
        // Static Factory Methods
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Shows a success toast notification.
        /// </summary>
        public static void ShowSuccess(string message, int durationMs = 3000)
        {
            Show(message, ToastType.Success, durationMs);
        }

        /// <summary>
        /// Shows a warning toast notification.
        /// </summary>
        public static void ShowWarning(string message, int durationMs = 4000)
        {
            Show(message, ToastType.Warning, durationMs);
        }

        /// <summary>
        /// Shows an error toast notification.
        /// </summary>
        public static void ShowError(string message, int durationMs = 5000)
        {
            Show(message, ToastType.Error, durationMs);
        }

        /// <summary>
        /// Shows an info toast notification.
        /// </summary>
        public static void ShowInfo(string message, int durationMs = 3000)
        {
            Show(message, ToastType.Info, durationMs);
        }

        private static void Show(string message, ToastType type, int durationMs)
        {
            // Must run on UI thread
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.BeginInvoke(new Action(() => ShowToast(message, type, durationMs)));
                }
                else
                {
                    ShowToast(message, type, durationMs);
                }
            }
        }

        private static void ShowToast(string message, ToastType type, int durationMs)
        {
            var toast = new ToastNotification(message, type, durationMs);
            toast.Show();
        }
    }
}
