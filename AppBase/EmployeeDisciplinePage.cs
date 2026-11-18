using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class EmployeeDisciplinePage : AppBasePage
    {
        private static readonly string[] AllowedExtensions =
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".txt"
        };

        private readonly DisciplineService _svc = new DisciplineService();

        public string PageTitle => "Employee Discipline & Non-Conformance";
        public List<DisciplineCaseItem> Cases { get; private set; } = new List<DisciplineCaseItem>();
        public List<DisciplineAttachment> Attachments { get; private set; } = new List<DisciplineAttachment>();
        public List<DisciplineAuditItem> AuditTrail { get; private set; } = new List<DisciplineAuditItem>();
        public List<DisciplineFindingItem> Findings { get; private set; } = new List<DisciplineFindingItem>();
        public List<LookupItem> Employees { get; private set; } = new List<LookupItem>();
        public DisciplineCaseItem Input { get; private set; } = new DisciplineCaseItem();
        public bool ShowForm { get; private set; }
        public bool EditMode { get; private set; }
        public bool CanWrite { get; private set; }
        public string StatusFilter { get; private set; } = "all";
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
            CanWrite = _svc.CanWrite();

            if (IsPostBack)
            {
                HandlePost();
                return;
            }

            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;

            StatusFilter = (Request.QueryString["status"] ?? "all").Trim();
            var editId = QueryInt("editId");
            var isNew = Request.QueryString["new"] == "1";
            ShowForm = isNew || (editId.HasValue && editId.Value > 0);

            if (ShowForm)
            {
                if (isNew && !CanWrite)
                {
                    SetAlert(PermissionService.AccessRestrictedMessage, "error");
                    Response.Redirect("~/EmployeeDiscipline.aspx");
                    return;
                }

                Employees = _svc.GetEmployeeLookups();
                if (editId.HasValue && editId.Value > 0)
                {
                    Input = _svc.GetById(editId.Value);
                    if (Input == null)
                    {
                        SetAlert("Case not found or access denied.", "error");
                        Response.Redirect("~/EmployeeDiscipline.aspx");
                        return;
                    }
                    EditMode = true;
                    Attachments = _svc.GetAttachments(Input.DisciplineID);
                    AuditTrail = _svc.GetAudit(Input.DisciplineID);
                    Findings = _svc.GetFindingsForEmployee(Input.EmployeeID);
                }
                else
                {
                    Input = new DisciplineCaseItem
                    {
                        CaseStatus = DisciplineStatuses.Open,
                        ReportedDate = DateTime.Today.ToString("yyyy-MM-dd"),
                        IncludeInPerformance = true
                    };
                }
            }
            else
            {
                Cases = _svc.ListCases(StatusFilter == "all" ? null : StatusFilter);
            }
        }

        private void HandlePost()
        {
            var handler = FormString("__handler");
            try
            {
                if (string.Equals(handler, "Save", StringComparison.OrdinalIgnoreCase))
                {
                    var item = BuildFromForm();
                    var id = _svc.Save(item);
                    var file = Request.Files["evidenceFile"];
                    if (file != null && file.ContentLength > 0)
                    {
                        var saved = SaveUpload(file, id);
                        _svc.AddAttachment(id, saved.Item1, saved.Item2);
                    }
                    SetAlert(item.DisciplineID > 0 ? "Case updated." : "Case created.");
                    Response.Redirect("~/EmployeeDiscipline.aspx?editId=" + id);
                    return;
                }
                if (string.Equals(handler, "AddFinding", StringComparison.OrdinalIgnoreCase))
                {
                    var caseId = FormInt("DisciplineID");
                    var empId = FormInt("EmployeeID");
                    if (empId <= 0 && caseId > 0)
                    {
                        var c = _svc.GetById(caseId);
                        empId = c?.EmployeeID ?? 0;
                    }
                    _svc.AddFinding(new DisciplineFindingItem
                    {
                        EmployeeID = empId,
                        DisciplineID = caseId > 0 ? caseId : (int?)null,
                        FindingSource = FormString("FindingSource"),
                        FindingCategory = FormString("FindingCategory"),
                        Title = FormString("FindingTitle"),
                        Remarks = FormString("FindingRemarksText"),
                        FindingDate = FormString("FindingDate"),
                        Severity = FormString("FindingSeverity"),
                        IncludeInPerformance = FormString("FindingIncludeInPerformance")
                            .IndexOf("true", StringComparison.OrdinalIgnoreCase) >= 0
                            || FormBool("FindingIncludeInPerformance")
                    });
                    SetAlert("Finding / remark added.");
                    Response.Redirect(caseId > 0
                        ? "~/EmployeeDiscipline.aspx?editId=" + caseId
                        : "~/EmployeeDiscipline.aspx");
                    return;
                }
            }
            catch (Exception ex)
            {
                AlertMessage = ex.Message;
                AlertType = "error";
                ShowForm = true;
                CanWrite = _svc.CanWrite();
                Employees = _svc.GetEmployeeLookups();
                Input = BuildFromForm();
                EditMode = Input.DisciplineID > 0;
                if (Input.DisciplineID > 0)
                {
                    Attachments = _svc.GetAttachments(Input.DisciplineID);
                    AuditTrail = _svc.GetAudit(Input.DisciplineID);
                    Findings = _svc.GetFindingsForEmployee(Input.EmployeeID);
                }
                return;
            }

            Response.Redirect("~/EmployeeDiscipline.aspx");
        }

        private DisciplineCaseItem BuildFromForm()
        {
            return new DisciplineCaseItem
            {
                DisciplineID = FormInt("DisciplineID"),
                EmployeeID = FormInt("EmployeeID"),
                IncidentDate = FormString("IncidentDate"),
                ReportedDate = FormString("ReportedDate"),
                DisciplineType = FormString("DisciplineType"),
                DisciplineCategory = FormString("DisciplineCategory"),
                Description = FormString("Description"),
                InvestigationDetails = FormString("InvestigationDetails"),
                InvestigationFindings = FormString("InvestigationFindings"),
                EmployeeExplanation = FormString("EmployeeExplanation"),
                FindingsObservations = FormString("FindingsObservations"),
                FindingRemarks = FormString("FindingRemarks"),
                CorrectiveActions = FormString("CorrectiveActions"),
                DisciplinaryAction = FormString("DisciplinaryAction"),
                ActionDate = FormString("ActionDate"),
                ActionResponsiblePerson = FormString("ActionResponsiblePerson"),
                CaseStatus = FormString("CaseStatus"),
                FinalDecision = FormString("FinalDecision"),
                ManagementRemarks = FormString("ManagementRemarks"),
                IncludeInPerformance = FormBool("IncludeInPerformance")
            };
        }

        private Tuple<string, string> SaveUpload(HttpPostedFile file, int disciplineId)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("File type not allowed.");

            var folder = Server.MapPath("~/uploads/employee-discipline");
            Directory.CreateDirectory(folder);
            var safe = "dc_" + disciplineId + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ext;
            file.SaveAs(Path.Combine(folder, safe));
            return Tuple.Create("/uploads/employee-discipline/" + safe, Path.GetFileName(file.FileName));
        }
    }
}
