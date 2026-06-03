using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mtc_app.features.applicator_patrol.presentation.screens;
using mtc_app.features.operator_worksheet.presentation.screens;
using mtc_app.features.micrometer_patrol.presentation.screens;
using mtc_app.features.machine_history.presentation.controllers;
using mtc_app.shared.data.session;
using mtc_app.shared.infrastructure;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.machine_history.presentation.screens
{
    public class OperatorMainMenuForm : AppBaseForm, IOperatorMainMenuView
    {
        private readonly OperatorMainMenuController _controller;
        
        private Label lblWelcome;
        private Label lblSubtitle;
        private AppButton btnHistory;
        private AppButton btnChecksheet;
        private AppButton btnApplicatorPatrol;
        private AppButton btnMicrometer;
        private AppButton btnLko;
        private AppButton btnIdleToggle;
        private AppButton btnLogout;

        private Timer _hourCheckTimer;
        private Dictionary<string, Label> _lblCounts = new Dictionary<string, Label>();
        private Label _lblJam;
        private AppButton _btnJamMinus;
        private AppButton _btnJamPlus;

        public OperatorMainMenuForm()
        {
            _controller = new OperatorMainMenuController(this);
            InitializeUI();
            
            _ = _controller.CheckActiveIdleStatusAsync();
            _ = _controller.FetchCurrentHourCountsAsync();

            _hourCheckTimer = new Timer { Interval = 60000 };
            _hourCheckTimer.Tick += (s, e) => _controller.CheckHourChange();
            _hourCheckTimer.Start();

            SyncMgr.OnSyncStatusChanged += SyncMgr_OnSyncStatusChanged;
        }

        private void SyncMgr_OnSyncStatusChanged(object sender, mtc_app.shared.data.services.SyncStatusEventArgs e)
        {
            if (e.Status == mtc_app.shared.data.services.SyncStatus.Complete)
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(new Action(() => _ = _controller.FetchCurrentHourCountsAsync()));
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var screen = Screen.FromControl(this);
            var workingArea = screen.WorkingArea;
            this.Location = new Point(workingArea.Right - this.Width - 20, workingArea.Top + 20);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x84) 
            {
                if ((int)m.Result == 0x1) m.Result = (IntPtr)0x2;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (SyncMgr != null) SyncMgr.OnSyncStatusChanged -= SyncMgr_OnSyncStatusChanged;
            _hourCheckTimer?.Stop();
            _hourCheckTimer?.Dispose();
            _controller.StopCurrentDowntime();
            base.OnFormClosing(e);
        }

        // ==========================================
        // IOperatorMainMenuView Implementation
        // ==========================================

        public void SetRunState()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(SetRunState)); return; }
            if (btnIdleToggle == null) return;
            btnIdleToggle.Text = "▶ MESIN RUN (Klik untuk Keluar/Berhenti)";
            btnIdleToggle.Type = AppButton.ButtonType.Primary;
        }

        public void SetIdleState(string activityName)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => SetIdleState(activityName))); return; }
            if (btnIdleToggle == null) return;
            btnIdleToggle.Text = $"■ MESIN STOP: {activityName} (Klik untuk RUN)";
            btnIdleToggle.Type = AppButton.ButtonType.Danger;
        }

        public void UpdateQuickCountDisplay(string itemName, int count)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => UpdateQuickCountDisplay(itemName, count))); return; }
            if (_lblCounts.TryGetValue(itemName, out var lbl))
            {
                lbl.Text = $"{itemName}: {count}";
            }
        }

        public void UpdateJamDisplay(int viewedHour, int currentTrackedHour, int shiftHourDisplay)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => UpdateJamDisplay(viewedHour, currentTrackedHour, shiftHourDisplay))); return; }
            if (_lblJam != null) _lblJam.Text = $"Jam: {shiftHourDisplay}";
            if (_btnJamPlus != null) _btnJamPlus.Enabled = (viewedHour != currentTrackedHour);
            if (_btnJamMinus != null) _btnJamMinus.Enabled = (shiftHourDisplay > 1);
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

        public void OpenHistoryForm()
        {
            var historyForm = new MachineHistoryFormOperator();
            historyForm.Show();
        }

        public void OpenChecksheetForm()
        {
            var existing = Application.OpenForms.OfType<ChecksheetForm>().FirstOrDefault();
            if (existing != null)
            {
                if (existing.WindowState == FormWindowState.Minimized) existing.WindowState = FormWindowState.Normal;
                existing.BringToFront();
                return;
            }
            var checkForm = new ChecksheetForm(isTeknisiMode: false);
            checkForm.Show();
        }

        public void OpenApplicatorPatrolForm()
        {
            var existing = Application.OpenForms.OfType<ApplicatorPatrolForm>().FirstOrDefault();
            if (existing != null)
            {
                if (existing.WindowState == FormWindowState.Minimized) existing.WindowState = FormWindowState.Normal;
                existing.BringToFront();
                return;
            }
            var form = new ApplicatorPatrolForm(
                ServiceLocator.CreateApplicatorPatrolRepository(),
                ServiceLocator.CreateMasterDataRepository());
            form.Show();
        }

        public void OpenMicrometerPatrolForm()
        {
            var existing = Application.OpenForms.OfType<MicrometerPatrolForm>().FirstOrDefault();
            if (existing != null)
            {
                if (existing.WindowState == FormWindowState.Minimized) existing.WindowState = FormWindowState.Normal;
                existing.BringToFront();
                return;
            }
            var microForm = new MicrometerPatrolForm(
                ServiceLocator.CreateMicrometerPatrolRepository(),
                ServiceLocator.CreateMasterDataRepository());
            microForm.Show();
        }

        public void OpenOperatorWorksheetForm()
        {
            var existing = Application.OpenForms.OfType<OperatorWorksheetForm>().FirstOrDefault();
            if (existing != null)
            {
                if (existing.WindowState == FormWindowState.Minimized) existing.WindowState = FormWindowState.Normal;
                existing.BringToFront();
                return;
            }
            var lkoForm = new OperatorWorksheetForm();
            lkoForm.Show();
        }

        public void OpenActivitySelectionDialog()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(OpenActivitySelectionDialog)); return; }
            
            if (btnIdleToggle.Text.StartsWith("▶"))
            {
                using (var dlg = new ActivitySelectionDialog())
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        _controller.ToggleMachineState(dlg.SelectedActivityId, dlg.SelectedActivityName);
                    }
                }
            }
            else
            {
                _controller.StopCurrentDowntime();
            }
        }

        public void TriggerSync()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(TriggerSync)); return; }
            SyncMgr?.SyncNow();
        }

        // ==========================================
        // UI Code
        // ==========================================

        private void InitializeUI()
        {
            this.Text = "Menu Utama Operator";
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.None; 
            this.AutoScroll = true;

            var screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
            int desiredHeight = 830; 
            this.Size = new Size(550, Math.Min(desiredHeight, screenHeight - 60));
            this.StartPosition = FormStartPosition.Manual;

            string userName = UserSession.CurrentUser?.Username ?? "Operator";

            lblWelcome = new Label { Text = $"Selamat Datang, {userName}!", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = AppColors.TextPrimary, AutoSize = true, Location = new Point(40, 20) };
            lblSubtitle = new Label { Text = "Silakan pilih tugas yang ingin Anda lakukan saat ini:", Font = new Font("Segoe UI", 10F), ForeColor = AppColors.TextSecondary, AutoSize = true, Location = new Point(40, 55) };

            int btnW = 470, btnH = 60, btnX = 40, startY = 90, gap = 8;

            btnHistory = new AppButton { Text = "History Mesin", Type = AppButton.ButtonType.Primary, Size = new Size(btnW, btnH), Location = new Point(btnX, startY), Font = new Font("Segoe UI", 13F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnHistory.Click += (s, e) => { _controller.StopCurrentDowntime(); OpenHistoryForm(); };

            btnChecksheet = new AppButton { Text = "Patroli Harian Mesin Cutting", Type = AppButton.ButtonType.Secondary, Size = new Size(btnW, btnH), Location = new Point(btnX, startY + (btnH + gap) * 1), Font = new Font("Segoe UI", 13F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnChecksheet.Click += (s, e) => { _controller.StopCurrentDowntime(); OpenChecksheetForm(); };

            btnMicrometer = new AppButton { Text = "Patroli Harian Mikrometer", Type = AppButton.ButtonType.Secondary, Size = new Size(btnW, btnH), Location = new Point(btnX, startY + (btnH + gap) * 2), Font = new Font("Segoe UI", 13F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnMicrometer.Click += (s, e) => { _controller.StopCurrentDowntime(); OpenMicrometerPatrolForm(); };

            btnApplicatorPatrol = new AppButton { Text = "Patroli Harian Aplikator", Type = AppButton.ButtonType.Secondary, Size = new Size(btnW, btnH), Location = new Point(btnX, startY + (btnH + gap) * 3), Font = new Font("Segoe UI", 13F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnApplicatorPatrol.Click += (s, e) => { _controller.StopCurrentDowntime(); OpenApplicatorPatrolForm(); };

            btnLko = new AppButton { Text = "Lembar Kerja Operator (LKO)", Type = AppButton.ButtonType.Secondary, Size = new Size(btnW, btnH), Location = new Point(btnX, startY + (btnH + gap) * 4), Font = new Font("Segoe UI", 13F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnLko.Click += (s, e) => { _controller.StopCurrentDowntime(); OpenOperatorWorksheetForm(); };

            int currentY = startY + (btnH + gap) * 5;
            int rowHeight = 48, rowGap = 10;

            AddQuickCountRow("Wire", btnX, currentY, btnW, rowHeight); currentY += rowHeight + rowGap;
            AddQuickCountRow("Applikator A", btnX, currentY, btnW, rowHeight); currentY += rowHeight + rowGap;
            AddQuickCountRow("Applikator B", btnX, currentY, btnW, rowHeight); currentY += rowHeight + rowGap;
            AddQuickCountRow("Double", btnX, currentY, btnW, rowHeight); currentY += rowHeight + rowGap;

            AddJamRow(btnX, currentY, btnW, rowHeight);
            currentY += rowHeight + (gap * 2);

            btnIdleToggle = new AppButton { Text = "▶ MESIN RUN (Klik untuk Keluar/Berhenti)", Type = AppButton.ButtonType.Primary, Size = new Size(btnW, btnH), Location = new Point(btnX, currentY), Font = new Font("Segoe UI", 13F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnIdleToggle.Click += (s, e) => OpenActivitySelectionDialog();

            btnLogout = new AppButton { Text = "Logout / Kembali", Type = AppButton.ButtonType.Danger, Size = new Size(200, 40), Location = new Point((550 - 200) / 2, currentY + btnH + gap), Cursor = Cursors.Hand };
            btnLogout.Click += (s, e) => this.Close();

            this.Controls.Add(lblWelcome); this.Controls.Add(lblSubtitle); this.Controls.Add(btnHistory);
            this.Controls.Add(btnChecksheet); this.Controls.Add(btnMicrometer); this.Controls.Add(btnApplicatorPatrol);
            this.Controls.Add(btnLko); this.Controls.Add(btnIdleToggle); this.Controls.Add(btnLogout);
        }

        private void AddQuickCountRow(string itemName, int x, int y, int width, int height)
        {
            int btnWidth = 60, innerGap = 8;
            int lblWidth = width - (btnWidth * 2) - (innerGap * 2);

            var btnMinus = new AppButton { Text = "-", Type = AppButton.ButtonType.Secondary, Size = new Size(btnWidth, height), Location = new Point(x, y), Font = new Font("Segoe UI", 20F, FontStyle.Bold), Cursor = Cursors.Hand };
            var lblDisplay = new Label { Text = $"{itemName}: 0", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = AppColors.PrimaryDark, BackColor = AppColors.RowHover, TextAlign = ContentAlignment.MiddleCenter, Size = new Size(lblWidth, height), Location = new Point(x + btnWidth + innerGap, y) };
            var btnPlus = new AppButton { Text = "+", Type = AppButton.ButtonType.Primary, Size = new Size(btnWidth, height), Location = new Point(x + btnWidth + innerGap + lblWidth + innerGap, y), Font = new Font("Segoe UI", 18F, FontStyle.Bold), Cursor = Cursors.Hand };

            btnMinus.Click += (s, e) => _controller.UpdateQuickCount(itemName, -1);
            btnPlus.Click += (s, e) => _controller.UpdateQuickCount(itemName, 1);

            _lblCounts[itemName] = lblDisplay;
            this.Controls.Add(btnMinus); this.Controls.Add(lblDisplay); this.Controls.Add(btnPlus);
        }

        private void AddJamRow(int x, int y, int width, int height)
        {
            int btnWidth = 60, innerGap = 8;
            int lblWidth = width - (btnWidth * 2) - (innerGap * 2);

            _btnJamMinus = new AppButton { Text = "-", Type = AppButton.ButtonType.Secondary, Size = new Size(btnWidth, height), Location = new Point(x, y), Font = new Font("Segoe UI", 20F, FontStyle.Bold), Cursor = Cursors.Hand };
            _lblJam = new Label { Text = $"Jam: 1", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = AppColors.PrimaryDark, BackColor = AppColors.RowHover, TextAlign = ContentAlignment.MiddleCenter, Size = new Size(lblWidth, height), Location = new Point(x + btnWidth + innerGap, y) };
            _btnJamPlus = new AppButton { Text = "+", Type = AppButton.ButtonType.Primary, Size = new Size(btnWidth, height), Location = new Point(x + btnWidth + innerGap + lblWidth + innerGap, y), Font = new Font("Segoe UI", 18F, FontStyle.Bold), Cursor = Cursors.Hand };

            _btnJamMinus.Click += (s, e) => _controller.ChangeViewedHour(-1);
            _btnJamPlus.Click += (s, e) => _controller.ChangeViewedHour(1);

            this.Controls.Add(_btnJamMinus); this.Controls.Add(_lblJam); this.Controls.Add(_btnJamPlus);
            _controller.CheckHourChange();
        }
    }
}
