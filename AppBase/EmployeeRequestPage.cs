using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class EmployeeRequestPage : AppBasePage
    {
        private static readonly string[] AllowedExtensions =
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".txt"
        };

        private readonly EmployeeRequestService _svc = new EmployeeRequestService();

        public string PageTitle => "Employee Requests";
        public List<EmployeeRequestItem> Requests { get; private set; } = new List<EmployeeRequestItem>();
        public List<EmployeeRequestAttachment> Attachments { get; private set; } = new List<EmployeeRequestAttachment>();
        public List<EmployeeRequestAction> Actions { get; private set; } = new List<EmployeeRequestAction>();
        public List<EmployeeRequestAlertItem> Alerts { get; private set; } = new List<EmployeeRequestAlertItem>();
        public EmployeeRequestItem Input { get; private set; } = new EmployeeRequestItem();
        public bool ShowForm { get; private set; }
        public bool EditMode { get; private set; }
        public bool CanApprove { get; private set; }
        public bool CanEditDraft { get; private set; }
        public bool IsHr { get; private set; }
        public bool IsManagement { get; private set; }
        public string Inbox { get; private set; } = "all";
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            Form.Enctype = "multipart/form-data";
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (!_svc.CanAccessModule())
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            _svc.EnsureSchema();
            IsHr = _svc.CanActAsHr();
            IsManagement = _svc.CanActAsManagement();
            Alerts = _svc.GetUnreadAlerts(10);

            if (IsPostBack)
            {
                HandlePost();
                return;
            }

            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;

            Inbox = (Request.QueryString["inbox"] ?? "all").Trim().ToLowerInvariant();
            var editId = QueryInt("editId");
            var isNew = Request.QueryString["new"] == "1";
            ShowForm = isNew || (editId.HasValue && editId.Value > 0);

            if (ShowForm)
            {
                if (editId.HasValue && editId.Value > 0)
                {
                    Input = _svc.GetById(editId.Value);
                    if (Input == null)
                    {
                        SetAlert("Request not found or access denied.", "error");
                        Response.Redirect("~/EmployeeRequest.aspx");
                        return;
                    }
                    EditMode = true;
                    Attachments = _svc.GetAttachments(Input.RequestID);
                    Actions = _svc.GetActions(Input.RequestID);
                    _svc.MarkAlertsReadForRequest(Input.RequestID);
                    CanEditDraft = Input.Status == EmployeeRequestStatuses.Draft
                                   || Input.Status == EmployeeRequestStatuses.ReturnedForCorrection;
                    CanApprove = CanUserApprove(Input);
                }
                else
                {
                    var linked = _svc.GetLinkedEmployeeId();
                    if (!linked.HasValue)
                    {
                        SetAlert(EmployeeProfileAccessService.NotSynchronizedMessage, "warning");
                        Response.Redirect("~/UserProfile.aspx");
                        return;
                    }
                    Input = new EmployeeRequestItem
                    {
                        EmployeeID = linked.Value,
                        RequestCategory = EmployeeRequestCategories.Other,
                        Status = EmployeeRequestStatuses.Draft,
                        Details = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    };
                    CanEditDraft = true;
                }
            }
            else
            {
                Requests = _svc.ListVisible(Inbox == "all" ? null : Inbox);
            }
        }

        private void HandlePost()
        {
            var handler = FormString("__handler");
            var requestId = FormInt("RequestID");
            var comments = FormString("ApprovalComments");

            try
            {
                if (string.Equals(handler, "SaveDraft", StringComparison.OrdinalIgnoreCase))
                {
                    var id = SaveFromForm(submit: false);
                    SetAlert("Draft saved.");
                    Response.Redirect("~/EmployeeRequest.aspx?editId=" + id);
                    return;
                }
                if (string.Equals(handler, "Submit", StringComparison.OrdinalIgnoreCase))
                {
                    var id = SaveFromForm(submit: false);
                    _svc.Submit(id, comments);
                    SetAlert("Request submitted for manager approval.");
                    Response.Redirect("~/EmployeeRequest.aspx?editId=" + id);
                    return;
                }
                if (string.Equals(handler, "Approve", StringComparison.OrdinalIgnoreCase))
                {
                    _svc.Approve(requestId, comments);
                    SetAlert("Request approved and moved to the next stage (or completed).");
                    Response.Redirect("~/EmployeeRequest.aspx?editId=" + requestId);
                    return;
                }
                if (string.Equals(handler, "Reject", StringComparison.OrdinalIgnoreCase))
                {
                    _svc.Reject(requestId, comments);
                    SetAlert("Request rejected.", "warning");
                    Response.Redirect("~/EmployeeRequest.aspx?editId=" + requestId);
                    return;
                }
                if (string.Equals(handler, "Return", StringComparison.OrdinalIgnoreCase))
                {
                    _svc.ReturnForCorrection(requestId, comments);
                    SetAlert("Request returned for correction.", "warning");
                    Response.Redirect("~/EmployeeRequest.aspx?editId=" + requestId);
                    return;
                }
            }
            catch (Exception ex)
            {
                AlertMessage = ex.Message;
                AlertType = "error";
                ShowForm = true;
                EditMode = requestId > 0;
                Input = requestId > 0 ? (_svc.GetById(requestId) ?? BuildInputFromForm(requestId)) : BuildInputFromForm(0);
                if (Input.RequestID > 0)
                {
                    Attachments = _svc.GetAttachments(Input.RequestID);
                    Actions = _svc.GetActions(Input.RequestID);
                }
                CanEditDraft = Input.Status == EmployeeRequestStatuses.Draft
                               || Input.Status == EmployeeRequestStatuses.ReturnedForCorrection
                               || Input.RequestID == 0;
                CanApprove = Input.RequestID > 0 && CanUserApprove(Input);
                return;
            }

            Response.Redirect("~/EmployeeRequest.aspx");
        }

        private int SaveFromForm(bool submit)
        {
            var item = BuildInputFromForm(FormInt("RequestID"));
            var id = _svc.SaveDraft(item);

            var file = Request.Files["supportFile"];
            if (file != null && file.ContentLength > 0)
            {
                var saved = SaveUpload(file, id);
                _svc.AddAttachment(id, saved.Item1, saved.Item2);
            }
            return id;
        }

        private EmployeeRequestItem BuildInputFromForm(int requestId)
        {
            var category = FormString("RequestCategory");
            if (!EmployeeRequestCategories.All.Contains(category))
                category = EmployeeRequestCategories.Other;

            decimal? amount = null;
            if (decimal.TryParse(FormString("Amount"), out var amt)) amount = amt;

            var details = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            void Put(string key, string formKey)
            {
                var v = FormString(formKey);
                if (!string.IsNullOrWhiteSpace(v)) details[key] = v;
            }

            Put("PhoneModel", "PhoneModel");
            Put("ClaimType", "ClaimType");
            Put("HospitalName", "HospitalName");
            Put("Destination", "Destination");
            Put("TravelMode", "TravelMode");
            Put("LoanPurpose", "LoanPurpose");
            Put("VehicleType", "VehicleType");
            Put("VehicleRegNo", "VehicleRegNo");
            Put("FuelLiters", "FuelLiters");
            Put("OtherType", "OtherType");

            return new EmployeeRequestItem
            {
                RequestID = requestId,
                EmployeeID = _svc.GetLinkedEmployeeId() ?? 0,
                RequestCategory = category,
                Subject = FormString("Subject"),
                Description = FormString("Description"),
                Amount = amount,
                StartDate = FormString("StartDate"),
                EndDate = FormString("EndDate"),
                Details = details,
                Status = requestId > 0 ? (FormString("CurrentStatus") ?? EmployeeRequestStatuses.Draft) : EmployeeRequestStatuses.Draft
            };
        }

        private Tuple<string, string> SaveUpload(HttpPostedFile file, int requestId)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("File type not allowed.");

            var folder = Server.MapPath("~/uploads/employee-requests");
            Directory.CreateDirectory(folder);
            var safe = "req_" + requestId + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ext;
            file.SaveAs(Path.Combine(folder, safe));
            return Tuple.Create("/uploads/employee-requests/" + safe, Path.GetFileName(file.FileName));
        }

        private bool CanUserApprove(EmployeeRequestItem item)
        {
            var linked = _svc.GetLinkedEmployeeId() ?? 0;
            if (item.Status == EmployeeRequestStatuses.PendingManagerApproval)
                return Auth.IsAdmin || linked == item.ManagerEmployeeID;
            if (item.Status == EmployeeRequestStatuses.PendingHRApproval)
                return IsHr;
            if (item.Status == EmployeeRequestStatuses.PendingManagementApproval)
                return IsManagement;
            return false;
        }

        public string DetailValue(string key)
        {
            if (Input?.Details == null) return "";
            return Input.Details.TryGetValue(key, out var v) ? v : "";
        }
    }
}
