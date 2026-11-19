using System;
using System.Collections.Generic;
using HRMS.Services;

namespace HRMS
{
    public partial class ContactChangeReviewPage : AppBasePage
    {
        private readonly ContactChangeWorkflowService _svc = new ContactChangeWorkflowService();

        public List<ContactChangeRequest> Pending { get; private set; } = new List<ContactChangeRequest>();
        public List<ContactChangeRequest> Completed { get; private set; } = new List<ContactChangeRequest>();
        public ContactChangeRequest Detail { get; private set; }
        public bool CanDecide { get; private set; }
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Auth.IsAdmin && !Perms.CanRead("ContactMaster") && !_svc.CanReview())
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = "Contact Approvals";
            _svc.EnsureSchema();

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "";
                var id = int.TryParse(Request.Form["requestId"], out var rid) ? rid : 0;
                var comments = Request.Form["comments"] ?? "";
                if (id > 0 && (handler == "Approve" || handler == "Reject"))
                {
                    var err = _svc.Decide(id, handler == "Approve", comments);
                    if (!string.IsNullOrEmpty(err))
                    {
                        AlertMessage = err;
                        AlertType = "error";
                    }
                    else
                    {
                        SetAlert(handler == "Approve" ? "Request approved. Verified status was set by the workflow." : "Request rejected. The record was not marked Verified.");
                        Response.Redirect("~/ContactChangeReview.aspx?id=" + id);
                        return;
                    }
                }
            }

            LoadAlert(out var msg, out var typ);
            if (string.IsNullOrEmpty(AlertMessage))
            {
                AlertMessage = msg;
                AlertType = typ;
            }

            Pending = _svc.CanReview() ? _svc.ListPending() : new List<ContactChangeRequest>();
            var recent = _svc.ListRecent(40);
            Completed = new List<ContactChangeRequest>();
            foreach (var r in recent)
                if (!string.Equals(r.Status, ContactWorkflowStatuses.InProcess, StringComparison.OrdinalIgnoreCase))
                    Completed.Add(r);
            var openId = QueryInt("id");
            if (openId.HasValue && openId.Value > 0)
            {
                Detail = _svc.GetRequest(openId.Value);
                if (Detail != null && !_svc.CanViewRequest(Detail))
                    Detail = null;
                CanDecide = _svc.CanApproveRequest(Detail);
            }
        }
    }
}
