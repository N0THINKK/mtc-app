using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks; 
using System.Windows.Forms;
using System.Data;
using Dapper;
using mtc_app.features.machine_history.presentation.components;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.infrastructure;
using mtc_app.features.machine_history.data.dtos;

namespace mtc_app.features.machine_history.presentation.screens
{
    public partial class MachineHistoryFormTechnician : AppBaseForm
    {
        private readonly long _currentTicketId;
        private bool _isVerified = false;
        private bool _allowClose = false; 
        
        // [BARU] Flag untuk memicu Auto-Start
        private bool _isAutoStart = false;
        private int _patrolDetailId = 0;
        
        // Ticket State (for resume workflow)
        private int _ticketStatus = 1;
        private int _isMachineRunning = 0;
        private int _lastPartStatusId = -1; 
        
        // Accumulated Timer (DB-persisted, counts only while form is open)
        private int _arrivalSeconds = 0;  
        private int _repairSeconds = 0;   
        private int _inspectionSeconds = 0; 
        private Timer _timer;
        private Timer _timerNotifSound;
        
        // Session Tracking
        private List<long> _activeSessionIds = new List<long>();
        private List<int> _activeTechnicianIds = new List<int>();
        private Dictionary<long, int> _sessionElapsedMap = new Dictionary<long, int>();

        // UI Controls
        private Label _lblPreviousTechnicians; 
        private AppInput inputNIK;
        private FlowLayoutPanel pnlActiveTechs; 
        private AppButton btnVerify;
        private AppButton btnAddTechnician; 
        
        // Multi-Problem List
        private FlowLayoutPanel pnlProblems;
        private Button btnAddProblem;
        private List<TechnicianProblemItemControl> _problemControls = new List<TechnicianProblemItemControl>();

        // Other Inputs
        private AppInput inputCounter;
        private AppInput inputSparepart;
        private CheckBox chk4M;
        private CheckBox chkTidak4M;
        private AppStarRating ratingOperator;
        private AppInput inputOperatorNote;

        // [MODIFIKASI] Menambahkan parameter autoStart = false
        public MachineHistoryFormTechnician(long ticketId, bool autoStart = false, int patrolDetailId = 0)
        {
            _currentTicketId = ticketId;
            _isAutoStart = autoStart;
            _patrolDetailId = patrolDetailId;

            InitializeComponent();
            LoadTicketStatus(); 
            SetupTimer();
            SetupInputs();
            LoadTicketProblems();
            LoadOfflineTicketState();
            LoadActiveSessions(); 
            UpdateUIState();
            
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CP_NOCLOSE_BUTTON = 0x200;
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.OnResize(EventArgs.Empty);

            // ========================================================
            // [BARU] LOGIKA AUTO-START DARI PATROLI CHECKSHEET
            // ========================================================
            if (_isAutoStart && !_isVerified && _currentTicketId > 0)
            {
                var currentUser = mtc_app.shared.data.session.UserSession.CurrentUser;
                if (currentUser != null)
                {
                    // Gunakan Username (Inisial) jika ada, jika tidak fallback ke NIK
                    string activeCredential = !string.IsNullOrEmpty(currentUser.Username) ? currentUser.Username : currentUser.Nik;
                    
                    if (!string.IsNullOrEmpty(activeCredential))
                    {
                        inputNIK.InputValue = activeCredential;
                        BtnVerify_Click(this, EventArgs.Empty);
                    }
                }
            }
        }

        private void LoadTicketStatus()
        {
            if (_currentTicketId <= 0) return; 
            
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var ticket = conn.QueryFirstOrDefault(@"
                        SELECT status_id AS StatusId, 
                               IFNULL(arrival_elapsed_seconds, 0) AS ArrivalSeconds,
                               IFNULL(repair_elapsed_seconds, 0) AS RepairSeconds,
                               IFNULL(inspection_elapsed_seconds, 0) AS InspectionSeconds,
                               IFNULL(is_machine_running, 0) AS IsMachineRunning
                        FROM tickets WHERE ticket_id = @Id",
                        new { Id = _currentTicketId });
                    
                    if (ticket != null)
                    {
                        _ticketStatus = (int)ticket.StatusId;
                        _arrivalSeconds = (int)ticket.ArrivalSeconds;
                        _repairSeconds = (int)ticket.RepairSeconds;
                        _inspectionSeconds = (int)ticket.InspectionSeconds;
                        _isMachineRunning = (int)ticket.IsMachineRunning;
                        
                        UpdateMachineStateIndicator();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FormTechnician] Error loading status: {ex.Message}");
            }
        }

        private void UpdateMachineStateIndicator()
        {
            if (_isMachineRunning == 1)
            {
                lblMachineState.Text = "\u25b6 RUN";
                lblMachineState.ForeColor = System.Drawing.Color.FromArgb(34, 197, 94); 
            }
            else
            {
                lblMachineState.Text = "\u25a0 STOP";
                lblMachineState.ForeColor = System.Drawing.Color.FromArgb(239, 68, 68); 
            }
        }

