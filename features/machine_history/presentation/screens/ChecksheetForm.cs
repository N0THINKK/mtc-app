using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapper;
using System.Collections.Generic;
using mtc_app.shared.data.session;
using mtc_app.shared.data.utils;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.features.machine_history.data.repositories;
using mtc_app.features.machine_history.data.dtos;

namespace mtc_app.features.machine_history.presentation.screens
{
    public class ChecksheetForm : AppBaseForm
    {
        private FlowLayoutPanel pnlQuestions;
        private AppButton btnSave;
        private Button btnLihatNg; // [BARU] Deklarasi di tingkat class agar bisa diakses event Resize
        private AppButton btnHistory; // Tombol riwayat checksheet
        private Label lblMachineInfo;

        private readonly bool _isTeknisiMode;
        private int _currentMachineId;
        private int _currentTemplateId;
        private List<ChecksheetItemControl> _itemControls = new List<ChecksheetItemControl>();

        public ChecksheetForm(bool isTeknisiMode = false)
        {
            _isTeknisiMode = isTeknisiMode;
            InitializeUI();
            LoadChecksheetData();
        }

        private void InitializeUI()
        {
            this.Text = _isTeknisiMode ? "Patroli Checksheet - TEKNISI" : "Patroli Checksheet - OPERATOR";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = AppColors.Background;

            // Sembunyikan border agar terlihat seperti Kiosk (opsional)
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // --- HEADER ---
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = AppColors.CardBackground };
            Label lblTitle = new Label { Text = this.Text, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = AppColors.TextPrimary, AutoSize = true, Location = new Point(20, 15) };
            lblMachineInfo = new Label { Text = "Loading...", Font = new Font("Segoe UI", 11F), ForeColor = AppColors.TextSecondary, AutoSize = true, Location = new Point(20, 45) };

            // --- UI IDENTITAS PELAKSANA (DINAMIS) ---
            string pelaksanaLabel = _isTeknisiMode ? "Teknisi" : "NIK Operator";
            string pelaksanaValue = UserSession.CurrentUser?.Username ?? "-";

            if (_isTeknisiMode)
            {
                string fullName = UserSession.CurrentUser?.FullName;
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    var words = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 1)
                    {
                        pelaksanaValue = words[0];
                    }
                    else if (words.Length >= 2)
                    {
                        pelaksanaValue = $"{words[0]} {words[words.Length - 1]}";
                    }

