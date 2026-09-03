using System;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class UserProfilePage : AppBasePage
    {
        private readonly EmployeeProfileAccessService _profileAccess = new EmployeeProfileAccessService();

        public string PageTitle => "My Profile";
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "info";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (!_profileAccess.CanAccessEmployeeMasterPage())
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            var employeeId = _profileAccess.GetLinkedEmployeeId();
            if (employeeId.HasValue && employeeId.Value > 0)
            {
                Response.Redirect("~/EmployeeMaster.aspx?editId=" + employeeId.Value);
                return;
            }

            if (Session["Alert"] != null)
            {
                AlertMessage = Session["Alert"]?.ToString() ?? "";
                AlertType = Session["AlertType"]?.ToString() ?? "info";
                Session.Remove("Alert");
                Session.Remove("AlertType");
            }
            else
            {
                AlertMessage = "No employee record is linked to your user account. Please contact HR to assign your User ID in Employee Master.";
                AlertType = "warning";
            }
        }
    }
}
