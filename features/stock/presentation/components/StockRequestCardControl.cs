// using System;
// using System.Drawing;
// using System.Windows.Forms;
// using mtc_app.shared.presentation.components;
// using mtc_app.shared.presentation.styles;

// namespace mtc_app.features.stock.presentation.components
// {
//     public partial class StockRequestCardControl : UserControl
//     {
//         public int RequestId { get; private set; }
//         public string PartName { get; private set; }

//         public event EventHandler<int> OnReadyClicked;

//         public StockRequestCardControl(
//             int requestId, 
//             string partName, 
//             string technicianName, 
//             DateTime requestedAt)
//         {
//             InitializeComponent();
//             InitializeCardData(requestId, partName, technicianName, requestedAt);
//             AttachEventHandlers();
//         }

//         private void InitializeCardData(
//             int requestId, 
//             string partName, 
//             string technicianName, 
//             DateTime requestedAt)
//         {
//             this.RequestId = requestId;
//             this.PartName = partName;

//             lblPartName.Text = TruncatePartName(partName);
//             lblTechnicianName.Text = $"👤 By: {technicianName}";
//             lblRequestedAt.Text = $"🕐 {FormatRequestTime(requestedAt)}";
//         }

//         private string TruncatePartName(string partName)
//         {
//             const int maxLength = 60;
            
//             if (string.IsNullOrEmpty(partName))
//                 return "Unknown Part";

//             return partName.Length > maxLength 
//                 ? partName.Substring(0, maxLength) + "..." 
//                 : partName;
//         }

//         private string FormatRequestTime(DateTime requestedAt)
//         {
//             var timeDifference = DateTime.Now - requestedAt;

//             if (timeDifference.TotalMinutes < 60)
//                 return $"{(int)timeDifference.TotalMinutes} menit lalu";
            
//             if (timeDifference.TotalHours < 24)
//                 return $"{(int)timeDifference.TotalHours} jam lalu";

//             return requestedAt.ToString("dd MMM, HH:mm");
//         }

//         private void AttachEventHandlers()
//         {
//             btnReady.Click += HandleReadyButtonClick;
            
//             // Add hover effect
//             pnlCard.MouseEnter += (s, e) => ApplyHoverEffect(true);
//             pnlCard.MouseLeave += (s, e) => ApplyHoverEffect(false);
//         }

//         private void HandleReadyButtonClick(object sender, EventArgs e)
//         {
//             OnReadyClicked?.Invoke(this, this.RequestId);
//         }

//         private void ApplyHoverEffect(bool isHovering)
//         {
//             pnlCard.BackColor = isHovering 
//                 ? Color.FromArgb(245, 247, 250) 
//                 : AppColors.Surface;
//         }
//     }
// }