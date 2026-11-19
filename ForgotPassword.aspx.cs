using System;
using System.Collections.Generic;
using HRMS.Services;

namespace HRMS
{
    public partial class ForgotPasswordPage : AppBasePage
    {
        private readonly AccountSecurityService _security = new AccountSecurityService();

        protected override bool IsPublicPage => true;

        public string ErrorMessage { get; private set; } = "";
        public string InfoMessage { get; private set; } = "";
        public string Step { get; private set; } = "username";
        public List<SecurityQuestionRow> Questions { get; private set; } = new List<SecurityQuestionRow>();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Auth.IsLoggedIn)
            {
                Response.Redirect("~/Home.aspx");
                return;
            }

            _security.EnsureSchema();
            Step = SessionHelper.GetString(Session, "RecoveryStep");
            if (string.IsNullOrEmpty(Step)) Step = "username";

            if (string.Equals(Step, "verify", StringComparison.OrdinalIgnoreCase))
                Questions = Session["RecoveryQuestions"] as List<SecurityQuestionRow> ?? _security.GetDummyQuestions();

            if (IsPostBack)
                HandlePost();
        }

        private void HandlePost()
        {
            var handler = FormString("__handler");
            var ip = Request.UserHostAddress ?? "";

            if (handler == "Start")
            {
                var username = FormString("username");
                if (_security.IsThrottled(username, ip))
                {
                    ErrorMessage = "If this account exists, recovery is temporarily limited. Try again later or contact an administrator.";
                    Step = "username";
                    return;
                }
                var userId = _security.FindUserIdByUsername(username);
                SessionHelper.SetInt32(Session, "RecoveryUserID", userId ?? 0);
                SessionHelper.SetString(Session, "RecoveryUsername", username);
                SessionHelper.SetString(Session, "RecoveryStep", "verify");
                if (userId.HasValue)
                {
                    var qs = _security.GetQuestionsForUser(userId.Value);
                    Questions = qs.Count >= 2 ? qs : _security.GetDummyQuestions();
                }
                else
                    Questions = _security.GetDummyQuestions();
                Session["RecoveryQuestions"] = Questions;
                Step = "verify";
                InfoMessage = "Answer your recovery questions or enter a backup code. If you never set these up, ask an administrator to reset your password in User Setup.";
                return;
            }

            if (handler == "Verify")
            {
                var userId = SessionHelper.GetInt32(Session, "RecoveryUserID") ?? 0;
                var backup = FormString("backupCode");
                var ok = false;
                if (userId > 0)
                {
                    if (!string.IsNullOrWhiteSpace(backup))
                        ok = _security.ConsumeRecoveryCode(userId, backup);
                    if (!ok)
                    {
                        var answers = new List<Tuple<string, string>>();
                        foreach (var q in Session["RecoveryQuestions"] as List<SecurityQuestionRow> ?? new List<SecurityQuestionRow>())
                            answers.Add(Tuple.Create(q.QuestionKey, FormString("ans_" + q.QuestionKey)));
                        ok = _security.VerifySecurityAnswers(userId, answers);
                    }
                }
                if (!ok)
                {
                    ErrorMessage = "Identity could not be verified. Contact an administrator if you cannot recover this account.";
                    Step = "verify";
                    Questions = Session["RecoveryQuestions"] as List<SecurityQuestionRow> ?? _security.GetDummyQuestions();
                    _security.RecordFailedAttempt(SessionHelper.GetString(Session, "RecoveryUsername"), ip);
                    Audit.Log("PasswordRecoveryFailed", "User", userId > 0 ? (int?)userId : null, null, "ForgotPassword", "/ForgotPassword", "Verify", "Recovery failed", success: false);
                    return;
                }
                SessionHelper.SetString(Session, "RecoveryStep", "reset");
                SessionHelper.SetInt32(Session, "RecoveryVerifiedUserID", userId);
                Step = "reset";
                return;
            }

            if (handler == "Reset")
            {
                var userId = SessionHelper.GetInt32(Session, "RecoveryVerifiedUserID") ?? 0;
                if (userId <= 0 || SessionHelper.GetString(Session, "RecoveryStep") != "reset")
                {
                    ClearRecovery();
                    Response.Redirect("~/ForgotPassword.aspx");
                    return;
                }
                var error = _security.ResetPasswordAfterRecovery(userId, FormString("newPassword"), FormString("confirmPassword"));
                if (!string.IsNullOrEmpty(error))
                {
                    ErrorMessage = error;
                    Step = "reset";
                    return;
                }
                ClearRecovery();
                InfoMessage = "Password updated. Sign in with your new password.";
                Step = "done";
            }
        }

        private void ClearRecovery()
        {
            Session.Remove("RecoveryStep");
            Session.Remove("RecoveryUserID");
            Session.Remove("RecoveryVerifiedUserID");
            Session.Remove("RecoveryQuestions");
            Session.Remove("RecoveryUsername");
        }
    }
}
