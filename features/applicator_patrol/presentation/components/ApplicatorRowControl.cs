using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.applicator_patrol.presentation.components
{
    /// <summary>
    /// UserControl untuk satu baris aplikator.
    /// Layout: [✓] [Nomor Aplikator 240px] [○OK  ○NG  ○N/A]
    /// Total width: 620px (disesuaikan dengan pnlApplicatorList)
    /// </summary>
    public class ApplicatorRowControl : UserControl
    {
        private CheckBox _chkActive;
        private Label _lblApplicatorCode;
        private RadioButton _rbOk;
        private RadioButton _rbNg;
        private RadioButton _rbNa;
        private Panel _pnlRadioGroup;

        public string ApplicatorCode
        {
            get => _lblApplicatorCode.Text;
            set => _lblApplicatorCode.Text = value;
        }

        public bool IsActive
        {
            get => _chkActive.Checked;
            set { _chkActive.Checked = value; UpdateRadioState(); }
        }

        public string Judgment
        {
            get
            {
                if (_rbOk.Checked) return "OK";
                if (_rbNg.Checked) return "NG";
                return "NA";
            }
            set
            {
                if (value == "NG") _rbNg.Checked = true;
                else if (value == "NA") _rbNa.Checked = true;
                else _rbOk.Checked = true;
            }
        }

        public ApplicatorRowControl()
        {
            this.Height = 40;
            this.BackColor = AppColors.Background;

            _chkActive = new CheckBox
            {
                Width = 20,
                Height = 20,
                Location = new Point(6, 10),
                Checked = true
            };
            _chkActive.CheckedChanged += (s, e) => UpdateRadioState();

            _lblApplicatorCode = new Label
            {
                Location = new Point(32, 6),
                Width = 240,
                Height = 28,
                Font = new Font(AppFonts.FontFamily, 10.5f, FontStyle.Regular),
                ForeColor = AppColors.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Radio group panel — mutual exclusive per row
            _pnlRadioGroup = new Panel
            {
                Location = new Point(278, 3),
                Width = 320,
                Height = 34,
                BackColor = AppColors.Background
            };

            _rbOk = new RadioButton
            {
                Text = "OK",
                Location = new Point(4, 7),
                AutoSize = true,
                Font = new Font(AppFonts.FontFamily, 10.5f, FontStyle.Bold),
                ForeColor = AppColors.Success,
                Checked = true
            };
            _rbNg = new RadioButton
            {
                Text = "NG",
                Location = new Point(100, 7),
                AutoSize = true,
                Font = new Font(AppFonts.FontFamily, 10.5f, FontStyle.Bold),
                ForeColor = AppColors.Danger
            };
            _rbNa = new RadioButton
            {
                Text = "N/A",
                Location = new Point(196, 7),
                AutoSize = true,
                Font = new Font(AppFonts.FontFamily, 10.5f, FontStyle.Regular),
                ForeColor = AppColors.TextSecondary
            };

            _rbNg.CheckedChanged += (s, e) => HighlightRow();

            _pnlRadioGroup.Controls.AddRange(new Control[] { _rbOk, _rbNg, _rbNa });
            this.Controls.AddRange(new Control[] { _chkActive, _lblApplicatorCode, _pnlRadioGroup });

            Panel sep = new Panel { BackColor = AppColors.Border, Dock = DockStyle.Bottom, Height = 1 };
            this.Controls.Add(sep);
        }

        private void UpdateRadioState()
        {
            bool active = _chkActive.Checked;
            _rbOk.Enabled = active;
            _rbNg.Enabled = active;
            _rbNa.Enabled = active;
            _lblApplicatorCode.ForeColor = active ? AppColors.TextPrimary : AppColors.TextDisabled;
            if (!active) _rbNa.Checked = true;
            HighlightRow();
        }

        private void HighlightRow()
        {
            Color bg = (_rbNg.Checked && _chkActive.Checked)
                ? Color.FromArgb(255, 235, 235)
                : AppColors.Background;
            this.BackColor = bg;
            _pnlRadioGroup.BackColor = bg;
        }
    }
}
