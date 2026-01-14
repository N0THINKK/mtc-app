using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;

namespace mtc_app.features.technician.presentation.components
{
    public class TicketCardControl : UserControl
    {
        private Label lblMachineName;
        private Label lblProblem;
        private Label lblTime;

        public TicketCardControl(string machineName, string problem, string timeAgo)
        {
            InitializeComponent();
            this.lblMachineName.Text = machineName;
            this.lblProblem.Text = problem;
            this.lblTime.Text = timeAgo;
        }

        private void InitializeComponent()
        {
            this.lblMachineName = new Label();
            this.lblProblem = new Label();
            this.lblTime = new Label();
            this.SuspendLayout();
            
            // 
            // Main Card Panel
            // 
            this.BackColor = Color.White;
            this.Size = new Size(300, 120);
            this.Margin = new Padding(10);
            this.BorderStyle = BorderStyle.FixedSingle;

            // 
            // Machine Name Label
            // 
            this.lblMachineName.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblMachineName.ForeColor = AppColors.TextPrimary;
            this.lblMachineName.Location = new Point(15, 10);
            this.lblMachineName.AutoSize = true;

            // 
            // Problem Label
            // 
            this.lblProblem.Font = new Font("Segoe UI", 9F);
            this.lblProblem.ForeColor = AppColors.TextSecondary;
            this.lblProblem.Location = new Point(15, 45);
            this.lblProblem.MaximumSize = new Size(270, 40);
            this.lblProblem.AutoSize = true;

            // 
            // Time Ago Label
            // 
            this.lblTime.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblTime.ForeColor = AppColors.Danger;
            this.lblTime.Location = new Point(15, 90);
            this.lblTime.AutoSize = true;

            // Add controls
            this.Controls.Add(this.lblMachineName);
            this.Controls.Add(this.lblProblem);
            this.Controls.Add(this.lblTime);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
