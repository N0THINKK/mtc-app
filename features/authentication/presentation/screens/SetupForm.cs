using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using mtc_app.features.authentication.data.repositories;
using mtc_app.features.authentication.presentation.controllers;
using mtc_app.shared.presentation.components;

namespace mtc_app.features.authentication.presentation.screens
{
    public partial class SetupForm : AppBaseForm, ISetupView
    {
        private readonly SetupController _controller;
        
        private Label lblTemplate;
        private ComboBox cmbTemplate;
        private Timer _typeWatcherTimer;
        private string _lastType = "";

        public SetupForm()
        {
            _controller = new SetupController(this, new SetupRepository());
            InitializeComponent();
            InitTemplateUI();
            
            this.Shown += async (s, e) => await _controller.LoadDropdownDataAsync();
            this.Resize += (s, e) => RepositionControls();
        }

        // ==========================================
        // ISetupView Implementation
        // ==========================================

        public string MachineType => comboMachineType.InputValue?.Trim().ToUpper() ?? "";
        public string MachineArea => comboMachineArea.InputValue?.Trim().ToUpper() ?? "";
        public string MachineNumber => txtMachineNumber.InputValue?.Trim().ToUpper() ?? "";
        public string SelectedTemplateName => cmbTemplate.Text;
        public bool IsTemplateVisible => cmbTemplate.Visible;

        public void ShowTemplates(List<string> templates)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowTemplates(templates)));
                return;
            }
            lblTemplate.Visible = true;
            cmbTemplate.Visible = true;
            cmbTemplate.Items.Clear();
            foreach (var t in templates) cmbTemplate.Items.Add(t);
            if (cmbTemplate.Items.Count > 0) cmbTemplate.SelectedIndex = 0;
            RepositionControls();
        }

        public void HideTemplates()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(HideTemplates));
                return;
            }
            lblTemplate.Visible = false;
            cmbTemplate.Visible = false;
            cmbTemplate.Items.Clear();
            RepositionControls();
        }

        public void PopulateDropdowns(string[] types, string[] areas)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => PopulateDropdowns(types, areas)));
                return;
            }
            comboMachineType.Visible = true;
            comboMachineArea.Visible = true;
            txtMachineNumber.Visible = true;
            
            comboMachineType.SetDropdownItems(types);
            comboMachineArea.SetDropdownItems(areas);

            btnSave.Click -= btnSave_Click;
            btnSave.Click += async (s, e) => await _controller.SaveConfigAsync();
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

        public void CloseForm(bool success)
        {
            if (this.InvokeRequired) { this.BeginInvoke(new Action(() => CloseForm(success))); return; }
            this.DialogResult = success ? DialogResult.OK : DialogResult.Cancel;
            this.Close();
        }

        // ==========================================
        // UI Logic
        // ==========================================

        private void InitTemplateUI()
        {
            lblTemplate = new Label 
            { 
                Text = "Template Checksheet (Khusus AC90/AC95):", 
                AutoSize = true, 
                Visible = false, 
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.DimGray
            };
            
            cmbTemplate = new ComboBox 
            { 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                Visible = false, 
                Font = new Font("Segoe UI", 12F), 
                Size = new Size(300, 35) 
            };

            this.Load += (s, e) => 
            {
                if (txtMachineNumber.Parent != null)
                {
                    txtMachineNumber.Parent.Controls.Add(lblTemplate);
                    txtMachineNumber.Parent.Controls.Add(cmbTemplate);
                    RepositionControls();
                }
            };

            _typeWatcherTimer = new Timer { Interval = 500 };
            _typeWatcherTimer.Tick += (s, e) => 
            {
                string currentType = comboMachineType.InputValue?.Trim().ToUpper() ?? "";
                if (currentType != _lastType)
                {
                    _lastType = currentType;
                    _controller.HandleMachineTypeChange(currentType);
                }
            };
            _typeWatcherTimer.Start();
        }

        private void RepositionControls()
        {
            if (txtMachineNumber == null || btnSave == null || pnlMain == null) return;

            if (lblTemplate.Visible)
            {
                lblTemplate.Location = new Point(txtMachineNumber.Left, txtMachineNumber.Bottom + 15);
                cmbTemplate.Location = new Point(txtMachineNumber.Left, lblTemplate.Bottom + 5);
                btnSave.Top = cmbTemplate.Bottom + 25;
            }
            else
            {
                btnSave.Top = txtMachineNumber.Bottom + 25;
            }

            if (btnExit != null)
            {
                btnExit.Top = btnSave.Bottom + 15;
                btnExit.Left = (pnlMain.Width - btnExit.Width) / 2;
            }
        }

        private void btnSave_Click(object sender, EventArgs e) { }
        private void btnExit_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }
    }
}