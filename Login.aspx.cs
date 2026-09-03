using System;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class LoginPage : AppBasePage
    {
        protected System.Web.UI.WebControls.TextBox txtUsername;
        protected System.Web.UI.WebControls.TextBox txtPassword;
        protected System.Web.UI.WebControls.Button btnLogin;

        protected override bool IsPublicPage => true;

        public string ErrorMessage { get; private set; } = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.QueryString["logout"] == "1")
            {
                Audit.LogLogout();
                Auth.Logout();
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (Auth.IsLoggedIn)
                Response.Redirect("~/Home.aspx");
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            var result = Auth.Login(txtUsername.Text, txtPassword.Text);
            if (!result.Success)
            {
                Audit.LogLogin(txtUsername.Text.Trim(), false, message: result.Message);
                ErrorMessage = result.Message;
                return;
            }

            Session["ShowNotificationPopup"] = 1;
            Session["ShowMemorandumPopup"] = 1;
            if (!Auth.IsAdmin && !Auth.LinkedEmployeeId.HasValue)
                Session["ShowProfileSyncWarning"] = 1;

            Response.Redirect("~/Home.aspx");
        }
    }
}
