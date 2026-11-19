using System;
using HRMS.Services;

namespace HRMS
{
    public partial class Verify2FAPage : AppBasePage
    {
        protected override bool IsPublicPage => true;

        public string ErrorMessage { get; private set; } = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Auth.IsLoggedIn)
            {
                Response.Redirect("~/Home.aspx");
                return;
            }
            if (!SessionHelper.GetInt32(Session, "Pending2FAUserID").HasValue)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (IsPostBack)
                Verify();
        }

        private void Verify()
        {
            var code = FormString("code");
            var result = Auth.CompleteTwoFactor(code);
            if (!result.Success)
            {
                Audit.LogLogin(SessionHelper.GetString(Session, "Pending2FAUsername"), false, message: result.Message);
                ErrorMessage = result.Message;
                return;
            }

            Audit.LogLogin(Auth.CurrentUsername, true, Auth.CurrentUserId);
            Session["ShowNotificationPopup"] = 1;
            Session["ShowMemorandumPopup"] = 1;
            if (!Auth.IsAdmin && !Auth.LinkedEmployeeId.HasValue)
                Session["ShowProfileSyncWarning"] = 1;
            if (Auth.MustChangePassword)
            {
                Response.Redirect("~/AccountSettings.aspx?mustChange=1");
                return;
            }
            Response.Redirect("~/Home.aspx");
        }
    }
}
