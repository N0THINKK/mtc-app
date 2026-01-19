using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.shared.presentation.components;
using mtc_app.shared.presentation.styles;
using mtc_app.shared.presentation.utils;

namespace mtc_app.features.group_leader.presentation.components
{
    public class GroupLeaderTicketCardControl : UserControl
    {
        private AppCard _card;
        private AppButton _btnAction;
        private DetailItem _detailMachine;
        private DetailItem _detailTech;
        private DetailItem _detailDate;

        public Guid TicketId { get; private set; }
        
        // Output Event (Dumb Component)
        public event EventHandler<Guid> OnValidate;

        public GroupLeaderTicketCardControl(Guid ticketId, string machineName, string technicianName, DateTime createdAt, bool isReviewed)
        {
            this.TicketId = ticketId;
            InitializeComponent(machineName, technicianName, createdAt, isReviewed);
        }

        private void InitializeComponent(string machineName, string techName, DateTime date, bool isReviewed)
        {
            this.Size = new Size(320, 220);
            this.Padding = new Padding(5);
            this.BackColor = Color.Transparent;

            // 1. Main Card
            _card = new AppCard
            {
                Dock = DockStyle.Fill,
                // Title property doesn't exist on AppCard base, so we use DetailItem or Label inside.
            };

            // 2. Details
            // Note: Docking order matters. We add from bottom to top or use panel containers.
            // Stack: [Machine (Top)] -> [Tech] -> [Date] -> [Action (Bottom)]
            
            _detailMachine = new DetailItem { Title = "Machine", Value = machineName, Dock = DockStyle.Top };
            _detailTech = new DetailItem { Title = "Technician", Value = techName ?? "-", Dock = DockStyle.Top };
            _detailDate = new DetailItem { Title = "Date", Value = date.ToString("dd MMM HH:mm"), Dock = DockStyle.Top };

            // 3. Status / Action
            Panel pnlAction = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(0, 5, 0, 0) };
            
            if (isReviewed)
            {
                var lblStatus = new AppLabel 
                { 
                    Text = "Success", 
                    Type = AppLabel.LabelType.Caption, 
                    ForeColor = AppColors.Success,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                pnlAction.Controls.Add(lblStatus);
            }
            else
            {
                _btnAction = new AppButton 
                { 
                    Text = "Validate", 
                    Type = AppButton.ButtonType.Primary,
                    Dock = DockStyle.Fill
                };
                _btnAction.Click += (s, e) => OnValidate?.Invoke(this, this.TicketId);
                pnlAction.Controls.Add(_btnAction);
            }

            // Assemble - Order of addition for Dock.Top is "Last Added is Top-most" usually? 
            // In WinForms, the LAST control added with Dockstyle.Top appears at the TOP.
            // Wait, actually: "The z-order of the controls determines their docking order... The control at the top of the z-order is docked last."
            // `Controls.Add` puts at top of Z-order (index 0).
            // So if I Add(A) then Add(B), B is index 0. B gets docked first?
            // "Controls are docked in reverse z-order." -> Index 0 is docked LAST (closest to center).
            // So to get [Machine] at Top, then [Tech], then [Date]:
            // Add(Machine) -> Z=0. Docked LAST (Inner).
            // Add(Tech) -> Z=0, Machine=1. Tech Docked LAST (Inner).
            // This is confusing. 
            // Correct approach: Add Bottom-docked items first. Then Top-docked items in REVERSE order of appearance (Bottom-most top item first).
            
            _card.Controls.Add(pnlAction);   // Dock Bottom
            _card.Controls.Add(_detailDate); // Dock Top (Inner-most of tops)
            _card.Controls.Add(_detailTech); // Dock Top
            _card.Controls.Add(_detailMachine); // Dock Top (Outer-most)

            this.Controls.Add(_card);
        }
    }
}
