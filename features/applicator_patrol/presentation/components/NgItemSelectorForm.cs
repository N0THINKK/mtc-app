using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.applicator_patrol.presentation.components
{
    /// <summary>
    /// Popup kecil yang muncul saat operator memilih NG pada aplikator.
    /// Menampilkan 7 item checklist dan meminta operator memilih item mana yang NG.
    /// Tidak bisa OK/tutup tanpa memilih minimal 1 item.
    /// </summary>
    public class NgItemSelectorForm : Form
    {
        public static readonly string[] ITEM_LABELS = {
            "1. Crimper Anvil / Anvil holder / Supporting stopper",
            "2. Posisi Ram (I-marks harus lurus)",
            "3. Kondisi pot oil PA5 (range Max–Min)",
            "4. Wire stopper / Safety cover / Strip terminal EASY",
            "5. Crimper front/rear / Anvil punggung / Shear blade",
            "6. Crimper Anvil (Masuk standard – Micrometer)",
            "7. Validasi Appl (Sesuai schedule Prev)",
        };

        private readonly string _applicatorCode;
        private CheckBox[] _checkboxes;
        private AppButton btnOk, btnBatal;
        private Label lblError;

        /// <summary>String nomor item NG, dipisah koma. Contoh "1,3". Null jika dibatalkan.</summary>
        public string SelectedNgItems { get; private set; }

        public NgItemSelectorForm(string applicatorCode, string existingNgItems = null)
        {
            _applicatorCode = applicatorCode;
            InitializeUI(existingNgItems);
        }

        private void InitializeUI(string existingNgItems)
        {
            this.Text = "Pilih Item yang NG";
            this.ClientSize = new Size(480, 340);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Header
            var lblTitle = new Label
            {
                Text = $"Aplikator  {_applicatorCode}  — Item yang NG:",
                Font = new Font(AppFonts.FontFamily, 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 40, 40),
                AutoSize = false, Bounds = new Rectangle(14, 12, 452, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblTitle);

            this.Controls.Add(new Panel { Bounds = new Rectangle(0, 44, 480, 1), BackColor = AppColors.Border });

            // Checkboxes untuk tiap item
            _checkboxes = new CheckBox[ITEM_LABELS.Length];
            var preSelected = new HashSet<string>(
                (existingNgItems ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

            for (int i = 0; i < ITEM_LABELS.Length; i++)
            {
                _checkboxes[i] = new CheckBox
                {
                    Text = ITEM_LABELS[i],
                    Font = new Font(AppFonts.FontFamily, 10, FontStyle.Regular),
                    ForeColor = AppColors.TextPrimary,
                    AutoSize = false,
                    Bounds = new Rectangle(18, 52 + i * 30, 444, 26),
                    Checked = preSelected.Contains((i + 1).ToString())
                };
                this.Controls.Add(_checkboxes[i]);
            }

            // Error label
            lblError = new Label
            {
                Text = "⚠ Pilih minimal satu item yang NG!",
                Font = new Font(AppFonts.FontFamily, 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(200, 30, 30),
                AutoSize = true,
                Location = new Point(18, 270),
                Visible = false
            };
            this.Controls.Add(lblError);

            // Buttons
            btnBatal = new AppButton
            {
                Text = "Batal (Kembali ke OK)",
                Type = AppButton.ButtonType.Secondary,
                Bounds = new Rectangle(14, 296, 170, 34)
            };
            btnBatal.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnBatal);

            btnOk = new AppButton
            {
                Text = "Konfirmasi NG ✓",
                Type = AppButton.ButtonType.Primary,
                Bounds = new Rectangle(310, 296, 155, 34)
            };
            btnOk.Click += BtnOk_Click;
            this.Controls.Add(btnOk);

            this.AcceptButton = btnOk;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            var selected = _checkboxes
                .Select((cb, idx) => cb.Checked ? (idx + 1).ToString() : null)
                .Where(v => v != null)
                .ToList();

            if (selected.Count == 0)
            {
                lblError.Visible = true;
                return;
            }

            lblError.Visible = false;
            SelectedNgItems = string.Join(",", selected);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
