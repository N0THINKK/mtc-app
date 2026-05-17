using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.applicator_patrol.presentation.components
{
    /// <summary>
    /// UserControl untuk satu baris aplikator.
    /// Layout: [✓] [Nomor Aplikator 240px] [○OK  ○NG  ○N/A]  [label NG items kecil]
    /// Ketika NG dipilih → popup NgItemSelectorForm untuk konfirmasi item yang NG.
    /// Jika popup dibatalkan → kembali ke OK.
    /// </summary>
    public class ApplicatorRowControl : UserControl
    {
        private CheckBox _chkActive;
        private Label _lblApplicatorCode;
        private RadioButton _rbOk;
        private RadioButton _rbNg;
        private RadioButton _rbNa;
        private Panel _pnlRadioGroup;
        private Label _lblNgItems;

        // State: ng item numbers yang dipilih, e.g. "1,3"
        private string _ngItems = null;
        // Flag agar tidak trigger popup saat restore
        private bool _suppressNgPopup = false;

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
                _suppressNgPopup = true;
                if (value == "NG") _rbNg.Checked = true;
                else if (value == "NA") _rbNa.Checked = true;
                else _rbOk.Checked = true;
                _suppressNgPopup = false;
                HighlightRow();
            }
        }

        /// <summary>Nomor item NG dipisah koma (e.g. "1,3"). Null jika OK atau NA.</summary>
        public string NgItems
        {
            get => _ngItems;
            set
            {
                _ngItems = value;
                UpdateNgLabel();
            }
        }

        public ApplicatorRowControl()
        {
            this.Height = 40;
            this.BackColor = AppColors.Background;

            _chkActive = new CheckBox
            {
                Width = 20, Height = 20,
                Location = new Point(6, 10),
                Checked = true
            };
            _chkActive.CheckedChanged += (s, e) => UpdateRadioState();

            _lblApplicatorCode = new Label
            {
                Location = new Point(32, 6),
                Width = 235,
                Height = 28,
                Font = new Font(AppFonts.FontFamily, 10.5f, FontStyle.Regular),
                ForeColor = AppColors.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _pnlRadioGroup = new Panel
            {
                Location = new Point(274, 3),
                Width = 290,
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
                Location = new Point(90, 7),
                AutoSize = true,
                Font = new Font(AppFonts.FontFamily, 10.5f, FontStyle.Bold),
                ForeColor = AppColors.Danger
            };
            _rbNa = new RadioButton
            {
                Text = "N/A",
                Location = new Point(176, 7),
                AutoSize = true,
                Font = new Font(AppFonts.FontFamily, 10.5f, FontStyle.Regular),
                ForeColor = AppColors.TextSecondary
            };

            _rbNg.CheckedChanged += RbNg_CheckedChanged;

            _pnlRadioGroup.Controls.AddRange(new Control[] { _rbOk, _rbNg, _rbNa });

            // Label kecil untuk menampilkan item NG, contoh "▶ 1,3"
            _lblNgItems = new Label
            {
                Location = new Point(570, 6),
                Width = 160,
                Height = 28,
                Font = new Font(AppFonts.FontFamily, 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(200, 30, 30),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };

            this.Controls.AddRange(new Control[] { _chkActive, _lblApplicatorCode, _pnlRadioGroup, _lblNgItems });

            var sep = new Panel { BackColor = AppColors.Border, Dock = DockStyle.Bottom, Height = 1 };
            this.Controls.Add(sep);
        }

        private void RbNg_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressNgPopup) return;
            if (!_rbNg.Checked) { HighlightRow(); return; }

            // Buka popup item selector
            var popup = new NgItemSelectorForm(_lblApplicatorCode.Text, _ngItems);
            var result = popup.ShowDialog(this.FindForm());

            if (result == DialogResult.OK)
            {
                _ngItems = popup.SelectedNgItems;
                UpdateNgLabel();
                HighlightRow();
            }
            else
            {
                // Batalkan → kembali ke OK
                _suppressNgPopup = true;
                _rbOk.Checked = true;
                _suppressNgPopup = false;
                _ngItems = null;
                UpdateNgLabel();
                HighlightRow();
            }
        }

        private void UpdateNgLabel()
        {
            if (!string.IsNullOrEmpty(_ngItems) && _rbNg.Checked)
            {
                _lblNgItems.Text = $"▶ item {_ngItems}";
                _lblNgItems.Visible = true;
            }
            else
            {
                _lblNgItems.Visible = false;
            }
        }

        private void UpdateRadioState()
        {
            bool active = _chkActive.Checked;
            _rbOk.Enabled = active;
            _rbNg.Enabled = active;
            _rbNa.Enabled = active;
            _lblApplicatorCode.ForeColor = active ? AppColors.TextPrimary : AppColors.TextDisabled;
            if (!active)
            {
                _suppressNgPopup = true;
                _rbNa.Checked = true;
                _suppressNgPopup = false;
                _ngItems = null;
                UpdateNgLabel();
            }
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
