using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.shared.presentation.components
{
    public class AutoClosingMessageBox : Form
    {
        private System.Windows.Forms.Timer _timeoutTimer;
        private AppLabel _lblMessage;
        private AppButton _btnOk;
        private int _countdown;

        public AutoClosingMessageBox(string text, string caption, int timeoutMs)
        {
            InitializeComponent(text, caption);
            
            _countdown = timeoutMs / 1000;
            _btnOk.Text = $"OK ({_countdown})";

            _timeoutTimer = new System.Windows.Forms.Timer();
            _timeoutTimer.Interval = 1000;
            _timeoutTimer.Tick += (s, e) =>
            {
                _countdown--;
                if (_countdown <= 0)
                {
                    this.Close();
                }
                else
                {
                    _btnOk.Text = $"OK ({_countdown})";
                }
            };
            _timeoutTimer.Start();
        }

        private void InitializeComponent(string text, string caption)
        {
            this.Text = caption;
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.TopMost = true;

            // Message Label
            _lblMessage = new AppLabel
            {
                Text = text,
                Font = new Font("Segoe UI", 11F),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };
            this.Controls.Add(_lblMessage);

            // Button Panel
            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(100, 10, 100, 10) };
            _btnOk = new AppButton
            {
                Text = "OK",
                Dock = DockStyle.Fill,
                Type = AppButton.ButtonType.Primary
            };
            _btnOk.Click += (s, e) => this.Close();
            pnlBottom.Controls.Add(_btnOk);
            this.Controls.Add(pnlBottom);
        }

        public static void Show(string text, string caption, int timeoutMs = 5000)
        {
            using (var form = new AutoClosingMessageBox(text, caption, timeoutMs))
            {
                form.ShowDialog();
            }
        }
    }
}