        private async void LblMachineState_Click(object sender, EventArgs e)
        {
            int newState = (_isMachineRunning == 1) ? 0 : 1;
            string stateText = newState == 1 ? "RUN" : "STOP";

            var confirm = MessageBox.Show(
                $"Ubah status mesin menjadi {stateText}?",
                "Konfirmasi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                if (_currentTicketId > 0)
                {
                    await Task.Run(() =>
                    {
                        using (var conn = DatabaseHelper.GetConnection())
                        {
                            conn.Open();
                            conn.Execute(
                                "UPDATE tickets SET is_machine_running = @State WHERE ticket_id = @Id",
                                new { State = newState, Id = _currentTicketId });
                        }
                    });
                }
                _isMachineRunning = newState;
                UpdateMachineStateIndicator();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal mengubah status mesin: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupTimer()
        {
            _timer = new Timer { Interval = 1000 }; 
            _timer.Tick += Timer_Tick;
            _timer.Start();
            
            _timerNotifSound = new Timer { Interval = 1500 }; 
            _timerNotifSound.Tick += (s, e) => System.Media.SystemSounds.Asterisk.Play();

            UpdateTimerDisplay();
        }

        private int _tickCounter = 0;
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_isVerified)
            {
                _arrivalSeconds++;
            }
            else if (_ticketStatus == 2)
            {
                _repairSeconds++;
                foreach (var sessionId in _activeSessionIds)
                {
                    if (_sessionElapsedMap.ContainsKey(sessionId))
                    {
                        _sessionElapsedMap[sessionId]++;
                    }
                }
            }
            else if (_ticketStatus == 4)
            {
                _inspectionSeconds++;
            }

            UpdateTimerDisplay();

            _tickCounter++;
            if (_isVerified && _tickCounter % 3 == 0 && _currentTicketId > 0)
            {
                Task.Run(() => UpdatePartRequestStatus());
            }

            // Sync timer to DB every 10 seconds so Dashboard can see "Live" updates
            if (_tickCounter % 10 == 0 && _currentTicketId > 0)
            {
                Task.Run(() => SaveTimerToDatabase());
            }
        }

        private void UpdateTimerDisplay()
        {
            labelArrival.Text = TimeSpan.FromSeconds(_arrivalSeconds).ToString(@"hh\:mm\:ss");
            labelFinished.Text = TimeSpan.FromSeconds(_repairSeconds).ToString(@"hh\:mm\:ss");
        }

