using System;
using System.Linq;
using HRMS.Services;

namespace HRMS
{
    public partial class ProfileChangeConfirmPage : AppBasePage
    {
        private readonly ProfileChangeRequestService _svc = new ProfileChangeRequestService();

        public string PageTitle => "Confirm profile update";
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "error";
        public ProfileChangeDraft Draft { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;
            _svc.EnsureSchema();
            Draft = _svc.LoadDraft();

            if (IsPostBack)
            {
                var handler = FormString("__handler");
                if (handler == "Cancel")
                {
                    _svc.ClearDraft();
                    Response.Redirect("~/EmployeeMaster.aspx?editId=" + (Draft != null ? Draft.EmployeeID : 0));
                    return;
                }
                if (handler == "Submit")
                {
                    int id;
                    var error = _svc.SubmitDraft(Draft, out id);
                    if (!string.IsNullOrEmpty(error))
                    {
                        AlertMessage = error;
                        return;
                    }
                    _svc.ClearDraft();
                    SetAlert("Request submitted to HR. Your approved profile was not changed.");
                    Response.Redirect("~/ProfileChangeHistory.aspx?id=" + id);
                    return;
                }
            }

            if (Draft == null || Draft.Lines == null || !Draft.Lines.Any(l => !l.IsSnapshot))
            {
                SetAlert("There is no profile change waiting for confirmation.", "warning");
                Response.Redirect("~/UserProfile.aspx");
            }
        }
    }
}
