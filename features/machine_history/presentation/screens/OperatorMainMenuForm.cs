using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.data.session;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.features.applicator_patrol.presentation.screens;
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
        private AppButton btnIdleToggle;
        private AppButton btnLogout;

        private int? _currentActiveRecordId = null;

        public OperatorMainMenuForm()
        {
            InitializeUI();
            CheckActiveIdleStatus();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var screen = Screen.FromControl(this);
            var workingArea = screen.WorkingArea;
            // Posisikan di sebelah kanan layar, dengan margin 20 pixel, tengah secara vertikal
            this.Location = new Point(workingArea.Right - this.Width - 20, workingArea.Top + (workingArea.Height - this.Height) / 2);
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
            this.Size = new Size(550, 580);
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = AppColors.Background;
            this.FormBorderStyle = FormBorderStyle.None; 
            this.AutoScroll = true;

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

            btnIdleToggle = new AppButton
            {
                Text = "▶ MESIN RUN (Klik untuk Keluar/Berhenti)",
                Type = AppButton.ButtonType.Primary,
                Size = new Size(btnW, btnH),
                Location = new Point(btnX, startY + (btnH + gap) * 4),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnIdleToggle.Click += BtnIdleToggle_Click;

            btnLogout = new AppButton
            {
                Text = "Logout / Kembali",
                Type = AppButton.ButtonType.Danger, 
                Size = new Size(200, 40),
                Location = new Point((550 - 200) / 2, startY + (btnH + gap) * 5),
                Cursor = Cursors.Hand
            };
            btnLogout.Click += (s, e) => this.Close(); 

            this.Controls.Add(lblWelcome);
            this.Controls.Add(lblSubtitle);
            this.Controls.Add(btnHistory);
            this.Controls.Add(btnChecksheet);
            this.Controls.Add(btnMicrometer);
            this.Controls.Add(btnApplicatorPatrol);
            this.Controls.Add(btnIdleToggle);
            this.Controls.Add(btnLogout);
        }

        private void BtnHistory_Click(object sender, EventArgs e)
        {
            var historyForm = new MachineHistoryFormOperator();
            this.Hide();
            historyForm.FormClosed += (s, args) => this.Show(); 
            historyForm.Show();
        }

        private void BtnChecksheet_Click(object sender, EventArgs e)
        {
            var checkForm = new ChecksheetForm(isTeknisiMode: false);
            this.Hide();
            checkForm.FormClosed += (s, args) => this.Show();
            checkForm.Show();
        }

        private void BtnApplicatorPatrol_Click(object sender, EventArgs e)
        {
            var form = new ApplicatorPatrolForm(
                mtc_app.shared.infrastructure.ServiceLocator.CreateApplicatorPatrolRepository(),
                mtc_app.shared.infrastructure.ServiceLocator.CreateMasterDataRepository());
            this.Hide();
            form.FormClosed += (s, args) => this.Show();
            form.Show();
        }

        private void BtnMicrometer_Click(object sender, EventArgs e)
        {
            var microForm = new mtc_app.features.micrometer_patrol.presentation.screens.MicrometerPatrolForm(
                mtc_app.shared.infrastructure.ServiceLocator.CreateMicrometerPatrolRepository(),
                mtc_app.shared.infrastructure.ServiceLocator.CreateMasterDataRepository());
            this.Hide();
            microForm.FormClosed += (s, args) => this.Show();
            microForm.Show();
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

                if (_currentActiveRecordId == null)
                {
                    // State is RUN, going to STOP
                    using (var dlg = new ActivitySelectionDialog())
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            using (var conn = DatabaseHelper.GetConnection())
                            {
                                string sql = "INSERT INTO machine_operator_activities (machine_id, operator_name, activity_id, start_time, shift_name) VALUES (@MId, @OpName, @ActId, @Now, @Shift); SELECT LAST_INSERT_ID();";
                                int newId = conn.QuerySingle<int>(sql, new { MId = mId, OpName = opName, ActId = dlg.SelectedActivityId, Now = DateTime.Now, Shift = shiftName });
                                
                                _currentActiveRecordId = newId;
                                SetButtonToIdleState(dlg.SelectedActivityName);
                            }
                        }
                    }
                }
                else
                {
                    // State is STOP, going to RUN
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        // Calc duration natively via DATEDIFF or dynamically in DB wait, I'll let the user SQL query calculate it or I can just save end_time
                        string sql = "UPDATE machine_operator_activities SET end_time = @Now WHERE id = @Id";
                        conn.Execute(sql, new { Now = DateTime.Now, Id = _currentActiveRecordId.Value });
                        
                        _currentActiveRecordId = null;
                        SetButtonToRunState();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengupdate status: " + ex.Message, "Error Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
