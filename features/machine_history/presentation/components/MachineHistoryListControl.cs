using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.features.machine_history.data.dtos;
using mtc_app.shared.presentation.styles; // Utilizing Shared Styles

namespace mtc_app.features.machine_history.presentation.components
{
    public class MachineHistoryListControl : UserControl
    {
        private DataGridView _grid;

        // Event for item click
        public event EventHandler<MachineHistoryDto> ItemClicked;

        public MachineHistoryListControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this._grid = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this.SuspendLayout();
            
            // 
            // _grid
            // 
            this._grid.Dock = DockStyle.Fill;
            this._grid.BackgroundColor = AppColors.CardBackground;
            this._grid.BorderStyle = BorderStyle.None;
            this._grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            this._grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            this._grid.EnableHeadersVisualStyles = false;
            this._grid.ReadOnly = true;
            this._grid.RowHeadersVisible = false;
            this._grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this._grid.AllowUserToAddRows = false;
            this._grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this._grid.RowTemplate.Height = 40;
            
            // Styling
            this._grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            this._grid.ColumnHeadersDefaultCellStyle.ForeColor = AppColors.TextSecondary;
            this._grid.ColumnHeadersDefaultCellStyle.Font = AppFonts.Caption;
            this._grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(10);
            this._grid.ColumnHeadersHeight = 50;

            this._grid.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            this._grid.DefaultCellStyle.Font = AppFonts.BodySmall;
            this._grid.DefaultCellStyle.Padding = new Padding(10, 5, 10, 5);
            this._grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
            this._grid.DefaultCellStyle.SelectionForeColor = AppColors.TextPrimary;

            // Columns
            _grid.Columns.Add(CreateColumn("TicketCode", "Kode", 100));
            
            var colDate = CreateColumn("CreatedAt", "Tanggal", 150);
            colDate.DefaultCellStyle.Format = "dd MMM HH:mm";
            _grid.Columns.Add(colDate);

            _grid.Columns.Add(CreateColumn("MachineName", "Mesin", 150));
            _grid.Columns.Add(CreateColumn("Issue", "Masalah", 250));
            _grid.Columns.Add(CreateColumn("TechnicianName", "Teknisi", 150));
            _grid.Columns.Add(CreateColumn("StatusName", "Status", 100));

            // Hidden column for StatusId (used for color formatting)
            var colStatusId = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "StatusId",
                Name = "colStatusId",
                Visible = false
            };
            _grid.Columns.Add(colStatusId);

            // Events
            this._grid.CellFormatting += Grid_CellFormatting;
            this._grid.CellDoubleClick += Grid_CellDoubleClick;

            this.Controls.Add(this._grid);
            this.Size = new Size(800, 500);
            this.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
        }

        private DataGridViewColumn CreateColumn(string dataPropertyName, string headerText, int minWidth)
        {
            return new DataGridViewTextBoxColumn
            {
                DataPropertyName = dataPropertyName,
                HeaderText = headerText,
                MinimumWidth = minWidth,
                Name = "col" + dataPropertyName
            };
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = _grid.Rows[e.RowIndex];
            var statusCell = row.Cells["colStatusId"];
            
            if (statusCell?.Value == null) return;

            int statusId = Convert.ToInt32(statusCell.Value);

            // Apply row background color based on status
            Color backColor;
            switch (statusId)
            {
                case 1: // Waiting
                    backColor = Color.FromArgb(255, 251, 235); // Light Yellow
                    break;
                case 2: // Repairing
                    backColor = Color.FromArgb(254, 243, 199); // Amber
                    break;
                case 3: // Done
                    backColor = Color.FromArgb(236, 253, 245); // Light Green
                    break;
                default:
                    backColor = AppColors.CardBackground;
                    break;
            }

            row.DefaultCellStyle.BackColor = backColor;
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = _grid.Rows[e.RowIndex];
            var item = row.DataBoundItem as MachineHistoryDto;

            if (item != null)
            {
                ItemClicked?.Invoke(this, item);
            }
        }

        public void SetData(IEnumerable<MachineHistoryDto> data)
        {
            // Binding List allows automatic UI updates if needed, though simpler List works for ReadOnly
            _grid.DataSource = new List<MachineHistoryDto>(data);
            _grid.ClearSelection();
        }
    }
}

