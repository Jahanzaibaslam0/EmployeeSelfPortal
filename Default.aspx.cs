using System;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class DefaultPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var auth = new AuthService();
            Response.Redirect(auth.IsLoggedIn ? "~/Home.aspx" : "~/Login.aspx");
        }
    }
}
