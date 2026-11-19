using System;
using System.Collections.Generic;
using HRMS.Services;

namespace HRMS
{
    public partial class ProfileChangeHistoryPage : AppBasePage
    {
        private readonly ProfileChangeRequestService _svc = new ProfileChangeRequestService();
        private readonly EmployeeProfileAccessService _profile = new EmployeeProfileAccessService();

        public string PageTitle => "My profile requests";
        public List<ProfileChangeRequest> Requests { get; private set; } = new List<ProfileChangeRequest>();
        public ProfileChangeRequest Detail { get; private set; }
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;
            _svc.EnsureSchema();

            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;

            var empId = _profile.GetLinkedEmployeeId() ?? 0;
            if (empId <= 0 && !_svc.CanReview())
            {
                AlertMessage = "No employee profile is linked to your account.";
                AlertType = "warning";
                return;
            }

            if (empId > 0)
                Requests = _svc.ListForEmployee(empId);
            else
                Requests = _svc.ListPendingForHr();

            var qid = QueryInt("id");
            if (qid.HasValue && qid.Value > 0)
            {
                Detail = _svc.GetRequest(qid.Value);
                if (Detail != null && !_svc.CanViewRequest(Detail))
                    Detail = null;
            }
        }
    }
}