        private void SaveTimerToDatabase()
        {
            if (_currentTicketId <= 0) return; 
            
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    conn.Execute(@"
                        UPDATE tickets 
                        SET arrival_elapsed_seconds = @Arrival, 
                            repair_elapsed_seconds = @Repair,
                            inspection_elapsed_seconds = @Inspect
                        WHERE ticket_id = @Id",
                        new { Arrival = _arrivalSeconds, Repair = _repairSeconds, Inspect = _inspectionSeconds, Id = _currentTicketId });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FormTechnician] Error saving timer: {ex.Message}");
            }
        }

        private void CreateSession(int technicianId)
        {
            if (_currentTicketId <= 0) return; 
            
            if (_activeTechnicianIds.Contains(technicianId))
            {
                MessageBox.Show("Teknisi ini sudah aktif dalam sesi ini.", "Info");
                return;
            }
            
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    conn.Execute(@"
                        INSERT INTO ticket_technician_sessions 
                        (ticket_id, technician_id, shift_id, started_at, elapsed_seconds, is_completing_session)
                        VALUES (@TicketId, @TechId, NULL, NOW(), 0, 0)",
                        new { TicketId = _currentTicketId, TechId = technicianId });
                    
                    long newSessionId = conn.QueryFirstOrDefault<long>("SELECT LAST_INSERT_ID()");
                    
                    _activeSessionIds.Add(newSessionId);
                    _activeTechnicianIds.Add(technicianId);
                    _sessionElapsedMap[newSessionId] = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FormTechnician] Error creating session: {ex.Message}");
            }
        }

        private void SaveSession()
        {
            if (_activeSessionIds.Count == 0) return;
            
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    foreach (var sessionId in _activeSessionIds)
                    {
                        if (_sessionElapsedMap.ContainsKey(sessionId))
                        {
                            int elapsed = _sessionElapsedMap[sessionId];
                            conn.Execute(@"
                                UPDATE ticket_technician_sessions 
                                SET elapsed_seconds = @Elapsed, ended_at = NOW()
                                WHERE session_id = @Id",
                                new { Elapsed = elapsed, Id = sessionId });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FormTechnician] Error saving session: {ex.Message}");
            }
        }

        private void EndSessionAsCompleted()
        {
            if (_activeSessionIds.Count == 0) return;
            
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    foreach (var sessionId in _activeSessionIds)
                    {
                        if (_sessionElapsedMap.ContainsKey(sessionId))
                        {
                            int elapsed = _sessionElapsedMap[sessionId];
                            conn.Execute(@"
                                UPDATE ticket_technician_sessions 
                                SET elapsed_seconds = @Elapsed, ended_at = NOW(), is_completing_session = 1
                                WHERE session_id = @Id",
                                new { Elapsed = elapsed, Id = sessionId });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FormTechnician] Error completing session: {ex.Message}");
            }
        }

        private void LoadPreviousSessions()
        {
            if (_currentTicketId <= 0) return;
            
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var sessions = conn.Query(@"
                        SELECT u.full_name AS TechName, 
                               tts.elapsed_seconds AS Elapsed,
                               tts.is_completing_session AS IsCompleting
                        FROM ticket_technician_sessions tts
                        JOIN users u ON tts.technician_id = u.user_id
                        WHERE tts.ticket_id = @Id
                        ORDER BY tts.started_at ASC",
                        new { Id = _currentTicketId });
                    
                    if (sessions.Any())
                    {
                        var lines = new List<string>();
                        lines.Add("⚠️ Riwayat Sesi Teknisi:");
                        
                        foreach (var s in sessions)
                        {
                            string name = (string)s.TechName;
                            int elapsed = (int)s.Elapsed;
                            bool isCompleting = ((int)s.IsCompleting) == 1;
                            
                            var ts = TimeSpan.FromSeconds(elapsed);
                            string duration = ts.TotalMinutes >= 1 
                                ? $"{(int)ts.TotalMinutes} menit" 
                                : $"{elapsed} detik";
                            
                            string marker = isCompleting ? " ✅" : "";
                            lines.Add($"  • {name}: {duration}{marker}");
                        }
                        
                        _lblPreviousTechnicians.Text = string.Join("\n", lines);
                        _lblPreviousTechnicians.Visible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FormTechnician] Error loading sessions: {ex.Message}");
            }
        }

        private void LoadTicketProblems()
        {
            try
            {
                if (_currentTicketId < 0)
                {
                    int pendingId = (int)Math.Abs(_currentTicketId);
                    var request = ServiceLocator.OfflineRepo.GetPendingTicketById(pendingId);
                    
                    if (request != null)
                    {
                        foreach (var prob in request.Problems)
                        {
                            var control = new TechnicianProblemItemControl(
                                0, 
                                prob.ProblemTypeName ?? "", 
                                prob.FailureName ?? "", 
                                _isVerified
                            );
                            _problemControls.Add(control);
                            pnlProblems.Controls.Add(control);
                        }
                    }
                    return;
                }

                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT 
                            tp.problem_id,
                            COALESCE(pt.type_name, tp.problem_type_remarks, '') AS ProblemType,
                            COALESCE(f.failure_name, tp.failure_remarks, '') AS ProblemDetail
                        FROM ticket_problems tp
                        LEFT JOIN problem_types pt ON tp.problem_type_id = pt.type_id
                        LEFT JOIN failures f ON tp.failure_id = f.failure_id
                        WHERE tp.ticket_id = @Id";

                    var problems = conn.Query(sql, new { Id = _currentTicketId });

                    foreach (var p in problems)
                    {
                        var control = new TechnicianProblemItemControl(
                            (long)p.problem_id, 
                            (string)p.ProblemType, 
                            (string)p.ProblemDetail, 
                            _isVerified
                        );
                        _problemControls.Add(control);
                        pnlProblems.Controls.Add(control);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memuat problem: {ex.Message}", "Error");
            }
        }

        private void LoadOfflineTicketState()
        {
            if (_currentTicketId < 0)
            {
                try 
                {
                    int pendingId = (int)Math.Abs(_currentTicketId);
                    var request = ServiceLocator.OfflineRepo.GetPendingTicketById(pendingId);                    
                    if (request != null && !string.IsNullOrEmpty(request.TechnicianNik))
                    {
                        _isVerified = true;
                        inputNIK.InputValue = request.TechnicianNik;
                        
                        if (request.StartedAt.HasValue)
                        {
                            _ticketStatus = 2;
                        }
                    }
                }
                catch { /* Ignore */ }
            }
        }

        private void BtnAddProblem_Click(object sender, EventArgs e)
        {
            var control = new TechnicianProblemItemControl(0, "", "", _isVerified); 
            _problemControls.Add(control);
            pnlProblems.Controls.Add(control);
            
            control.SetEnabled(_isVerified);
            
            if (mainLayout != null)
            {
                 int contentWidth = mainLayout.ClientSize.Width - 80;
                 if (contentWidth < 300) contentWidth = 300;
                 control.Width = contentWidth;
            }
        }

        private void SetupInputs()
        {
            _lblPreviousTechnicians = new Label
            {
                Text = "",
                Font = AppFonts.BodySmall,
                ForeColor = Color.DarkOrange,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10),
                Visible = false
            };
            mainLayout.Controls.Add(_lblPreviousTechnicians);
            LoadPreviousSessions(); 
            
            inputNIK = new AppInput 
            { 
                LabelText = "Inisial Teknisi", 
                InputType = AppInput.InputTypeEnum.Text, 
                IsRequired = true,
                CharacterCasing = CharacterCasing.Upper
            };
            mainLayout.Controls.Add(inputNIK);

            pnlActiveTechs = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                MinimumSize = new Size(10, 10),
                Margin = new Padding(0, 5, 0, 5),
                BackColor = Color.Transparent
            };
            mainLayout.Controls.Add(pnlActiveTechs);

            btnVerify = new AppButton 
            { 
                Text = "Verifikasi Teknisi", 
                Type = AppButton.ButtonType.Primary, 
                Margin = new Padding(0, 0, 0, 15)
            };
            btnVerify.Click += BtnVerify_Click;
            mainLayout.Controls.Add(btnVerify);

            btnAddTechnician = new AppButton 
            { 
                Text = "+ Tambah Teknisi (Pendamping)", 
                Type = AppButton.ButtonType.Secondary, 
                Margin = new Padding(0, 0, 0, 15),
                Visible = false 
            };
            btnAddTechnician.Click += BtnAddTechnician_Click;
            mainLayout.Controls.Add(btnAddTechnician);

            var lblProblems = new Label 
            { 
                Text = "Daftar Perbaikan:", 
                Font = AppFonts.Subtitle, 
                AutoSize = true, 
                Margin = new Padding(0, 10, 0, 5) 
            };
            mainLayout.Controls.Add(lblProblems);

            pnlProblems = new FlowLayoutPanel 
            { 
                FlowDirection = FlowDirection.TopDown, 
                AutoSize = true, 
                WrapContents = false 
            };
            mainLayout.Controls.Add(pnlProblems);

            btnAddProblem = new Button
            {
                Text = "+ Tambah Problem Lain",
                Size = new Size(200, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.WhiteSmoke,
                ForeColor = AppColors.Primary,
                Font = AppFonts.BodySmall,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 5, 0, 15)
            };
            btnAddProblem.FlatAppearance.BorderColor = AppColors.Primary;
            btnAddProblem.FlatAppearance.BorderSize = 1;
            btnAddProblem.Click += BtnAddProblem_Click;
            mainLayout.Controls.Add(btnAddProblem);

            var panel4M = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 15, 0, 5)
            };
            var lbl4M = new Label 
            { 
                Text = "Apakah ada pergantian blade/crimping dies?", 
                Font = AppFonts.Subtitle, 
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 5)
            };
            var pnlCheckboxes = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };
            
            chk4M = new CheckBox { Text = "Iya", AutoSize = true, Margin = new Padding(0, 0, 15, 0) };
            chkTidak4M = new CheckBox { Text = "Tidak", AutoSize = true };
            
            chk4M.CheckedChanged += (s, e) => 
            { 
                if (chk4M.Checked) chkTidak4M.Checked = false; 
                inputCounter.Enabled = _isVerified && chk4M.Checked; 
                if (!chk4M.Checked) inputCounter.InputValue = ""; 
            };
            
            chkTidak4M.CheckedChanged += (s, e) => 
            { 
                if (chkTidak4M.Checked) 
                { 
                    chk4M.Checked = false; 
                    inputCounter.Enabled = false; 
                    inputCounter.InputValue = ""; 
                } 
            };
            
            pnlCheckboxes.Controls.AddRange(new Control[] { chk4M, chkTidak4M });
            panel4M.Controls.AddRange(new Control[] { lbl4M, pnlCheckboxes });
            mainLayout.Controls.Add(panel4M);

            inputCounter = new AppInput 
            { 
                LabelText = "Jumlah Counter", 
                InputType = AppInput.InputTypeEnum.Text, 
                IsRequired = false 
            };
            mainLayout.Controls.Add(inputCounter);

            inputSparepart = new AppInput 
            { 
                LabelText = "Permintaan Sparepart", 
                InputType = AppInput.InputTypeEnum.Dropdown, 
                IsRequired = false, 
                AllowCustomText = true 
            };
            mainLayout.Controls.Add(inputSparepart);
            LoadParts();

            var panelRating = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 10, 0, 0)
            };
            var lblRating = new Label 
            { 
                Text = "Rating Dari Teknisi:", 
                Font = AppFonts.Subtitle, 
                AutoSize = true, 
                Margin = new Padding(0, 0, 0, 5) 
            };
            
            ratingOperator = new AppStarRating { Rating = 0, Margin = new Padding(0) };
            panelRating.Controls.AddRange(new Control[] { lblRating, ratingOperator });
            mainLayout.Controls.Add(panelRating);

            inputOperatorNote = new AppInput 
            { 
                LabelText = "Catatan : ", 
                InputType = AppInput.InputTypeEnum.Text, 
                IsRequired = false 
            };
            mainLayout.Controls.Add(inputOperatorNote);
            
            var spacer = new Panel { Height = 30, Width = 10, BackColor = Color.Transparent };
            mainLayout.Controls.Add(spacer);
        }

        private async void LoadParts()
        {
            try
            {
                var repo = mtc_app.shared.infrastructure.ServiceLocator.CreateMasterDataRepository();
                var parts = await repo.GetPartsAsync();
                
                var dropdownItems = parts
                    .Select(p => $"{(string.IsNullOrEmpty(p.PartCode) ? "N/A" : p.PartCode)} - {p.PartName}")
                    .ToArray();
                    
                inputSparepart.SetDropdownItems(dropdownItems);
            }
            catch (Exception ex) 
            {
                System.Diagnostics.Debug.WriteLine($"[FormTechnician] Error loading parts: {ex.Message}");
            }
        }

        private void UpdateUIState()
        {
            bool enabled = _isVerified;
            
            foreach (var prob in _problemControls)
            {
                prob.SetEnabled(enabled);
            }

            chk4M.Enabled = enabled;
            chkTidak4M.Enabled = enabled;
            inputCounter.Enabled = enabled && chk4M.Checked;
            inputSparepart.Enabled = enabled;
            ratingOperator.ReadOnly = !enabled;
            inputOperatorNote.Enabled = enabled;
            buttonRepairComplete.Enabled = enabled;
            buttonRequestSparepart.Enabled = enabled;

            inputNIK.Enabled = !enabled;
            btnVerify.Enabled = !enabled;
            btnVerify.Visible = !enabled;
            
            if (btnAddProblem != null)
            {
                btnAddProblem.Visible = !enabled;
            }

            if (btnAddTechnician != null)
            {
                btnAddTechnician.Visible = enabled; 
            }
        }

        private void UpdatePartRequestStatus()
        {
            if (_currentTicketId < 0)
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)delegate 
                    {
                        inputSparepart.Enabled = _isVerified;
                        buttonRequestSparepart.Enabled = _isVerified;
                        buttonRequestSparepart.Text = "Kirimkan Permintaan Sparepart";
                    });
                }
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    var request = conn.QueryFirstOrDefault(
                        "SELECT status_id FROM part_requests WHERE ticket_id = @Id ORDER BY requested_at DESC", 
                        new { Id = _currentTicketId });
                    
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.Invoke((MethodInvoker)delegate 
                        {
                            if (request != null)
                            {
                                inputSparepart.Enabled = false;
                                buttonRequestSparepart.Enabled = false;
                                
                                int statusId = (int)request.status_id;
                                
                                if (_lastPartStatusId != -1 && _lastPartStatusId != statusId)
                                {
                                    if (statusId == 2)
                                    {
                                        _lastPartStatusId = statusId; 
                                        
                                        _timerNotifSound.Start();
                                        using (var notifForm = new mtc_app.features.machine_history.presentation.components.SparepartReadyNotificationForm())
                                        {
                                            notifForm.ShowDialog();
                                        }
                                        _timerNotifSound.Stop();
                                    }
                                    else if (statusId == 4)
                                    {
                                        _lastPartStatusId = statusId; 
                                        
                                        _timerNotifSound.Start();
                                        using (var notifForm = new mtc_app.features.machine_history.presentation.components.SparepartRejectedNotificationForm())
                                        {
                                            notifForm.ShowDialog();
                                        }
                                        _timerNotifSound.Stop();
                                    }
                                }
                                _lastPartStatusId = statusId;
                                
                                if (statusId == 1) 
                                { 
                                    buttonRequestSparepart.Text = "PERMINTAAN DIPROSES"; 
                                    buttonRequestSparepart.BackColor = Color.Gray; 
                                }
                                else if (statusId == 2) 
                                { 
                                    buttonRequestSparepart.Text = "BARANG SIAP DI GUDANG"; 
                                    buttonRequestSparepart.BackColor = AppColors.Success; 
                                }
                                else if (statusId == 4) 
                                { 
                                    buttonRequestSparepart.Text = "REQUEST DITOLAK"; 
                                    buttonRequestSparepart.BackColor = AppColors.Danger; 
                                }
                                else 
                                { 
                                    buttonRequestSparepart.Text = "REQUEST DITUTUP"; 
                                    buttonRequestSparepart.BackColor = Color.DarkGray; 
                                }
                            }
                            else
                            {
                                inputSparepart.Enabled = _isVerified;
                                buttonRequestSparepart.Enabled = _isVerified;
                                buttonRequestSparepart.Text = "Request Sparepart";
                            }
                        });
                    }
                }
            }
            catch { /* Ignore */ }
        }

        private void BtnVerify_Click(object sender, EventArgs e)
        {
            string nik = inputNIK.InputValue?.Trim();
            
            if (string.IsNullOrWhiteSpace(nik))
            {
                MessageBox.Show("Masukkan Inisial Teknisi.", "Validasi");
                return;
            }

            if (_currentTicketId < 0)
            {
                var user = ServiceLocator.OfflineRepo.GetUserByNik(nik);
                
                if (user != null)
                {
                    if (user.RoleId != 2)
                    {
                        MessageBox.Show("Hanya teknisi yang dapat melakukan verifikasi.", "Akses Ditolak");
                        return;
                    }

                    int pendingId = (int)Math.Abs(_currentTicketId);
                    var request = ServiceLocator.OfflineRepo.GetPendingTicketById(pendingId);
                    
                    if (request != null)
                    {
                        request.TechnicianNik = nik;
                        request.StartedAt = DateTime.Now;
                        request.StatusId = 2; 
                        request.IsMachineRunning = 0; 
                        
                        ServiceLocator.OfflineRepo.UpdatePendingTicket(pendingId, request);

                        _isVerified = true;
                        _ticketStatus = 2;
                        _isMachineRunning = 0; 

                        AutoClosingMessageBox.Show($"Verifikasi Berhasil (Offline)!\nSelamat bekerja, {user.FullName}.", "Sukses", 2000);
                        UpdateUIState();
                        UpdateMachineStateIndicator(); 
                    }
                }
                else
                {
                    MessageBox.Show("Inisial tidak ditemukan di database offline.", "Gagal Validasi");
                }
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var tech = conn.QueryFirstOrDefault(
                        "SELECT user_id, full_name, nik FROM users WHERE nik = @Nik AND role_id = 2", 
                        new { Nik = nik });
                    
                    if (tech != null)
                    {
                        conn.Execute(
                            "UPDATE tickets SET status_id = 2, technician_id = @Id, started_at = NOW(), is_machine_running = 0 WHERE ticket_id = @TId", 
                            new { Id = tech.user_id, TId = _currentTicketId });
                        
                        _isVerified = true;
                        _ticketStatus = 2;
                        _isMachineRunning = 0; 
                        
                        SaveTimerToDatabase(); 
                        CreateSession((int)tech.user_id); 
                        
                        AddTechnicianChip(tech.nik, tech.full_name, (int)tech.user_id, _activeSessionIds.Last());

                        AutoClosingMessageBox.Show($"Verifikasi Berhasil!\nSelamat bekerja, {tech.full_name}.", "Sukses", 2000);
                        LoadPreviousSessions(); 
                        UpdateUIState();
                        UpdateMachineStateIndicator(); 
                    }
                    else
                    {
                        MessageBox.Show("Inisial tidak ditemukan atau bukan teknisi.", "Akses Ditolak");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void BtnAddTechnician_Click(object sender, EventArgs e)
        {
            string nik = ShowTechnicianEntryDialog();
            
            if (string.IsNullOrWhiteSpace(nik)) return;

            if (_currentTicketId < 0)
            {
                MessageBox.Show("Penambahan teknisi hanya tersedia saat online.", "Info");
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var tech = conn.QueryFirstOrDefault(
                        "SELECT user_id, full_name, nik FROM users WHERE nik = @Nik AND role_id = 2", 
                        new { Nik = nik });
                    
                    if (tech != null)
                    {
                        if (_activeTechnicianIds.Contains((int)tech.user_id))
                        {
                            MessageBox.Show($"Teknisi {tech.full_name} sudah aktif.", "Info");
                            return;
                        }

                        CreateSession((int)tech.user_id);
                        AddTechnicianChip(tech.nik, tech.full_name, (int)tech.user_id, _activeSessionIds.Last());

                        AutoClosingMessageBox.Show($"Teknisi {tech.full_name} berhasil ditambahkan.", "Sukses", 2000);
                        LoadPreviousSessions(); 
                    }
                    else
                    {
                        MessageBox.Show("Inisial tidak ditemukan atau bukan teknisi.", "Validasi");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void AddTechnicianChip(string nik, string name, int techId, long sessionId)
        {
            var techInput = new AppInput
            {
                LabelText = "Teknisi Pendamping",
                InputValue = $"{nik} - {name}",
                InputType = AppInput.InputTypeEnum.Text,
                Width = inputNIK.Width, 
                Enabled = false, 
                Margin = new Padding(0, 0, 0, 10)
            };
            
            techInput.Tag = new { SessionId = sessionId, TechId = techId };
            pnlActiveTechs.Controls.Add(techInput);
            pnlActiveTechs.Visible = true;
        }

        private string ShowTechnicianEntryDialog()
        {
            Form prompt = new Form()
            {
                Width = 350,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Tambah Teknisi",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label textLabel = new Label() 
            { 
                Left = 20, Top = 20, Text = "Masukkan Inisial (NIK):", AutoSize = true, Font = AppFonts.Body 
            };
            
            TextBox textBox = new TextBox() 
            { 
                Left = 20, Top = 50, Width = 290, Font = AppFonts.Body, CharacterCasing = CharacterCasing.Upper 
            };
            
            Button confirmation = new Button() 
            { 
                Text = "Tambahkan", Left = 180, Width = 130, Top = 90, DialogResult = DialogResult.OK, 
                Height = 35, BackColor = AppColors.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat 
            };
            confirmation.FlatAppearance.BorderSize = 0;
            
            Button cancel = new Button() 
            { 
                Text = "Batal", Left = 40, Width = 100, Top = 90, DialogResult = DialogResult.Cancel, 
                Height = 35, FlatStyle = FlatStyle.Flat 
            };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : "";
        }

        private void buttonRequestSparepart_Click(object sender, EventArgs e)
        {
            if (!_isVerified) return;
            
            string val = inputSparepart.InputValue?.Trim();
            
            if (string.IsNullOrWhiteSpace(val))
            {
                MessageBox.Show("Isi detail sparepart.", "Validasi");
                return;
            }

            if (MessageBox.Show("Lanjutkan request sparepart?", "Konfirmasi", MessageBoxButtons.YesNo) != DialogResult.Yes)
            {
                return;
            }

            if (_currentTicketId < 0)
            {
                try
                {
                    int pendingId = (int)Math.Abs(_currentTicketId);
                    var request = ServiceLocator.OfflineRepo.GetPendingTicketById(pendingId);
                    
                    if (request != null)
                    {
                        if (request.SparepartRequests == null)
                        {
                            request.SparepartRequests = new List<string>();
                        }
                        
                        request.SparepartRequests.Add(val);
                        ServiceLocator.OfflineRepo.UpdatePendingTicket(pendingId, request);
                        
                        inputSparepart.Enabled = false;
                        buttonRequestSparepart.Enabled = false;
                        buttonRequestSparepart.BackColor = Color.Gray;
                        
                        AutoClosingMessageBox.Show("Request tersimpan (Offline).\nAkan dikirim saat online.", "Sukses", 2000);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error menyimpan offline: {ex.Message}", "Error");
                }
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    int? partId = null;
                    
                    if (val.Contains(" - "))
                    {
                        var parts = val.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            partId = conn.QueryFirstOrDefault<int?>(
                                "SELECT part_id FROM parts WHERE part_code = @C", 
                                new { C = parts[0].Trim() });
                        }
                    }
                    
                    if (partId == null)
                    {
                        partId = conn.QueryFirstOrDefault<int?>(
                            "SELECT part_id FROM parts WHERE part_name = @N", 
                            new { N = val });
                    }
                    
                    conn.Execute(
                        "INSERT INTO part_requests (ticket_id, part_id, part_name_manual, qty, status_id, requested_at) VALUES (@TId, @PId, @Name, 1, 1, NOW())", 
                        new { TId = _currentTicketId, PId = partId, Name = val });
                    
                    inputSparepart.Enabled = false;
                    buttonRequestSparepart.Enabled = false;
                    buttonRequestSparepart.BackColor = Color.Gray;
                    
                    AutoClosingMessageBox.Show("Request terkirim.", "Sukses", 2000);
                    UpdatePartRequestStatus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (!_isVerified)
            {
                MessageBox.Show("Verifikasi teknisi terlebih dahulu.", "Validasi");
                return;
            }
            
            if (!chk4M.Checked && !chkTidak4M.Checked)
            {
                MessageBox.Show("Pilih opsi Pergantian Blade/Crimping Dies.", "Validasi");
                return;
            }
            
            foreach (var prob in _problemControls)
            {
                if (!prob.InputProblemType.ValidateInput() || 
                    !prob.InputProblemDetail.ValidateInput() ||
                    !prob.InputCause.ValidateInput() || 
                    !prob.InputAction.ValidateInput())
                {
                    MessageBox.Show("Lengkapi semua detail perbaikan.", "Validasi");
                    return;
                }
            }

            if (chk4M.Checked && string.IsNullOrWhiteSpace(inputCounter.InputValue))
            {
                inputCounter.SetError("Wajib diisi jika 4M.");
                return;
            }
            
            if (ratingOperator.Rating == 0)
            {
                MessageBox.Show("Beri rating.", "Validasi");
                return;
            }

            _ticketStatus = 4; 
            _timer.Stop();
            SaveTimerToDatabase(); 

            if (_currentTicketId > 0)
            {
                try 
                {
                    using (var conn = DatabaseHelper.GetConnection()) 
                    {
                        conn.Execute(
                            "UPDATE tickets SET status_id = 4 WHERE ticket_id = @Id", 
                            new { Id = _currentTicketId });
                    }
                } 
                catch (Exception ex) 
                {
                    MessageBox.Show("Gagal update status inspeksi: " + ex.Message);
                }
            }
            
            _timer.Start(); 

            bool isApproved = false;

            using (Form prompt = new Form())
            {
                prompt.Width = 350; 
                prompt.Height = 180; 
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Inspeksi Perbaikan";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.ControlBox = false; 

                Label lblInfo = new Label() 
                { 
                    Left = 0, Top = 15, Width = 350, TextAlign = ContentAlignment.MiddleCenter, 
                    Text = "Perbaikan sudah sesuai?", Font = AppFonts.Subtitle 
                };
                
                Label lblTimer = new Label() 
                { 
                    Left = 0, Top = 45, Width = 350, TextAlign = ContentAlignment.MiddleCenter, 
                    Text = "00:00:00", Font = new Font(AppFonts.Subtitle.FontFamily, 14, FontStyle.Bold), 
                    ForeColor = Color.DarkOrange 
                };

                Button btnApprove = new Button() 
                { 
                    Text = "OK", Left = 20, Width = 140, Top = 90, Height = 40, 
                    BackColor = AppColors.Success, ForeColor = Color.White, FlatStyle = FlatStyle.Flat 
                };
                btnApprove.FlatAppearance.BorderSize = 0;
                
                Button btnReject = new Button() 
                { 
                    Text = "NG", Left = 170, Width = 140, Top = 90, Height = 40, 
                    BackColor = AppColors.Danger, ForeColor = Color.White, FlatStyle = FlatStyle.Flat 
                };
                btnReject.FlatAppearance.BorderSize = 0;

                Timer popupTimer = new Timer() { Interval = 1000 };
                popupTimer.Tick += (s, ev) => 
                {
                    lblTimer.Text = TimeSpan.FromSeconds(_inspectionSeconds).ToString(@"hh\:mm\:ss");
                };

                btnApprove.Click += (s, ev) => 
                { 
                    isApproved = true; 
                    popupTimer.Stop(); 
                    prompt.DialogResult = DialogResult.OK; 
                };

                btnReject.Click += (s, ev) => 
                {
                    isApproved = false; 
                    popupTimer.Stop(); 
                    prompt.DialogResult = DialogResult.OK; 
                };

                prompt.Controls.AddRange(new Control[] { lblInfo, lblTimer, btnApprove, btnReject });
                
                popupTimer.Start(); 
                prompt.ShowDialog(); 
                popupTimer.Dispose(); 
            }

            _timer.Stop(); 
            SaveTimerToDatabase(); 

            if (isApproved)
            {
                ProcessFinalSaveAndRun(); 
            }
            else
            {
                _ticketStatus = 2;
                if (_currentTicketId > 0) 
                {
                    try 
                    {
                        using (var conn = DatabaseHelper.GetConnection()) 
                        {
                            conn.Execute(
                                "UPDATE tickets SET status_id = 2 WHERE ticket_id = @Id", 
                                new { Id = _currentTicketId });
                        }
                    } 
                    catch { /* ignore */ }
                }
                
                MessageBox.Show(
                    "Perbaikan dinilai NG oleh inspektur. Silakan perbaiki mesin kembali.", 
                    "Inspeksi Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
                _timer.Start(); 
            }
        }

        private void ProcessFinalSaveAndRun()
        {
            if (_currentTicketId < 0)
            {
                try
                {
                    int pendingId = (int)Math.Abs(_currentTicketId);
                    var request = ServiceLocator.OfflineRepo.GetPendingTicketById(pendingId);
                    
                    if (request != null)
                    {
                        request.StatusId = 3; 
                        request.FinishedAt = DateTime.Now;
                        request.CounterStroke = int.TryParse(inputCounter.InputValue, out int cnt) ? cnt : 0;
                        request.Is4M = chk4M.Checked;
                        request.TechRatingScore = ratingOperator.Rating;
                        request.TechRatingNote = inputOperatorNote.InputValue;
                        
                        for (int i = 0; i < _problemControls.Count && i < request.Problems.Count; i++)
                        {
                            request.Problems[i].CauseName = _problemControls[i].InputCause.InputValue;
                            request.Problems[i].ActionName = _problemControls[i].InputAction.InputValue;
                            request.Problems[i].ProblemTypeName = _problemControls[i].InputProblemType.InputValue;
                            request.Problems[i].FailureName = _problemControls[i].InputProblemDetail.InputValue;
                        }
                        
                        ServiceLocator.OfflineRepo.UpdatePendingTicket(pendingId, request);
                        
                        _timer.Stop();
                        TimeSpan repairDuration = TimeSpan.FromSeconds(_repairSeconds);
                        
                        AutoClosingMessageBox.Show(
                            $"Perbaikan Selesai (Offline)!\nDurasi: {repairDuration:hh\\:mm\\:ss}\n\nData akan disinkronkan saat online.",
                            "Sukses", 2000);
                        
                        var runForm = new MachineRunForm(_currentTicketId);
                        
                        if (runForm.ShowDialog() == DialogResult.OK)
                        {
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else
                        {
                            _ticketStatus = 2; 
                            _timer.Start();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Data tiket tidak ditemukan.", "Error");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error menyimpan offline: {ex.Message}", "Error");
                }
                return;
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            string sql = @"
                                UPDATE tickets 
                                SET status_id = 3, 
                                    technician_finished_at = NOW(), 
                                    counter_stroke = @Cnt, 
                                    is_4m = @Is4M, 
                                    tech_rating_score = @Sc, 
                                    tech_rating_note = @Nt
                                WHERE ticket_id = @Id";
                            
                            conn.Execute(sql, new 
                            { 
                                Cnt = int.TryParse(inputCounter.InputValue, out int c) ? c : 0, 
                                Is4M = chk4M.Checked ? 1 : 0, 
                                Sc = ratingOperator.Rating, 
                                Nt = inputOperatorNote.InputValue,
                                Id = _currentTicketId 
                            }, trans);

                            string detailSql = @"
                                UPDATE ticket_problems SET 
                                    problem_type_id = @TId, problem_type_remarks = @TRem,
                                    failure_id = @FId, failure_remarks = @FRem,
                                    root_cause_id = @CId, root_cause_remarks = @CRem, 
                                    action_id = @AId, action_details_manual = @ARem 
                                WHERE problem_id = @PId";
                            
                            foreach (var prob in _problemControls)
                            {
                                int? tId = GetOrCreateMasterData(conn, trans, "problem_types", "type_id", "type_name", prob.InputProblemType.InputValue);
                                int? fId = GetOrCreateMasterData(conn, trans, "failures", "failure_id", "failure_name", prob.InputProblemDetail.InputValue);
                                int? cId = GetOrCreateMasterData(conn, trans, "failure_causes", "cause_id", "cause_name", prob.InputCause.InputValue);
                                int? aId = GetOrCreateMasterData(conn, trans, "actions", "action_id", "action_name", prob.InputAction.InputValue);
                                
                                conn.Execute(detailSql, new 
                                {
                                    TId = tId, TRem = (string)null, 
                                    FId = fId, FRem = (string)null,
                                    CId = cId, CRem = (string)null,
                                    AId = aId, ARem = (string)null,
                                    PId = prob.ProblemId
                                }, trans);
                            }
                            
                            // [BARU] Auto-Resolve Patroli NG items for this machine
                            // [MODIFIKASI] Auto-Resolve HANYA item NG yang sedang diperbaiki
                            if (_patrolDetailId > 0)
                            {
                                string resolveNgSql = @"
                                    UPDATE patrol_log_details 
                                    SET status = 'PERBAIKAN_OK'
                                    WHERE detail_id = @DetailId AND status IN ('NOT_OK', 'NG')";
                                      
                                conn.Execute(resolveNgSql, new { DetailId = _patrolDetailId }, trans);
                            }
                            
                            trans.Commit();
                            _timer.Stop();
                            SaveTimerToDatabase(); 
                            EndSessionAsCompleted(); 
                            
                            TimeSpan repairDuration = TimeSpan.FromSeconds(_repairSeconds);
                            
                            AutoClosingMessageBox.Show(
                                $"Perbaikan Selesai!\nDurasi: {repairDuration:hh\\:mm\\:ss}", 
                                "Sukses", 2000);
                            
                            var runForm = new MachineRunForm(_currentTicketId);
                            
                            if (runForm.ShowDialog() == DialogResult.OK)
                            {
                                this.DialogResult = DialogResult.OK;
                                _allowClose = true; 
                                this.Close();
                            }
                            else
                            {
                                _ticketStatus = 2; 
                                _timer.Start();
                            }
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void PanelFooter_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(AppColors.Separator))
            {
                e.Graphics.DrawLine(pen, 0, 0, panelFooter.Width, 0);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }

            _timer?.Stop();
            SaveTimerToDatabase(); 
            SaveSession(); 
            _timer?.Dispose();
            
            base.OnFormClosing(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
    
            if (panelHeader != null && lblMachineState != null && lblMachineStateTitle != null)
            {
                int centerX = panelHeader.ClientSize.Width / 2;
                lblMachineStateTitle.Location = new Point(centerX - lblMachineStateTitle.Width / 2, 10);
                lblMachineState.Location = new Point(centerX - lblMachineState.Width / 2, 35);
            }
            
            if (mainLayout == null) return;
                    
            int contentWidth = mainLayout.ClientSize.Width - 80;
            if (contentWidth < 300) contentWidth = 300;
            
            foreach (Control c in mainLayout.Controls)
            {
                if (c is AppInput || c is AppButton || c == pnlProblems || c is Panel || c is FlowLayoutPanel || c == btnAddProblem)
                {
                    c.Width = contentWidth;
                }
            }
            
            if (pnlProblems != null)
            {
                foreach (Control child in pnlProblems.Controls)
                {
                    child.Width = contentWidth;
                }
            }

            if (pnlActiveTechs != null)
            {
                foreach (Control child in pnlActiveTechs.Controls)
                {
                    child.Width = contentWidth;
                }
            }

            if (panelFooter != null && buttonRequestSparepart != null && buttonRepairComplete != null)
            {
                int footerPad = 20;
                int gap = 20;
                int availableWidth = panelFooter.ClientSize.Width - (footerPad * 2) - gap;
                int btnWidth = availableWidth / 2;
                int btnHeight = panelFooter.Height - 30;
                int btnY = (panelFooter.Height - btnHeight) / 2;

                buttonRequestSparepart.Location = new Point(footerPad, btnY);
                buttonRequestSparepart.Size = new Size(btnWidth, btnHeight);

                buttonRepairComplete.Location = new Point(panelFooter.ClientSize.Width - footerPad - btnWidth, btnY);
                buttonRepairComplete.Size = new Size(btnWidth, btnHeight);
            }
        }

        private void LoadActiveSessions()
        {
            if (_currentTicketId <= 0) return;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT tts.session_id, tts.technician_id, tts.elapsed_seconds, 
                               u.nik, u.full_name
                        FROM ticket_technician_sessions tts
                        JOIN users u ON tts.technician_id = u.user_id
                        WHERE tts.ticket_id = @Id AND tts.ended_at IS NULL
                        ORDER BY tts.started_at ASC";
                    
                    var activeSessions = conn.Query(sql, new { Id = _currentTicketId });
                    
                    if (activeSessions.Any())
                    {
                        bool isFirst = true;
                        
                        foreach (var s in activeSessions)
                        {
                            long sId = (long)s.session_id;
                            int tId = (int)s.technician_id;
                            int elapsed = (int)s.elapsed_seconds;
                            
                            if (!_activeSessionIds.Contains(sId))
                            {
                                _activeSessionIds.Add(sId);
                                _activeTechnicianIds.Add(tId);
                                _sessionElapsedMap[sId] = elapsed;
                                
                                AddTechnicianChip((string)s.nik, (string)s.full_name, tId, sId);
                            }

                            if (isFirst)
                            {
                                _isVerified = true;
                                inputNIK.InputValue = (string)s.nik;
                                isFirst = false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FormTechnician] Error active sessions: {ex.Message}");
            }
        }

        private int? GetOrCreateMasterData(IDbConnection conn, IDbTransaction trans, string tableName, string idCol, string nameCol, string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue)) return null;

            string formattedValue = FormatInputText(rawValue);

            string checkSql = $"SELECT {idCol} FROM {tableName} WHERE {nameCol} = @Name";
            var existingId = conn.QueryFirstOrDefault<int?>(checkSql, new { Name = formattedValue }, trans);

            if (existingId.HasValue)
            {
                return existingId.Value; 
            }

            string insertSql = $"INSERT INTO {tableName} ({nameCol}) VALUES (@Name); SELECT LAST_INSERT_ID();";
            int newId = conn.ExecuteScalar<int>(insertSql, new { Name = formattedValue }, trans);

            return newId; 
        }

        private string FormatInputText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            var words = input.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];

                if (word.Equals("aus", StringComparison.OrdinalIgnoreCase))
                {
                    words[i] = "Aus";
                }
                else if (word.Length >= 2 && word.Length <= 3)
                {
                    words[i] = word.ToUpper();
                }
                else
                {
                    words[i] = char.ToUpper(word[0]) + word.Substring(1).ToLower();
                }
            }

            return string.Join(" ", words);
        }
    }
}