using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.data.session;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.features.applicator_patrol.presentation.screens;
using mtc_app.features.operator_worksheet.presentation.screens;
using Dapper;
using mtc_app.shared.infrastructure;

namespace mtc_app.features.machine_history.presentation.screens
{
    public class OperatorMainMenuForm : AppBaseForm
    {
        private Label lblWelcome;
        private Label lblSubtitle;
        private AppButton btnHistory;
        private AppButton btnChecksheet;
        private AppButton btnApplicatorPatrol;
        private AppButton btnMicrometer;
        private AppButton btnLko;
        private AppButton btnIdleToggle;
        private AppButton btnLogout;

        private int? _currentActiveRecordId = null;
        private DateTime? _offlineStartTime = null; // Tracks activity started while offline

        private int _currentTrackedHour = DateTime.Now.Hour;
        private System.Windows.Forms.Timer _hourCheckTimer;

        // Quick Counters
        private System.Collections.Generic.Dictionary<string, int> _quickCounts = new System.Collections.Generic.Dictionary<string, int>
        {
            { "Wire", 0 },
            { "Applikator A", 0 },
            { "Applikator B", 0 },
            { "Double", 0 }
        };
        private System.Collections.Generic.Dictionary<string, Label> _lblCounts = new System.Collections.Generic.Dictionary<string, Label>();

        public OperatorMainMenuForm()
        {
            InitializeUI();
            CheckActiveIdleStatus();
            FetchCurrentHourCounts();

            _hourCheckTimer = new System.Windows.Forms.Timer { Interval = 60000 };
            _hourCheckTimer.Tick += (s, e) => CheckHourChange();
            _hourCheckTimer.Start();

            // Subscribe to sync completion to update cache with latest DB state
            SyncMgr.OnSyncStatusChanged += SyncMgr_OnSyncStatusChanged;
        }

