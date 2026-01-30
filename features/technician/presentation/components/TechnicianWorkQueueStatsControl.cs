using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.utils;

namespace mtc_app.features.technician.presentation.components
{
    public class TechnicianWorkQueueStatsControl : UserControl
    {
        private Panel pnlOpen;
        private Panel pnlRepairing;
        private Panel pnlDone;
        
        private Label lblOpenValue;
        private Label lblOpenLabel;
        private PictureBox iconOpen;
        
        private Label lblRepairValue;
        private Label lblRepairLabel;
        private PictureBox iconRepair;
        
        private Label lblDoneValue;
        private Label lblDoneLabel;
        private PictureBox iconDone;

        public TechnicianWorkQueueStatsControl()
        {
            InitializeComponent();
        }

        public void UpdateStats(int openCount, int repairCount, int doneCount)
        {
            lblOpenValue.Text = openCount.ToString();
            lblRepairValue.Text = repairCount.ToString();
            lblDoneValue.Text = doneCount.ToString();
        }

        private void InitializeComponent()
        {
            this.pnlOpen = new Panel();
            this.pnlRepairing = new Panel();
            this.pnlDone = new Panel();
            
            this.lblOpenValue = new Label();
            this.lblOpenLabel = new Label();
            this.iconOpen = new PictureBox();
            
            this.lblRepairValue = new Label();
            this.lblRepairLabel = new Label();
            this.iconRepair = new PictureBox();
            
            this.lblDoneValue = new Label();
            this.lblDoneLabel = new Label();
            this.iconDone = new PictureBox();

            this.SuspendLayout();
            
            // Main UserControl
            this.BackColor = Color.Transparent;
            this.Size = new Size(900, 100);
            this.Padding = new Padding(0);

            // Setup three stat cards
            SetupStatCard(pnlOpen, iconOpen, lblOpenValue, lblOpenLabel,
                0, "0", "Belum Ditangani", AppColors.Danger, Color.FromArgb(254, 242, 242));

            SetupStatCard(pnlRepairing, iconRepair, lblRepairValue, lblRepairLabel,
                310, "0", "Sedang Diperbaiki", Color.FromArgb(234, 179, 8), Color.FromArgb(254, 252, 232));

            SetupStatCard(pnlDone, iconDone, lblDoneValue, lblDoneLabel,
                620, "0", "Selesai", Color.FromArgb(34, 197, 94), Color.FromArgb(240, 253, 244));

            // Add panels to control
            this.Controls.Add(pnlOpen);
            this.Controls.Add(pnlRepairing);
            this.Controls.Add(pnlDone);

            this.ResumeLayout(false);
        }

        private void SetupStatCard(Panel panel, PictureBox icon, Label valueLabel, Label textLabel,
            int xPosition, string defaultValue, string labelText, Color accentColor, Color bgColor)
        {
            // Panel
            panel.BackColor = Color.White;
            panel.Size = new Size(290, 100);
            panel.Location = new Point(xPosition, 0);
            panel.Paint += (s, e) => DrawStatCard(e.Graphics, panel.ClientRectangle, accentColor);

            // Icon
            icon.Size = new Size(48, 48);
            icon.Location = new Point(20, 26);
            icon.BackColor = Color.Transparent;
            
            if (labelText.Contains("Belum"))
                icon.Paint += (s, e) => DrawAlertIcon(e.Graphics, accentColor);
            else if (labelText.Contains("Sedang"))
                icon.Paint += (s, e) => DrawWrenchIcon(e.Graphics, accentColor);
            else
                icon.Paint += (s, e) => DrawCheckCircleIcon(e.Graphics, accentColor);

            // Value Label
            valueLabel.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
            valueLabel.ForeColor = AppColors.TextPrimary;
            valueLabel.Location = new Point(85, 20);
            valueLabel.AutoSize = true;
            valueLabel.Text = defaultValue;

            // Text Label
            textLabel.Font = new Font("Segoe UI", 10F);
            textLabel.ForeColor = AppColors.TextSecondary;
            textLabel.Location = new Point(85, 58);
            textLabel.AutoSize = true;
            textLabel.Text = labelText;

            // Add controls to panel
            panel.Controls.Add(textLabel);
            panel.Controls.Add(valueLabel);
            panel.Controls.Add(icon);

            // Hover effect
            panel.MouseEnter += (s, e) => {
                panel.BackColor = bgColor;
                panel.Cursor = Cursors.Hand;
            };
            panel.MouseLeave += (s, e) => {
                panel.BackColor = Color.White;
                panel.Cursor = Cursors.Default;
            };
        }

        private void DrawStatCard(Graphics g, Rectangle bounds, Color accentColor)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Card background with rounded corners
            using (GraphicsPath path = GraphicsUtils.GetRoundedRectangle(new Rectangle(0, 0, bounds.Width - 1, bounds.Height - 1), 8))
            {
                g.FillPath(new SolidBrush(Color.White), path);
                g.DrawPath(new Pen(Color.FromArgb(230, 230, 230), 1), path);
            }

            // Top accent line
            using (Pen accentPen = new Pen(accentColor, 3))
            {
                g.DrawLine(accentPen, 8, 0, bounds.Width - 8, 0);
            }
        }

        private void DrawAlertIcon(Graphics g, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 3))
            {
                // Triangle
                g.DrawLine(pen, 24, 4, 44, 40);
                g.DrawLine(pen, 44, 40, 4, 40);
                g.DrawLine(pen, 4, 40, 24, 4);
                // Exclamation
                g.DrawLine(pen, 24, 16, 24, 28);
                g.DrawLine(pen, 24, 32, 24, 34);
            }
        }

        private void DrawWrenchIcon(Graphics g, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 3))
            {
                // Wrench handle
                g.DrawLine(pen, 12, 36, 24, 24);
                // Head
                g.DrawArc(pen, 20, 8, 20, 20, 270, 270); 
            }
        }

        private void DrawCheckCircleIcon(Graphics g, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, 3))
            {
                g.DrawEllipse(pen, 4, 4, 40, 40);
                g.DrawLine(pen, 14, 24, 20, 30);
                g.DrawLine(pen, 20, 30, 34, 16);
            }
        }
    }
}