                    // Convert to Title Case (e.g. RIZAL FIRMANSYAH -> Rizal Firmansyah)
                    var textInfo = new System.Globalization.CultureInfo("id-ID", false).TextInfo;
                    pelaksanaValue = textInfo.ToTitleCase(pelaksanaValue.ToLower());
                }
            }

            Label lblPelaksanaInfo = new Label
            {
                Text = $"{pelaksanaLabel}: {pelaksanaValue}",
                Font = new Font("Segoe UI", 11F),
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Location = new Point(20, 70)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblMachineInfo);
            pnlHeader.Controls.Add(lblPelaksanaInfo);

            // --- AREA PERTANYAAN (SCROLLABLE) ---
            pnlQuestions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(20, 20, 20, 20),
                AutoScrollMargin = new Size(0, 100) // Margin ekstra di bawah untuk memastikan item terakhir bisa ter-scroll penuh
            };

            // --- BOTTOM PANEL ---
            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = AppColors.CardBackground };

            // Tombol Simpan
            btnSave = new AppButton { Text = "Simpan Hasil Patroli", Width = 250, Height = 40, Type = AppButton.ButtonType.Primary, Location = new Point(this.Width - 280, 15), Cursor = Cursors.Hand };
            btnSave.Click += BtnSave_Click;

            // Tombol Batal
            AppButton btnCancel = new AppButton { Text = "Batal", Width = 100, Height = 40, Type = AppButton.ButtonType.Secondary, Location = new Point(20, 15), Cursor = Cursors.Hand };
            btnCancel.Click += (s, e) => this.Close();

            pnlBottom.Controls.Add(btnSave);
            pnlBottom.Controls.Add(btnCancel);

            // =========================================================================
            // [BARU] LANGKAH B: TOMBOL DAFTAR NG DI SEBELAH KIRI TOMBOL SIMPAN
            // =========================================================================
            if (_isTeknisiMode)
            {
                btnLihatNg = new Button
                {
                    Text = "Daftar Mesin NOT OK",
                    Size = new Size(200, 40),
                    BackColor = Color.DarkOrange,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(btnSave.Left - 215, 15) // Diberi jarak 15px dari tombol simpan
                };
                btnLihatNg.FlatAppearance.BorderSize = 0;

                btnLihatNg.Click += (sender, e) =>
                {
                    this.Hide();
                    using (var popup = new PopupNgListForm(_currentMachineId))
                    {
                        popup.ShowDialog(this);
                    }
                    this.Show();
                };

                pnlBottom.Controls.Add(btnLihatNg);
            }

            // Tombol History Checksheet
            btnHistory = new AppButton
            {
                Text = "History",
                Width = 100,
                Height = 40,
                Type = AppButton.ButtonType.Secondary,
                Cursor = Cursors.Hand
            };

            // Atur posisi awal (nanti di-recalculate di event Resize)
            if (btnLihatNg != null)
                btnHistory.Location = new Point(btnLihatNg.Left - 115, 15);
            else
                btnHistory.Location = new Point(btnSave.Left - 115, 15);

            btnHistory.Click += (sender, e) =>
            {
                if (_currentMachineId > 0 && _currentTemplateId > 0)
                {
                    this.Hide();
                    string roleTargetLocal = _isTeknisiMode ? "Teknisi" : "Operator";
                    using (var historyForm = new ChecksheetHistoryForm(_currentMachineId, _currentTemplateId, roleTargetLocal))
                    {
                        historyForm.ShowDialog(this);
                    }
                    this.Show();
                }
            };
            pnlBottom.Controls.Add(btnHistory);
            // =========================================================================

            this.Controls.Add(pnlQuestions);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlBottom);

            pnlHeader.SendToBack();
            pnlBottom.SendToBack();
            pnlQuestions.BringToFront();

            // [MODIFIKASI] Event Resize untuk mengatur letak kedua tombol agar menempel di kanan
            this.Resize += (s, e) =>
            {
                btnSave.Left = this.Width - btnSave.Width - 30;

                if (btnLihatNg != null)
                {
                    // Pastikan tombol Daftar NG selalu mengikuti letak tombol Simpan
                    btnLihatNg.Left = btnSave.Left - btnLihatNg.Width - 15;
                    btnHistory.Left = btnLihatNg.Left - btnHistory.Width - 15;
                }
                else
                {
                    btnHistory.Left = btnSave.Left - btnHistory.Width - 15;
                }
            };
        }

        private void LoadChecksheetData()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    // 1. Cek Komputer ini milik mesin mana (Kiosk Mode)
                    string machineIdStr = DatabaseHelper.GetMachineId();
                    if (!int.TryParse(machineIdStr, out _currentMachineId))
                    {
                        MessageBox.Show("Terminal ini belum di-setup untuk mesin apapun.\nSilakan gunakan menu Setup terlebih dahulu.", "Error Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.Close(); return;
                    }

                    // 2. Cek Template apa yang sedang aktif di mesin ini
                    var machineInfo = conn.QueryFirstOrDefault(
                        @"SELECT m.current_template_id, t.template_name, m.machine_number, mt.type_name 
                          FROM machines m 
                          LEFT JOIN checksheet_templates t ON m.current_template_id = t.template_id 
                          LEFT JOIN machine_types mt ON m.type_id = mt.type_id
                          WHERE m.machine_id = @Id", new { Id = _currentMachineId });

                    if (machineInfo == null || machineInfo.current_template_id == null)
                    {
                        MessageBox.Show("SPV / Admin belum mengatur 'Template Checksheet' untuk mesin ini.\nSilakan atur di Master Data Mesin.", "Template Kosong", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        this.Close(); return;
                    }

                    _currentTemplateId = (int)machineInfo.current_template_id;
                    lblMachineInfo.Text = $"No. Mesin: {machineInfo.type_name}.{machineInfo.machine_number} | Mode Pekerjaan: {machineInfo.template_name}";

                    // 3. Tarik Pertanyaan dari Database (Filter Lapis 3: Berdasarkan Role)
                    string targetRole = _isTeknisiMode ? "Teknisi" : "Operator";
                    var items = conn.Query(
                        @"SELECT item_id, item_name, standard_judgment, check_method, input_type 
                          FROM checksheet_items 
                          WHERE template_id = @TplId AND role_target = @RoleTarget",
                          new { TplId = _currentTemplateId, RoleTarget = targetRole }).ToList();

                    if (items.Count == 0)
                    {
                        Label emptyLbl = new Label { Text = $"Belum ada pertanyaan checksheet khusus {targetRole} di template '{machineInfo.template_name}'.\nHubungi SPV untuk menambahkan pertanyaan di Master Data.", AutoSize = true, Font = AppFonts.Body, ForeColor = Color.Red };
                        pnlQuestions.Controls.Add(emptyLbl);
                        btnSave.Enabled = false;
                        return;
                    }

                    // 4. Gambar Pertanyaannya ke Layar secara dinamis
                    int number = 1;
                    foreach (var item in items)
                    {
                        string inputType = item.input_type != null ? item.input_type.ToString() : "options";
                        var rowControl = new ChecksheetItemControl(number, (int)item.item_id, item.item_name, item.standard_judgment, item.check_method, inputType)
                        {
                            Width = this.Width - 80
                        };
                        _itemControls.Add(rowControl);
                        pnlQuestions.Controls.Add(rowControl);
                        number++;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error memuat checksheet: " + ex.Message); }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            // AMBIL IDENTITAS (NIK/INISIAL) DARI SESSION
            string userNik = UserSession.CurrentUser?.Username ?? "-";

            if (string.IsNullOrWhiteSpace(userNik) || userNik == "-")
            {
                string warningMsg = _isTeknisiMode ? "Sesi Teknisi tidak valid!" : "Sesi Operator tidak valid!";
                MessageBox.Show(warningMsg, "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 5. Validasi: Cari item PERTAMA yang belum dijawab
            var firstUnanswered = _itemControls.FirstOrDefault(c => !c.IsAnswered);
            
            if (firstUnanswered != null)
            {
                // Fokuskan (scroll) layar langsung ke pertanyaan yang belum dijawab tersebut
                pnlQuestions.ScrollControlIntoView(firstUnanswered);
                
                // Tambahkan sedikit efek visual (opsional) agar teknisi langsung sadar
                firstUnanswered.BackColor = Color.LightYellow;

                // Tampilkan pesan
                MessageBox.Show("Masih ada pertanyaan yang belum dijawab!\nPastikan semua pertanyaan memiliki status OK, NOT OK, atau N/A.", "Data Belum Lengkap", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
                // Kembalikan warna ke putih setelah user menekan OK
                firstUnanswered.BackColor = Color.White;
                return;
            }

            btnSave.Enabled = false;
            btnSave.Text = "Menyimpan Data...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    // 1. Catat Header Patroli
                    // Kolom user_nik di database bersifat string, sehingga aman diisi NIK angka maupun Inisial huruf.
                    string insertLogSql = "INSERT INTO patrol_logs (machine_id, user_nik, shift) VALUES (@MachId, @Nik, 'A'); SELECT LAST_INSERT_ID();";
                    int logId = conn.QuerySingle<int>(insertLogSql, new { MachId = _currentMachineId, Nik = userNik });

                    // 7. Simpan Detail Pemeriksaan
                    foreach (var item in _itemControls)
                    {
                        string status = item.ValueString;
                        bool createTicket = false;

                        // // AUTO-TICKETING LOGIC: 
                        // if (!item.IsOk && item.NeedsTechnician)
                        // {
                        //     createTicket = true;
                        //     try
                        //     {
                        //         var historyRepo = new MachineHistoryRepository();

                        //         await historyRepo.CreateTicketAsync(new CreateTicketRequest
                        //         {
                        //             MachineId = _currentMachineId,
                        //             OperatorNik = userNik,
                        //             ShiftName = "A", // Shift Default
                        //             ApplicatorCode = "-",
                        //             Problems = new List<TicketProblemRequest>
                        //             {
                        //                 new TicketProblemRequest
                        //                 {
                        //                     ProblemTypeName = "Lain-lain",
                        //                     FailureName = $"[CHECKSHEET] {item.ItemName} NG"
                        //                 }
                        //             }
                        //         });
                        //     }
                        //     catch { /* Abaikan error tiket otomatis agar proses simpan patroli utama tetap sukses */ }
                        // }

                        conn.Execute(
                            @"INSERT INTO patrol_log_details (log_id, item_id, status, action_note, is_ticket_created) 
                              VALUES (@LogId, @ItemId, @Status, @Note, @TicketCreated)",
                            new { LogId = logId, ItemId = item.ItemId, Status = status, Note = item.Notes, TicketCreated = createTicket }
                        );
                    }
                }

                MessageBox.Show("Hasil Patroli Harian berhasil disimpan!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan patroli: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnSave.Enabled = true;
                btnSave.Text = "Simpan Hasil Patroli";
                this.Cursor = Cursors.Default;
            }
        }

        // =========================================================================
        // CUSTOM CONTROL: BARIS PERTANYAAN
        // =========================================================================
        public class ChecksheetItemControl : UserControl
        {
            public int ItemId { get; private set; }
            public string ItemName { get; private set; }
            public string Standard { get; private set; }
            public string InputType { get; private set; }
            
            public bool IsAnswered => InputType == "numeric/text" ? !string.IsNullOrWhiteSpace(txtValue.Text) : (radOk.Checked || radNotOk.Checked || radNa.Checked);
            public bool IsOk => InputType == "numeric/text" ? true : radOk.Checked;
            public bool NeedsTechnician => InputType == "numeric/text" ? false : radNotOk.Checked;
            public bool IsNa => InputType == "numeric/text" ? false : radNa.Checked;
            public string ValueString => InputType == "numeric/text" ? txtValue.Text.Trim() : (radNa.Checked ? "N/A" : (radOk.Checked ? "OK" : "NG"));
            public string Notes => txtNote?.Visible == true ? txtNote.Text.Trim() : "";

            private RadioButton radOk, radNotOk, radNa;
            private TextBox txtValue;
            private TextBox txtNote;

            public ChecksheetItemControl(int number, int itemId, string name, string standard, string method, string inputType)
            {
                ItemId = itemId;
                ItemName = name;
                Standard = standard;
                InputType = inputType;

                this.Height = 110;
                this.BackColor = Color.White;
                this.Margin = new Padding(0, 0, 0, 15);
                this.Padding = new Padding(10);
                this.BorderStyle = BorderStyle.FixedSingle;

                Label lblName = new Label { Text = $"{number}. {name}", Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, Location = new Point(10, 10) };
                Label lblStd = new Label { Text = $"Standar: {standard}  |  Metode: {method}", Font = new Font("Segoe UI", 9.5F), ForeColor = Color.Gray, AutoSize = true, Location = new Point(30, 35) };

                this.Controls.Add(lblName);
                this.Controls.Add(lblStd);

                if (inputType == "numeric/text")
                {
                    txtValue = new TextBox { 
                        Width = 300, 
                        Font = new Font("Segoe UI", 12F), 
                        Location = new Point(30, 65),
                        PlaceholderText = "Masukkan nilai (angka/teks)" 
                    };
                    this.Controls.Add(txtValue);
                }
                else
                {
                    radOk = new RadioButton { Text = "OK", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.SeaGreen, AutoSize = true, Location = new Point(30, 65), Cursor = Cursors.Hand };
                    radNotOk = new RadioButton { Text = "NOT OK", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.Crimson, AutoSize = true, Location = new Point(100, 65), Cursor = Cursors.Hand };
                    radNa = new RadioButton { Text = "N/A", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.DimGray, AutoSize = true, Location = new Point(210, 65), Cursor = Cursors.Hand };

                    radOk.CheckedChanged += (s, e) => ToggleNotOkOptions();
                    radNotOk.CheckedChanged += (s, e) => ToggleNotOkOptions();
                    radNa.CheckedChanged += (s, e) => ToggleNotOkOptions();

                    this.Controls.Add(radOk);
                    this.Controls.Add(radNotOk);
                    this.Controls.Add(radNa);
                    
                    // TextBox untuk Catatan (hanya muncul saat NOT OK)
                    txtNote = new TextBox
                    {
                        Width = 400,
                        Font = new Font("Segoe UI", 11F),
                        Location = new Point(30, 105),
                        PlaceholderText = "Nomor berapa yang NG? / Berikan catatan",
                        Visible = false
                    };
                    this.Controls.Add(txtNote);
                }
            }

            private void ToggleNotOkOptions()
            {
                if (InputType != "numeric/text")
                {
                    this.BackColor = radNotOk.Checked ? Color.FromArgb(255, 220, 220) : Color.White;
                    
                    if (txtNote != null)
                    {
                        txtNote.Visible = radNotOk.Checked;
                        if (radNotOk.Checked) 
                        {
                            txtNote.Focus();
                            this.Height = 150; // Perbesar tinggi control
                        }
                        else
                        {
                            txtNote.Text = "";
                            this.Height = 110; // Kembalikan tinggi normal
                        }
                    }
                }
            }
        }
    }
}