        private void SyncMgr_OnSyncStatusChanged(object sender, mtc_app.shared.data.services.SyncStatusEventArgs e)
        {
            if (e.Status == mtc_app.shared.data.services.SyncStatus.Complete)
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(new Action(() => FetchCurrentHourCounts()));
                }
            }
        }

        private void CheckHourChange()
        {
            if (DateTime.Now.Hour != _currentTrackedHour)
            {
                _currentTrackedHour = DateTime.Now.Hour;
                FetchCurrentHourCounts();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var screen = Screen.FromControl(this);
            var workingArea = screen.WorkingArea;
            // Posisikan di pojok kanan ATAS
            this.Location = new Point(workingArea.Right - this.Width - 20, workingArea.Top + 20);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            // WM_NCHITTEST
            if (m.Msg == 0x84) 
            {
                // Jika kursor berada di area klien form, treat seperti berada di title bar agar bisa di-drag
                if ((int)m.Result == 0x1) 
                {
                    m.Result = (IntPtr)0x2; // HTCAPTION
                }
            }
        }

        private int GetMachineIdInt()
        {
            string machineIdStr = DatabaseHelper.GetMachineId();
            if (int.TryParse(machineIdStr, out int mId))
            {
                return mId;
            }
            return 0; // Fallback or invalid
        }

        private void CheckActiveIdleStatus()
        {
            try
            {
                int mId = GetMachineIdInt();
                if (mId == 0) return; // Prevent bad data

                using (var conn = DatabaseHelper.GetConnection())
                {
                    var sql = "SELECT id, activity_id, (SELECT activity_name FROM activity_types WHERE id = activity_id) as act_name FROM machine_operator_activities WHERE machine_id = @MId AND end_time IS NULL ORDER BY start_time DESC LIMIT 1";
                    var activeRec = conn.QueryFirstOrDefault(sql, new { MId = mId });
                    if (activeRec != null)
                    {
                        _currentActiveRecordId = Convert.ToInt32(activeRec.id);
                        string actName = activeRec.act_name?.ToString() ?? "Unknown";
                        SetButtonToIdleState(actName);
                    }
                    else
                    {
                        SetButtonToRunState();
                    }
                }
            }
            catch {}
        }

        private void SetButtonToRunState()
        {
            if (btnIdleToggle == null) return;
            btnIdleToggle.Text = "▶ MESIN RUN (Klik untuk Keluar/Berhenti)";
            btnIdleToggle.Type = AppButton.ButtonType.Primary; // Fallback ke Primary
        }

        private void SetButtonToIdleState(string activityName)
        {
            if (btnIdleToggle == null) return;
            btnIdleToggle.Text = $"■ MESIN STOP: {activityName} (Klik untuk RUN)";
            btnIdleToggle.Type = AppButton.ButtonType.Danger;
        }

        private void InitializeUI()
        {
            this.Text = "Menu Utama Operator";
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.None; 
            this.AutoScroll = true;

            // Restrict maximum form height to ensure it fits small screens, enabling AutoScroll naturally
            var screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
            int desiredHeight = 770; // Increased to fit new quick counters
            this.Size = new Size(550, Math.Min(desiredHeight, screenHeight - 60));
            this.StartPosition = FormStartPosition.Manual;

            string userName = UserSession.CurrentUser?.Username ?? "Operator";

            lblWelcome = new Label
            {
                Text = $"Selamat Datang, {userName}!",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                AutoSize = true,
                Location = new Point(40, 20)
            };

            lblSubtitle = new Label
            {
                Text = "Silakan pilih tugas yang ingin Anda lakukan saat ini:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = AppColors.TextSecondary,
                AutoSize = true,
                Location = new Point(40, 55)
            };

            int btnW = 470;
            int btnH = 60;
            int btnX = 40;
            int startY = 90;
            int gap = 8;

            btnHistory = new AppButton
            {
                Text = "History Mesin",
                Type = AppButton.ButtonType.Primary,
                Size = new Size(btnW, btnH),
                Location = new Point(btnX, startY),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnHistory.Click += BtnHistory_Click;

            btnChecksheet = new AppButton
            {
                Text = "Patroli Harian Mesin Cutting",
                Type = AppButton.ButtonType.Secondary, 
                Size = new Size(btnW, btnH),
                Location = new Point(btnX, startY + (btnH + gap) * 1),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnChecksheet.Click += BtnChecksheet_Click;

            btnMicrometer = new AppButton
            {
                Text = "Patroli Harian Mikrometer",
                Type = AppButton.ButtonType.Secondary,
                Size = new Size(btnW, btnH),
                Location = new Point(btnX, startY + (btnH + gap) * 2),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnMicrometer.Click += BtnMicrometer_Click;

            btnApplicatorPatrol = new AppButton
            {
                Text = "Patroli Harian Aplikator",
                Type = AppButton.ButtonType.Secondary,
                Size = new Size(btnW, btnH),
                Location = new Point(btnX, startY + (btnH + gap) * 3),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnApplicatorPatrol.Click += BtnApplicatorPatrol_Click;

            var btnLko = new AppButton
            {
                Text = "Lembar Kerja Operator (LKO)",
                Type = AppButton.ButtonType.Secondary,
                Size = new Size(btnW, btnH),
                Location = new Point(btnX, startY + (btnH + gap) * 4),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLko.Click += BtnLko_Click;

            int currentY = startY + (btnH + gap) * 5;
            int rowHeight = 48;
            int rowGap = 10;

            AddQuickCountRow("Wire", btnX, currentY, btnW, rowHeight);
            currentY += rowHeight + rowGap;
            
            AddQuickCountRow("Applikator A", btnX, currentY, btnW, rowHeight);
            currentY += rowHeight + rowGap;
            
            AddQuickCountRow("Applikator B", btnX, currentY, btnW, rowHeight);
            currentY += rowHeight + rowGap;
            
            AddQuickCountRow("Double", btnX, currentY, btnW, rowHeight);
            currentY += rowHeight + (gap * 2); // Extra spacing before MESIN RUN button

            btnIdleToggle = new AppButton
            {
                Text = "▶ MESIN RUN (Klik untuk Keluar/Berhenti)",
                Type = AppButton.ButtonType.Primary,
                Size = new Size(btnW, btnH),
                Location = new Point(btnX, currentY),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnIdleToggle.Click += BtnIdleToggle_Click;

            btnLogout = new AppButton
            {
                Text = "Logout / Kembali",
                Type = AppButton.ButtonType.Danger, 
                Size = new Size(200, 40),
                Location = new Point((550 - 200) / 2, currentY + btnH + gap),
                Cursor = Cursors.Hand
            };
            btnLogout.Click += (s, e) => this.Close(); 

            this.Controls.Add(lblWelcome);
            this.Controls.Add(lblSubtitle);
            this.Controls.Add(btnHistory);
            this.Controls.Add(btnChecksheet);
            this.Controls.Add(btnMicrometer);
            this.Controls.Add(btnApplicatorPatrol);
            this.Controls.Add(btnLko);
            this.Controls.Add(btnIdleToggle);
            this.Controls.Add(btnLogout);
        }

        private void StopCurrentDowntime()
        {
            if (_currentActiveRecordId != null || _offlineStartTime != null)
            {
                try
                {
                    int mId = GetMachineIdInt();

                    if (_offlineStartTime != null)
                    {
                        // Activity was started offline — queue the END action
                        var payload = new
                        {
                            MachineId = mId,
                            StartTime = _offlineStartTime.Value,
                            EndTime = DateTime.Now
                        };
                        OfflineRepo.AddToQueue("END_ACTIVITY", "machine_operator_activities", payload);
                    }
                    else if (_currentActiveRecordId != null)
                    {
                        if (NetworkMon.IsOnline)
                        {
                            using (var conn = DatabaseHelper.GetConnection())
                            {
                                string sql = "UPDATE machine_operator_activities SET end_time = @Now WHERE id = @Id";
                                conn.Execute(sql, new { Now = DateTime.Now, Id = _currentActiveRecordId.Value });
                            }
                        }
                        else
                        {
                            // Was started online but now offline — queue END by record ID
                            var payload = new
                            {
                                RecordId = _currentActiveRecordId.Value,
                                EndTime = DateTime.Now
                            };
                            OfflineRepo.AddToQueue("END_ACTIVITY_BY_ID", "machine_operator_activities", payload);
                        }
                    }
                }
                catch { }
                finally
                {
                    _currentActiveRecordId = null;
                    _offlineStartTime = null;
                    if (this.IsHandleCreated && !this.IsDisposed) SetButtonToRunState();
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (SyncMgr != null)
            {
                SyncMgr.OnSyncStatusChanged -= SyncMgr_OnSyncStatusChanged;
            }
            _hourCheckTimer?.Stop();
            _hourCheckTimer?.Dispose();
            StopCurrentDowntime();
            base.OnFormClosing(e);
        }

        private void BtnHistory_Click(object sender, EventArgs e)
        {
            StopCurrentDowntime();
            var historyForm = new MachineHistoryFormOperator();
            this.Hide();
            historyForm.FormClosed += (s, args) => this.Show(); 
            historyForm.Show();
        }

        private void BtnChecksheet_Click(object sender, EventArgs e)
        {
            StopCurrentDowntime();
            var checkForm = new ChecksheetForm(isTeknisiMode: false);
            this.Hide();
            checkForm.FormClosed += (s, args) => this.Show();
            checkForm.Show();
        }

        private void BtnApplicatorPatrol_Click(object sender, EventArgs e)
        {
            StopCurrentDowntime();
            var form = new ApplicatorPatrolForm(
                mtc_app.shared.infrastructure.ServiceLocator.CreateApplicatorPatrolRepository(),
                mtc_app.shared.infrastructure.ServiceLocator.CreateMasterDataRepository());
            this.Hide();
            form.FormClosed += (s, args) => this.Show();
            form.Show();
        }

        private void BtnMicrometer_Click(object sender, EventArgs e)
        {
            StopCurrentDowntime();
            var microForm = new mtc_app.features.micrometer_patrol.presentation.screens.MicrometerPatrolForm(
                mtc_app.shared.infrastructure.ServiceLocator.CreateMicrometerPatrolRepository(),
                mtc_app.shared.infrastructure.ServiceLocator.CreateMasterDataRepository());
            this.Hide();
            microForm.FormClosed += (s, args) => this.Show();
            microForm.Show();
        }

        private void BtnLko_Click(object sender, EventArgs e)
        {
            StopCurrentDowntime();
            var lkoForm = new OperatorWorksheetForm();
            this.Hide();
            lkoForm.FormClosed += (s, args) => this.Show();
            lkoForm.Show();
        }

        private void BtnIdleToggle_Click(object sender, EventArgs e)
        {
            try
            {
                int mId = GetMachineIdInt();
                if (mId == 0) 
                {
                    MessageBox.Show("Mesin belum dikonfigurasi. Silakan setup ID Mesin terlebih dahulu.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string opName = UserSession.CurrentUser?.Username ?? "Unknown";
                TimeSpan nowTime = DateTime.Now.TimeOfDay;
                string shiftName = (nowTime >= new TimeSpan(7, 0, 0) && nowTime < new TimeSpan(19, 0, 0)) ? "Shift Pagi" : "Shift Malam";

                if (_currentActiveRecordId == null && _offlineStartTime == null)
                {
                    // State is RUN, going to STOP
                    using (var dlg = new ActivitySelectionDialog())
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            if (NetworkMon.IsOnline)
                            {
                                // Online path — direct INSERT
                                using (var conn = DatabaseHelper.GetConnection())
                                {
                                    string sql = "INSERT INTO machine_operator_activities (machine_id, operator_name, activity_id, start_time, shift_name) VALUES (@MId, @OpName, @ActId, @Now, @Shift); SELECT LAST_INSERT_ID();";
                                    var startTime = DateTime.Now;
                                    int newId = conn.QuerySingle<int>(sql, new { MId = mId, OpName = opName, ActId = dlg.SelectedActivityId, Now = startTime, Shift = shiftName });
                                    
                                    _currentActiveRecordId = newId;
                                    SetButtonToIdleState(dlg.SelectedActivityName);
                                }
                            }
                            else
                            {
                                // Offline path — queue to SyncQueue
                                // Truncate milliseconds: MariaDB DATETIME stores second-precision only,
                                // so END_ACTIVITY must match the exact same truncated value.
                                var now = DateTime.Now;
                                var startTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
                                var payload = new
                                {
                                    MachineId = mId,
                                    OperatorName = opName,
                                    ActivityId = dlg.SelectedActivityId,
                                    StartTime = startTime,
                                    ShiftName = shiftName
                                };
                                OfflineRepo.AddToQueue("START_ACTIVITY", "machine_operator_activities", payload);

                                _offlineStartTime = startTime;
                                SetButtonToIdleState(dlg.SelectedActivityName);
                            }
                        }
                    }
                }
                else
                {
                    // State is STOP, going to RUN
                    StopCurrentDowntime();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengupdate status: " + ex.Message, "Error Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddQuickCountRow(string itemName, int x, int y, int width, int height)
        {
            int btnWidth = 60;
            int innerGap = 8;
            int lblWidth = width - (btnWidth * 2) - (innerGap * 2);

            var btnMinus = new AppButton
            {
                Text = "-",
                Type = AppButton.ButtonType.Secondary,
                Size = new Size(btnWidth, height),
                Location = new Point(x, y),
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            var lblDisplay = new Label
            {
                Text = $"{itemName}: 0",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = AppColors.PrimaryDark,
                BackColor = AppColors.RowHover,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(lblWidth, height),
                Location = new Point(x + btnWidth + innerGap, y)
            };

            var btnPlus = new AppButton
            {
                Text = "+",
                Type = AppButton.ButtonType.Primary,
                Size = new Size(btnWidth, height),
                Location = new Point(x + btnWidth + innerGap + lblWidth + innerGap, y),
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnMinus.Click += (s, e) => UpdateQuickCount(itemName, -1, lblDisplay);
            btnPlus.Click += (s, e) => UpdateQuickCount(itemName, 1, lblDisplay);

            _lblCounts[itemName] = lblDisplay;

            this.Controls.Add(btnMinus);
            this.Controls.Add(lblDisplay);
            this.Controls.Add(btnPlus);
        }

        private void FetchCurrentHourCounts()
        {
            // Reset to zero baseline
            foreach (var key in new System.Collections.Generic.List<string>(_quickCounts.Keys))
            {
                _quickCounts[key] = 0;
            }

            int mId = GetMachineIdInt();
            string opName = UserSession.CurrentUser?.Username ?? "Unknown";
            int currentHour = DateTime.Now.Hour;
            DateTime currentDate = DateTime.Now.Date;

            string cacheKey = $"QuickCount_{mId}_{opName}_{currentDate:yyyyMMdd}_{currentHour}";

            // Fetch from Remote DB if online
            if (NetworkMon.IsOnline)
            {
                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        string sql = @"
                            SELECT item_name, total_count 
                            FROM operator_quick_counts 
                            WHERE machine_id = @MId AND operator_name = @OpName 
                            AND record_date = @RecordDate AND record_hour = @RecordHour";

                        var results = conn.Query(sql, new 
                        { 
                            MId = mId, 
                            OpName = opName, 
                            RecordDate = currentDate,
                            RecordHour = currentHour
                        });

                        foreach (var row in results)
                        {
                            string itemName = row.item_name;
                            int count = (int)row.total_count;

                            if (_quickCounts.ContainsKey(itemName))
                            {
                                _quickCounts[itemName] = count;
                            }
                        }

                        OfflineRepo.SetCache(cacheKey, _quickCounts, TimeSpan.FromHours(12));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Fetch quick counts DB error: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    var cached = OfflineRepo.GetCache<System.Collections.Generic.Dictionary<string, int>>(cacheKey);
                    if (cached != null)
                    {
                        foreach (var kvp in cached)
                        {
                            if (_quickCounts.ContainsKey(kvp.Key))
                            {
                                _quickCounts[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Fetch quick counts Cache error: " + ex.Message);
                }
            }

            // Include pending offline deltas for current hour
            try
            {
                var queueItems = OfflineRepo.GetPendingItems();
                foreach (var item in queueItems)
                {
                    if (item.TableName == "operator_quick_counts" && 
                        (item.ActionType == "INCREMENT_QUICK_COUNT" || item.ActionType == "DECREMENT_QUICK_COUNT"))
                    {
                        var json = Newtonsoft.Json.Linq.JObject.Parse(item.PayloadJson);
                        int hmId = json["MachineId"]?.ToObject<int>() ?? 0;
                        string hopName = json["OperatorName"]?.ToString() ?? "";
                        string hItemName = json["ItemName"]?.ToString() ?? "";
                        DateTime hRecordDate = json["RecordDate"]?.ToObject<DateTime>() ?? DateTime.MinValue;
                        int hRecordHour = json["RecordHour"]?.ToObject<int>() ?? -1;

                        if (hmId == mId && hopName == opName && hRecordDate.Date == currentDate && hRecordHour == currentHour)
                        {
                            int delta = item.ActionType == "INCREMENT_QUICK_COUNT" ? 1 : -1;
                            if (_quickCounts.ContainsKey(hItemName))
                            {
                                int newCount = _quickCounts[hItemName] + delta;
                                _quickCounts[hItemName] = Math.Max(0, newCount);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Fetch quick counts offline error: " + ex.Message);
            }

            // Update UI Labels
            foreach (var kvp in _quickCounts)
            {
                if (_lblCounts.TryGetValue(kvp.Key, out var lbl))
                {
                    lbl.Text = $"{kvp.Key}: {kvp.Value}";
                }
            }
        }

        private void UpdateQuickCount(string itemName, int delta, Label lbl)
        {
            CheckHourChange();

            if (!_quickCounts.ContainsKey(itemName)) return;

            int current = _quickCounts[itemName];
            int newCount = current + delta;
            if (newCount < 0) newCount = 0;
            if (newCount == current) return;

            _quickCounts[itemName] = newCount;
            lbl.Text = $"{itemName}: {newCount}";

            try
            {
                int mId = GetMachineIdInt();
                string opName = UserSession.CurrentUser?.Username ?? "Unknown";
                TimeSpan nowTime = DateTime.Now.TimeOfDay;
                string shiftName = (nowTime >= new TimeSpan(7, 0, 0) && nowTime < new TimeSpan(19, 0, 0)) ? "Shift Pagi" : "Shift Malam";

                var payload = new
                {
                    MachineId = mId,
                    OperatorName = opName,
                    ShiftName = shiftName,
                    ItemName = itemName,
                    RecordDate = DateTime.Now.Date,
                    RecordHour = DateTime.Now.Hour
                };

                string actionType = delta > 0 ? "INCREMENT_QUICK_COUNT" : "DECREMENT_QUICK_COUNT";
                OfflineRepo.AddToQueue(actionType, "operator_quick_counts", payload);

                if (NetworkMon.IsOnline)
                {
                    SyncMgr?.SyncNow();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Queue quick count error: " + ex.Message);
            }
        }
    }
}
