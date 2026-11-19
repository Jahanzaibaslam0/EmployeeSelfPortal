using System;
using System.Collections.Generic;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class AccountSettingsPage : AppBasePage
    {
        private readonly AccountSecurityService _security = new AccountSecurityService();

        public string PageTitle => "Account Settings";
        public AccountProfile Account { get; private set; }
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "success";
        public string PendingSecret { get; private set; } = "";
        public string PendingOtpAuth { get; private set; } = "";
        public List<string> NewRecoveryCodes { get; private set; }
        public bool MustChange { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (!Auth.CurrentUserId.HasValue)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            _security.EnsureSchema();

            if (IsPostBack)
            {
                HandlePost();
                return;
            }

            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            MustChange = QueryInt("mustChange") == 1 || Auth.MustChangePassword;
            if (MustChange && string.IsNullOrEmpty(AlertMessage))
            {
                AlertMessage = "You must set a new password before using other pages.";
                AlertType = "warning";
            }
            Bind();
        }

        private void Bind()
        {
            Account = _security.GetProfile(Auth.CurrentUserId.Value);
            PendingSecret = SessionHelper.GetString(Session, "PendingTotpSecret");
            if (!string.IsNullOrEmpty(PendingSecret) && Account != null)
                PendingOtpAuth = TotpHelper.BuildOtpAuthUri("HRMS", Account.Username, PendingSecret);
            NewRecoveryCodes = Session["NewRecoveryCodes"] as List<string>;
            if (NewRecoveryCodes != null)
                Session.Remove("NewRecoveryCodes");
        }

        private void HandlePost()
        {
            var userId = Auth.CurrentUserId.Value;
            var handler = FormString("__handler");
            if (string.IsNullOrEmpty(handler))
            {
                Bind();
                return;
            }
            string error = null;

            if (handler == "SaveProfile")
                error = _security.UpdateProfile(userId, FormString("fullName"), FormString("email"));
            else if (handler == "ChangePassword")
            {
                error = _security.ChangePassword(userId, FormString("currentPassword"), FormString("newPassword"), FormString("confirmPassword"));
                if (error == null)
                {
                    SessionHelper.SetInt32(Session, "MustChangePassword", 0);
                    SetAlert("Password updated.");
                    Response.Redirect("~/AccountSettings.aspx");
                    return;
                }
            }
            else if (handler == "Begin2FA")
            {
                error = _security.BeginTwoFactorSetup(userId, FormString("begin2faPassword"), out _, out _);
                if (error == null)
                {
                    SetAlert("Scan or enter the setup key in your authenticator app, then confirm with a 6-digit code.", "info");
                    Response.Redirect("~/AccountSettings.aspx");
                    return;
                }
            }
            else if (handler == "Confirm2FA")
            {
                error = _security.ConfirmTwoFactorSetup(userId, FormString("totpCode"), out var codes);
                if (error == null)
                {
                    Session["NewRecoveryCodes"] = codes;
                    SetAlert("Two-factor authentication is on. Save your backup codes now; they will not be shown again.");
                    Response.Redirect("~/AccountSettings.aspx");
                    return;
                }
            }
            else if (handler == "Disable2FA")
            {
                error = _security.DisableTwoFactor(userId, FormString("disable2faPassword"), FormString("totpOrRecovery"));
                if (error == null)
                {
                    SetAlert("Two-factor authentication is off.");
                    Response.Redirect("~/AccountSettings.aspx");
                    return;
                }
            }
            else if (handler == "RegenCodes")
            {
                error = _security.RegenerateRecoveryCodes(userId, FormString("regenPassword"), out var codes);
                if (error == null)
                {
                    Session["NewRecoveryCodes"] = codes;
                    SetAlert("New backup codes generated. Save them now; previous codes no longer work.");
                    Response.Redirect("~/AccountSettings.aspx");
                    return;
                }
            }
            else if (handler == "SaveQuestions")
            {
                var answers = new List<Tuple<string, string>>
                {
                    Tuple.Create(FormString("question1"), FormString("answer1")),
                    Tuple.Create(FormString("question2"), FormString("answer2"))
                };
                error = _security.SaveSecurityQuestions(userId, FormString("questionsPassword"), answers);
                if (error == null)
                {
                    SetAlert("Security questions saved. You can use them to recover your password on this server.");
                    Response.Redirect("~/AccountSettings.aspx");
                    return;
                }
            }
            else
                error = "Unknown action.";

            SetAlert(error ?? "Saved.", string.IsNullOrEmpty(error) ? "success" : "error");
            Response.Redirect("~/AccountSettings.aspx");
        }
    }
}
