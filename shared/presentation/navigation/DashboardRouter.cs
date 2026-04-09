using System;
using System.Windows.Forms;
using mtc_app.shared.data.dtos;

// We need to reference feature namespaces. Since this is a monolith, it's fine.
// In a strict layered architecture, this might be composed via DI or reflection.
using mtc_app.features.admin.presentation.screens;
using mtc_app.features.technician.presentation.screens;
using mtc_app.features.group_leader.presentation.screens;
using mtc_app.features.stock.presentation.screens;
using mtc_app.features.machine_history.presentation.screens;

namespace mtc_app.shared.presentation.navigation
{
    public static class DashboardRouter
    {
        public static Form GetDashboardForUser(UserDto user)
        {
            if (user == null) return null;

            string role = user.RoleName?.ToLower().Trim() ?? "";

            switch (role)
            {
                case "operator":
                    return new MachineHistoryFormOperator(); // Assuming this is the operator entry point
                
                case "technician":
                    return new TechnicianDashboardForm();
                
                case "stock control":
                case "stock_control":
                case "stock":
                    // Ensure StockDashboardForm exists or handle generic
                    return new StockDashboardForm();
                    
                case "admin":
                case "administrator":
                    return new AdminMainForm();
                    
                case "gl_production":
                case "group leader":
                    return new GroupLeaderDashboardForm();
                    
                default:
                    return null;
            }
        }
    }
}
