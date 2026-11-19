using System;
using System.Collections.Generic;
using HRMS.Services;

namespace HRMS
{
    public partial class ProfileChangeReviewPage : AppBasePage
    {
        private readonly ProfileChangeRequestService _svc = new ProfileChangeRequestService();

        public string PageTitle => "Profile update requests";
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "success";
        public List<ProfileChangeRequest> Pending { get; private set; } = new List<ProfileChangeRequest>();
        public ProfileChangeRequest Detail { get; private set; }
        public bool CanDecide { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;
            _svc.EnsureSchema();

            if (!_svc.CanReview())
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            if (IsPostBack)
            {
                var id = FormInt("requestId");
                var approve = FormString("__handler") == "Approve";
                var error = _svc.Decide(id, approve, FormString("comments"));
                if (!string.IsNullOrEmpty(error))
                    SetAlert(error, "error");
                else
                    SetAlert(approve ? "Request approved. Official profile updated." : "Request rejected. Approved profile unchanged.");
                Response.Redirect("~/ProfileChangeReview.aspx?id=" + id);
                return;
            }

            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            Pending = _svc.ListPendingForHr();
            var qid = QueryInt("id");
            if (qid.HasValue && qid.Value > 0)
            {
                Detail = _svc.GetRequest(qid.Value);
                if (Detail != null && !_svc.CanViewRequest(Detail))
                    Detail = null;
                CanDecide = _svc.CanApproveRequest(Detail);
            }
        }
    }
}
