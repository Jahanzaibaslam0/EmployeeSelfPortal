using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Data.SqlClient;
using HRMS.Services;

namespace HRMS
{
    /// <summary>Admin page: create/update LMS knowledge documents and access grants.</summary>
    public class LmsDocumentSetupPage : AppBasePage
    {
        private static readonly string[] AllowedExtensions =
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".png", ".jpg", ".jpeg", ".txt"
        };

        private readonly LmsDocumentService _lms = new LmsDocumentService();

        public string PageTitle => "LMS Document Setup";
        public LmsDocumentItem Input { get; set; } = new LmsDocumentItem { IsActive = true, AccessScope = LmsAccessScopes.Organization, Category = "General" };
        public List<LmsDocumentItem> Records { get; set; } = new List<LmsDocumentItem>();
        public List<LmsAccessGrant> ExistingGrants { get; set; } = new List<LmsAccessGrant>();
        public List<LookupItem> Departments { get; set; } = new List<LookupItem>();
        public List<LookupItem> Jobs { get; set; } = new List<LookupItem>();
        public List<LookupItem> Employees { get; set; } = new List<LookupItem>();
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            Form.Enctype = "multipart/form-data";

            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (!_lms.CanManageDocuments() && !Auth.IsAdmin && !Perms.CanRead(LmsDocumentService.SetupFormKey))
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            _lms.EnsureSchema();

