using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.stock.presentation.components
{
    public partial class StockRequestCardControl : UserControl
    {
        // Data Properties
        public int RequestId { get; private set; }
        public string PartName { get; private set; }

        // Events
        public event EventHandler<int> OnReadyClicked;

        public StockRequestCardControl(int requestId, string partName, string technicianName, DateTime requestedAt)
        {
            InitializeComponent();

            // Store data
            this.RequestId = requestId;
            this.PartName = partName;

            // Set display values
            lblPartName.Text = partName;
            lblTechnicianName.Text = $"By: {technicianName}";
            lblRequestedAt.Text = requestedAt.ToString("dd MMM, HH:mm");

            btnReady.Click += (sender, e) => OnReadyClicked?.Invoke(this, this.RequestId);
        }
    }
}