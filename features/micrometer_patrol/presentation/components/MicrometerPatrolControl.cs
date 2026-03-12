using System;
using System.Drawing;
using System.Windows.Forms;
using mtc_app.features.micrometer_patrol.data.dtos;
using mtc_app.features.micrometer_patrol.data.repositories;
using mtc_app.shared.data.session;
using mtc_app.shared.presentation.components; 

namespace mtc_app.features.micrometer_patrol.presentation.components
{
    public partial class MicrometerPatrolControl : UserControl
    {
        // This control is no longer used, kept for compatibility if referenced in designer files.
        public MicrometerPatrolControl()
        {
            InitializeComponent(); 
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "MicrometerPatrolControl";
            this.Size = new Size(150, 150);
            this.ResumeLayout(false);
        }
    }
}