            if (IsPostBack)
            {
                if (!_lms.CanManageDocuments())
                {
                    SetAlert(PermissionService.AccessRestrictedMessage, "error");
                    Response.Redirect("~/LmsDocumentSetup.aspx");
                    return;
                }

                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    SoftDelete(FormInt("deleteId"));
                    return;
                }
                Save();
                return;
            }

            LoadPage(QueryInt("editId"));
        }

        private void LoadPage(int? editId)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            LoadLookups();
            if (editId.HasValue && editId.Value > 0)
            {
                var item = _lms.GetById(editId.Value, forSetup: true);
                if (item != null)
                {
                    Input = item;
                    ExistingGrants = _lms.GetAccessGrants(item.DocumentID);
                }
            }
            Records = _lms.ListAllForSetup();
        }

        private void Save()
        {
            var item = new LmsDocumentItem
            {
                DocumentID = FormInt("documentID"),
                Title = FormString("title"),
                Description = FormString("description"),
                Category = FormString("category"),
                AccessScope = FormString("accessScope"),
                DepartmentID = FormInt("departmentID"),
                JobID = FormInt("jobID"),
                VersionLabel = FormString("versionLabel"),
                EffectiveDate = FormString("effectiveDate"),
                ExpiryDate = FormString("expiryDate"),
                IsActive = FormBool("isActive")
            };

            if (string.IsNullOrWhiteSpace(item.Title))
            {
                SetAlert("Title is required.", "error");
                Response.Redirect("~/LmsDocumentSetup.aspx" + (item.DocumentID > 0 ? "?editId=" + item.DocumentID : ""));
                return;
            }

            if (!LmsCategories.All.Any(c => c.Equals(item.Category, StringComparison.OrdinalIgnoreCase)))
                item.Category = "General";
            if (!LmsAccessScopes.All.Any(s => s.Equals(item.AccessScope, StringComparison.OrdinalIgnoreCase)))
                item.AccessScope = LmsAccessScopes.Organization;

            if (item.AccessScope.Equals(LmsAccessScopes.Department, StringComparison.OrdinalIgnoreCase) && item.DepartmentID <= 0)
            {
                SetAlert("Select a department for Department-scoped documents.", "error");
                Response.Redirect("~/LmsDocumentSetup.aspx");
                return;
            }
            if (item.AccessScope.Equals(LmsAccessScopes.Job, StringComparison.OrdinalIgnoreCase) && item.JobID <= 0)
            {
                SetAlert("Select a job/role for Job-scoped documents.", "error");
                Response.Redirect("~/LmsDocumentSetup.aspx");
                return;
            }

            try
            {
                if (item.DocumentID > 0)
                {
                    var existing = _lms.GetById(item.DocumentID, forSetup: true);
                    if (existing != null)
                    {
                        item.DocumentPath = existing.DocumentPath;
                        item.OriginalFileName = existing.OriginalFileName;
                    }
                }

                var file = Request.Files["documentFile"];
                var hasFile = file != null && file.ContentLength > 0;

                var id = _lms.SaveDocument(item, Auth.CurrentUserId);

                if (hasFile)
                {
                    var saved = SaveDocumentFile(file, id);
                    item.DocumentID = id;
                    item.DocumentPath = saved.Item1;
                    item.OriginalFileName = saved.Item2;
                    _lms.SaveDocument(item, Auth.CurrentUserId);
                }

                var grants = BuildGrantsFromForm();
                _lms.ReplaceAccessGrants(id, grants, Auth.CurrentUserId);

                SetAlert(FormInt("documentID") > 0 ? "LMS document updated." : "LMS document added.");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }

            Response.Redirect("~/LmsDocumentSetup.aspx");
        }

        private void SoftDelete(int id)
        {
            try
            {
                _lms.SoftDelete(id, Auth.CurrentUserId);
                SetAlert("LMS document deactivated.");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }
            Response.Redirect("~/LmsDocumentSetup.aspx");
        }

        private List<LmsAccessGrant> BuildGrantsFromForm()
        {
            var grants = new List<LmsAccessGrant>();

            foreach (var id in Request.Form.GetValues("grantEmployeeIDs") ?? new string[0])
            {
                if (int.TryParse(id, out var empId) && empId > 0)
                    grants.Add(new LmsAccessGrant { GrantType = "Employee", EmployeeID = empId });
            }
            foreach (var id in Request.Form.GetValues("grantDepartmentIDs") ?? new string[0])
            {
                if (int.TryParse(id, out var deptId) && deptId > 0)
                    grants.Add(new LmsAccessGrant { GrantType = "Department", DepartmentID = deptId });
            }
            foreach (var id in Request.Form.GetValues("grantJobIDs") ?? new string[0])
            {
                if (int.TryParse(id, out var jobId) && jobId > 0)
                    grants.Add(new LmsAccessGrant { GrantType = "Job", JobID = jobId });
            }

            return grants
                .GroupBy(g => g.GrantType + ":" + g.EmployeeID + ":" + g.DepartmentID + ":" + g.JobID)
                .Select(g => g.First())
                .ToList();
        }

        private Tuple<string, string> SaveDocumentFile(HttpPostedFile file, int documentId)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("File type not allowed. Use PDF, Office, text, or image files.");

            var uploads = Server.MapPath("~/uploads/lms-documents");
            Directory.CreateDirectory(uploads);

            var safeName = "lms_" + documentId + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ext;
            file.SaveAs(Path.Combine(uploads, safeName));
            return Tuple.Create("/uploads/lms-documents/" + safeName, Path.GetFileName(file.FileName));
        }

        private void LoadLookups()
        {
            Departments.Clear();
            Jobs.Clear();
            Employees.Clear();

            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT DepartmentID, DepartmentName FROM tblDepartment WHERE IsActive=1 ORDER BY DepartmentName;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Departments.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
                }

                using (var cmd = new SqlCommand(
                    "SELECT JobID, ISNULL(JobTitle, JobCode) FROM tblJob WHERE IsActive=1 ORDER BY JobTitle;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Jobs.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.IsDBNull(1) ? "" : dr.GetString(1) });
                }

                using (var cmd = new SqlCommand(@"
SELECT EmployeeID,
       EmployeeCode + ' – ' + LTRIM(RTRIM(ISNULL(FirstName,'') + ' ' + ISNULL(LastName,'')))
FROM tblEmployee
WHERE Status = 'Active'
ORDER BY EmployeeCode;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Employees.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.IsDBNull(1) ? "" : dr.GetString(1) });
                }
            }
        }

        public bool IsGrantSelected(string grantType, int id)
        {
            if (id <= 0) return false;
            return ExistingGrants.Any(g =>
                g.GrantType.Equals(grantType, StringComparison.OrdinalIgnoreCase)
                && ((grantType == "Employee" && g.EmployeeID == id)
                    || (grantType == "Department" && g.DepartmentID == id)
                    || (grantType == "Job" && g.JobID == id)));
        }
    }
}
