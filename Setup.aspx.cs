using System;

namespace HRMS
{
    public partial class SetupPage : AppBasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Redirect("~/MasterSetup.aspx", true);
        }
    }
}
