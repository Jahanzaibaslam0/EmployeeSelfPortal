using System;
using System.Collections.Generic;
using System.Web.UI;
using HRMS.Services;

namespace HRMS.Controls
{
    public partial class AppSidebar : UserControl
    {
        protected List<WorkstationModuleView> Modules { get; private set; } = new List<WorkstationModuleView>();
        protected bool ShowSidebar { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var auth = new AuthService();
            ShowSidebar = auth.IsLoggedIn;
            if (!ShowSidebar) return;
            Modules = WorkstationNav.BuildVisible();
        }
    }
}
