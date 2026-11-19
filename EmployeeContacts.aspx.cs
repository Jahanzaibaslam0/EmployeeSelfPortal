using System;
using HRMS.Services;

namespace HRMS
{
    public partial class EmployeeContactsPage : AppBasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Auth.IsAdmin && !Perms.CanRead("ContactMaster"))
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = "Employee / Staff Contacts";
            new ContactHubService().EnsureSchema();
        }
    }
}
