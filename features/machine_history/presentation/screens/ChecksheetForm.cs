using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mtc_app.features.machine_history.presentation.controllers;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public class ChecksheetForm : AppBaseForm, IChecksheetView
    {
        private readonly ChecksheetController _controller;
        private readonly bool _isTeknisiMode;
        
        private FlowLayoutPanel pnlQuestions;
        private AppButton btnSave;
        private Button btnLihatNg;
        private AppButton btnHistory;
        private Label lblMachineInfo;
        private Label lblPelaksanaInfo;
        private ComboBox cmbShift;
        
        private List<ChecksheetItemControl> _itemControls = new List<ChecksheetItemControl>();

        public ChecksheetForm(bool isTeknisiMode = false)
        {
            _isTeknisiMode = isTeknisiMode;
            _controller = new ChecksheetController(this, isTeknisiMode);
            
            InitializeUI();
            this.Shown += async (s, e) => await _controller.LoadInitialDataAsync();
        }

        // ==========================================
        // IChecksheetView Implementation
        // ==========================================
        
        public string Shift => cmbShift.SelectedItem?.ToString() ?? "A1";

        public void SetMachineInfo(string info)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => SetMachineInfo(info))); return; }
            lblMachineInfo.Text = info;
        }

        public void SetPelaksanaInfo(string label, string value)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => SetPelaksanaInfo(label, value))); return; }
            lblPelaksanaInfo.Text = $"{label}: {value}";
        }

        public void ClearQuestions()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(ClearQuestions)); return; }
            pnlQuestions.Controls.Clear();
            _itemControls.Clear();
            btnSave.Enabled = true;
        }

        public void AddQuestion(int number, int itemId, string name, string standard, string method, string inputType, bool isPendingNg)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => AddQuestion(number, itemId, name, standard, method, inputType, isPendingNg))); return; }
            
            var rowControl = new ChecksheetItemControl(number, itemId, name, standard, method, inputType)
            {
                Width = this.Width - 80
            };
            if (isPendingNg) rowControl.SetAsPendingNg();
            
            _itemControls.Add(rowControl);
            pnlQuestions.Controls.Add(rowControl);
        }

        public void ShowEmptyState(string message)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => ShowEmptyState(message))); return; }
            Label emptyLbl = new Label { Text = message, AutoSize = true, Font = AppFonts.Body, ForeColor = Color.Red };
            pnlQuestions.Controls.Add(emptyLbl);
            btnSave.Enabled = false;
        }

        public void ShowError(string message, string title = "Error")
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => ShowError(message, title))); return; }
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void ShowWarning(string message, string title = "Peringatan")
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => ShowWarning(message, title))); return; }
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public void ShowSuccess(string message, string title = "Sukses")
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => ShowSuccess(message, title))); return; }
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void SetBusyState(bool isBusy)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => SetBusyState(isBusy))); return; }
            btnSave.Enabled = !isBusy;
            btnSave.Text = isBusy ? "Menyimpan Data..." : "Simpan Hasil Patroli";
            this.Cursor = isBusy ? Cursors.WaitCursor : Cursors.Default;
        }

        public void FocusUnansweredQuestion()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(FocusUnansweredQuestion)); return; }
            var firstUnanswered = _itemControls.FirstOrDefault(c => !c.IsAnswered);
            if (firstUnanswered != null)
            {
                pnlQuestions.ScrollControlIntoView(firstUnanswered);
                firstUnanswered.BackColor = Color.LightYellow;
                // Timeout to revert color could be added here
                System.Threading.Tasks.Task.Delay(1500).ContinueWith(t => 
                {
                    if (firstUnanswered.IsHandleCreated) 
                        firstUnanswered.Invoke(new Action(() => firstUnanswered.BackColor = Color.White)); 
                });
            }
        }

        public List<ChecksheetItemData> GetAnswers()
        {
            if (this.InvokeRequired) { return (List<ChecksheetItemData>)this.Invoke(new Func<List<ChecksheetItemData>>(GetAnswers)); }
            return _itemControls.Select(c => new ChecksheetItemData
            {
                ItemId = c.ItemId,
                ValueString = c.ValueString,
                Notes = c.Notes,
                IsPendingNg = c.IsPendingNg,
                IsAnswered = c.IsAnswered
            }).ToList();
        }

        public void CloseForm()
        {
            if (this.InvokeRequired) { this.BeginInvoke(new Action(CloseForm)); return; }
            this.Close();
        }

        // ==========================================
        // UI Layout
        // ==========================================
        
        private void InitializeUI()
        {
            this.Text = _isTeknisiMode ? "Patroli Checksheet - TEKNISI" : "Patroli Checksheet - OPERATOR";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = AppColors.CardBackground, Width = this.Width };
            Label lblTitle = new Label { Text = this.Text, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = AppColors.TextPrimary, AutoSize = true, Location = new Point(20, 15) };
            lblMachineInfo = new Label { Text = "Loading...", Font = new Font("Segoe UI", 11F), ForeColor = AppColors.TextSecondary, AutoSize = true, Location = new Point(20, 45) };
            
            lblPelaksanaInfo = new Label
            {
                Text = $"Loading...",
                Font = new Font("Segoe UI", 11F),
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Location = new Point(20, 70)
            };

            Label lblShift = new Label { Text = "Shift:", Font = new Font("Segoe UI", 11F), ForeColor = AppColors.TextPrimary, AutoSize = true, Location = new Point(650, 45), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            cmbShift = new ComboBox { 
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11F),
                Location = new Point(700, 42),
                Width = 150,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            cmbShift.Items.AddRange(new object[] { "A1", "A2", "B1", "B2", "NS" });
            cmbShift.SelectedIndex = 0;

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblMachineInfo);
            pnlHeader.Controls.Add(lblPelaksanaInfo);
            pnlHeader.Controls.Add(lblShift);
            pnlHeader.Controls.Add(cmbShift);

            pnlQuestions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(20, 20, 20, 20),
                AutoScrollMargin = new Size(0, 100)
            };

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = AppColors.CardBackground };

            btnSave = new AppButton { Text = "Simpan Hasil Patroli", Width = 250, Height = 40, Type = AppButton.ButtonType.Primary, Location = new Point(this.Width - 280, 15), Cursor = Cursors.Hand };
            btnSave.Click += async (s, e) => await _controller.SaveChecksheetAsync();

            AppButton btnCancel = new AppButton { Text = "Batal", Width = 100, Height = 40, Type = AppButton.ButtonType.Secondary, Location = new Point(20, 15), Cursor = Cursors.Hand };
            btnCancel.Click += (s, e) => this.Close();

            pnlBottom.Controls.Add(btnSave);
            pnlBottom.Controls.Add(btnCancel);

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
                    Location = new Point(btnSave.Left - 215, 15)
                };
                btnLihatNg.FlatAppearance.BorderSize = 0;
                btnLihatNg.Click += (sender, e) =>
                {
                    this.Hide();
                    using (var popup = new PopupNgListForm(_controller.CurrentMachineId))
                    {
                        popup.ShowDialog(this);
                    }
                    this.Show();
                };
                pnlBottom.Controls.Add(btnLihatNg);
            }

            btnHistory = new AppButton
            {
                Text = "History",
                Width = 100,
                Height = 40,
                Type = AppButton.ButtonType.Secondary,
                Cursor = Cursors.Hand
            };

            if (btnLihatNg != null) btnHistory.Location = new Point(btnLihatNg.Left - 115, 15);
            else btnHistory.Location = new Point(btnSave.Left - 115, 15);

            btnHistory.Click += (sender, e) =>
            {
                if (_controller.CurrentMachineId > 0 && _controller.CurrentTemplateId > 0)
                {
                    this.Hide();
                    string roleTargetLocal = _isTeknisiMode ? "Teknisi" : "Operator";
                    using (var historyForm = new ChecksheetHistoryForm(_controller.CurrentMachineId, _controller.CurrentTemplateId, roleTargetLocal))
                    {
                        historyForm.ShowDialog(this);
                    }
                    this.Show();
                }
            };
            pnlBottom.Controls.Add(btnHistory);

            this.Controls.Add(pnlQuestions);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlBottom);

            pnlHeader.SendToBack();
            pnlBottom.SendToBack();
            pnlQuestions.BringToFront();

            this.Resize += (s, e) =>
            {
                btnSave.Left = this.Width - btnSave.Width - 30;

                if (btnLihatNg != null)
                {
                    btnLihatNg.Left = btnSave.Left - btnLihatNg.Width - 15;
                    btnHistory.Left = btnLihatNg.Left - btnHistory.Width - 15;
                }
                else
                {
                    btnHistory.Left = btnSave.Left - btnHistory.Width - 15;
                }
            };
        }

        // ==========================================
        // Sub-control ChecksheetItemControl
        // ==========================================
        public class ChecksheetItemControl : UserControl
        {
            public int ItemId { get; private set; }
            public string ItemName { get; private set; }
            public string Standard { get; private set; }
            public string InputType { get; private set; }
            public bool IsPendingNg { get; private set; }
            
            public bool IsAnswered => InputType == "numeric/text" ? !string.IsNullOrWhiteSpace(txtValue.Text) : (radOk.Checked || radNotOk.Checked || radNa.Checked);
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
                    radOk = new RadioButton { Text = "OK", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.SeaGreen, AutoSize = true, Location = new Point(30, 65), Cursor = Cursors.Hand, Checked = true };
                    radNotOk = new RadioButton { Text = "NOT OK", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.Crimson, AutoSize = true, Location = new Point(100, 65), Cursor = Cursors.Hand };
                    radNa = new RadioButton { Text = "N/A", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.DimGray, AutoSize = true, Location = new Point(210, 65), Cursor = Cursors.Hand };

                    radOk.CheckedChanged += (s, e) => ToggleNotOkOptions();
                    radNotOk.CheckedChanged += (s, e) => ToggleNotOkOptions();
                    radNa.CheckedChanged += (s, e) => ToggleNotOkOptions();

                    this.Controls.Add(radOk);
                    this.Controls.Add(radNotOk);
                    this.Controls.Add(radNa);
                    
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
                            this.Height = 150;
                        }
                        else
                        {
                            txtNote.Text = "";
                            this.Height = 110;
                        }
                    }
                }
            }

            public void SetAsPendingNg()
            {
                IsPendingNg = true;
                if (InputType == "numeric/text")
                {
                    txtValue.Text = "NG";
                    txtValue.Enabled = false;
                }
                else
                {
                    radNotOk.Checked = true;
                    radOk.Enabled = false;
                    radNa.Enabled = false;
                    radNotOk.Enabled = false;
                }
                
                Label lblWarning = new Label 
                { 
                    Text = "Menunggu perbaikan", 
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic | FontStyle.Bold), 
                    ForeColor = Color.Crimson, 
                    AutoSize = true, 
                    Location = new Point(350, 70) 
                };
                if (InputType != "numeric/text") lblWarning.Location = new Point(280, 70);
                this.Controls.Add(lblWarning);
            }
        }
    }
}