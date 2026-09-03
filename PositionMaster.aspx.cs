using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class PositionListItem
    {
        public int PositionID { get; set; }
        public string PositionNo { get; set; } = "";
        public string Description { get; set; } = "";
        public string JobTitle { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string TitleName { get; set; } = "";
        public string PositionTypeName { get; set; } = "";
        public string PositionDuration { get; set; } = "";
        public DateTime? PositionStartDate { get; set; }
        public DateTime? PositionEndDate { get; set; }
        public string Email { get; set; } = "";
        public bool IsActive { get; set; }
    }

    public class PositionInput
    {
        public int PositionID { get; set; }
        public string PositionNo { get; set; } = "";
        public string Description { get; set; } = "";
        public int EmailEmployeeID { get; set; }
        public int JobID { get; set; }
        public int DepartmentID { get; set; }
        public int ReportsToPositionID { get; set; }
        public int TitleID { get; set; }
        public int PositionTypeID { get; set; }
        public string PositionDuration { get; set; } = "";
        public string PositionStartDate { get; set; } = "";
        public string PositionEndDate { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class EmailLookupItem
    {
        public int EmployeeID { get; set; }
        public string Email { get; set; } = "";
        public string Label { get; set; } = "";
    }

    public partial class PositionMasterPage : AppBasePage
    {
        public static readonly string[] PositionDurations =
            { "Permanent", "Fixed Term", "Temporary", "Contract", "Internship" };

        public string PageTitle => "Position Master";
        public bool ShowForm { get; set; }
        public bool EditMode => Input.PositionID > 0;
        public PositionInput Input { get; set; } = new PositionInput();
        public List<PositionListItem> Positions { get; set; } = new List<PositionListItem>();
        public List<EmailLookupItem> EmailLookups { get; set; } = new List<EmailLookupItem>();
        public List<LookupItem> Jobs { get; set; } = new List<LookupItem>();
        public List<LookupItem> Departments { get; set; } = new List<LookupItem>();
        public List<LookupItem> ReportToPositions { get; set; } = new List<LookupItem>();
        public List<LookupItem> Titles { get; set; } = new List<LookupItem>();
        public List<LookupItem> PositionTypes { get; set; } = new List<LookupItem>();
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    SoftDelete(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0);
                    return;
                }
                SavePosition();
                return;
            }

            var newPos = Request.QueryString["newPosition"] == "1" || Request.QueryString["newPosition"] == "true";
            OnGet(QueryInt("editId"), newPos);
        }

        private void OnGet(int? editId, bool newPosition)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg; AlertType = typ;
            LoadLookupLists();

            if (newPosition || (editId.HasValue && editId > 0))
            {
                ShowForm = true;
                if (editId.HasValue && editId > 0)
                    LoadPosition(editId.Value);
                else
                {
                    Input.PositionNo = GenerateNextPositionNo();
                    Input.PositionStartDate = DateTime.Today.ToString("yyyy-MM-dd");
                }
                LoadReportToPositions();
            }
            else LoadPositionList();
        }

        private void SavePosition()
        {
            var positionID = int.TryParse(Request.Form["positionID"], out var pid) ? pid : 0;
            var positionNo = FormString("positionNo");
            var description = FormString("description");
            var emailEmployeeID = int.TryParse(Request.Form["emailEmployeeID"], out var ee) ? ee : 0;
            var jobID = int.TryParse(Request.Form["jobID"], out var jid) ? jid : 0;
            var departmentID = int.TryParse(Request.Form["departmentID"], out var did) ? did : 0;
            var reportsToPositionID = int.TryParse(Request.Form["reportsToPositionID"], out var rid) ? rid : 0;
            var titleID = int.TryParse(Request.Form["titleID"], out var tid) ? tid : 0;
            var positionTypeID = int.TryParse(Request.Form["positionTypeID"], out var ptid) ? ptid : 0;
            var positionDuration = FormString("positionDuration");
            DateTime? startDate = DateTime.TryParse(FormString("positionStartDate"), out var sd) ? sd.Date : (DateTime?)null;
            DateTime? endDate = DateTime.TryParse(FormString("positionEndDate"), out var ed) ? ed.Date : (DateTime?)null;
            var isActive = FormBool("isActive");

            if (string.IsNullOrWhiteSpace(positionNo))
            {
                SetAlert("Position number is required.", "error");
                Response.Redirect(positionID > 0 ? "~/PositionMaster.aspx?editId=" + positionID : "~/PositionMaster.aspx?newPosition=1");
                return;
            }
            if (reportsToPositionID > 0 && reportsToPositionID == positionID)
            {
                SetAlert("A position cannot report to itself.", "error");
                Response.Redirect(positionID > 0 ? "~/PositionMaster.aspx?editId=" + positionID : "~/PositionMaster.aspx?newPosition=1");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (positionID > 0)
                    {
                        using (var cmd = new SqlCommand(@"
UPDATE tblPosition SET PositionNo=@PositionNo, Description=@Description, EmailEmployeeID=@EmailEmployeeID,
  JobID=@JobID, DepartmentID=@DepartmentID, ReportsToPositionID=@ReportsToPositionID, TitleID=@TitleID,
  PositionTypeID=@PositionTypeID, PositionDuration=@PositionDuration, PositionStartDate=@PositionStartDate,
  PositionEndDate=@PositionEndDate, IsActive=@IsActive, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
WHERE PositionID=@PositionID;", conn))
                        {
                            BindPositionParams(cmd, positionID, positionNo, description, emailEmployeeID, jobID,
                                departmentID, reportsToPositionID, titleID, positionTypeID, positionDuration,
                                startDate, endDate, isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Position updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
INSERT INTO tblPosition
 (PositionNo, Description, EmailEmployeeID, JobID, DepartmentID, ReportsToPositionID, TitleID,
  PositionTypeID, PositionDuration, PositionStartDate, PositionEndDate, IsActive, CreatedOn, CreatedByUserID)
VALUES
 (@PositionNo, @Description, @EmailEmployeeID, @JobID, @DepartmentID, @ReportsToPositionID, @TitleID,
  @PositionTypeID, @PositionDuration, @PositionStartDate, @PositionEndDate, @IsActive, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
                        {
                            BindPositionParams(cmd, 0, positionNo, description, emailEmployeeID, jobID,
                                departmentID, reportsToPositionID, titleID, positionTypeID, positionDuration,
                                startDate, endDate, isActive, isInsert: true);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            positionID = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        SetAlert("Position created successfully.");
                    }
                }
                Response.Redirect("~/PositionMaster.aspx?editId=" + positionID);
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                SetAlert("Position number already exists.", "error");
                Response.Redirect(positionID > 0 ? "~/PositionMaster.aspx?editId=" + positionID : "~/PositionMaster.aspx?newPosition=1");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
                Response.Redirect(positionID > 0 ? "~/PositionMaster.aspx?editId=" + positionID : "~/PositionMaster.aspx?newPosition=1");
            }
        }

        private void SoftDelete(int deleteId)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
UPDATE tblPosition SET IsActive=0, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE PositionID=@PositionID;", conn))
                {
                    cmd.Parameters.AddWithValue("@PositionID", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Position deactivated successfully.");
            }
            catch (Exception ex) { SetAlert("Error: " + ex.Message, "error"); }
            Response.Redirect("~/PositionMaster.aspx");
        }

        private void BindPositionParams(SqlCommand cmd, int positionID, string positionNo, string description,
            int emailEmployeeID, int jobID, int departmentID, int reportsToPositionID,
            int titleID, int positionTypeID, string positionDuration,
            DateTime? positionStartDate, DateTime? positionEndDate, bool isActive, bool isInsert = false)
        {
            if (!isInsert) cmd.Parameters.AddWithValue("@PositionID", positionID);
            cmd.Parameters.AddWithValue("@PositionNo", positionNo.Trim());
            cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());
            cmd.Parameters.AddWithValue("@EmailEmployeeID", emailEmployeeID > 0 ? (object)emailEmployeeID : DBNull.Value);
            cmd.Parameters.AddWithValue("@JobID", jobID > 0 ? (object)jobID : DBNull.Value);
            cmd.Parameters.AddWithValue("@DepartmentID", departmentID > 0 ? (object)departmentID : DBNull.Value);
            cmd.Parameters.AddWithValue("@ReportsToPositionID", reportsToPositionID > 0 ? (object)reportsToPositionID : DBNull.Value);
            cmd.Parameters.AddWithValue("@TitleID", titleID > 0 ? (object)titleID : DBNull.Value);
            cmd.Parameters.AddWithValue("@PositionTypeID", positionTypeID > 0 ? (object)positionTypeID : DBNull.Value);
            cmd.Parameters.AddWithValue("@PositionDuration", string.IsNullOrWhiteSpace(positionDuration) ? (object)DBNull.Value : positionDuration);
            cmd.Parameters.AddWithValue("@PositionStartDate", (object)positionStartDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PositionEndDate", (object)positionEndDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
        }

        private string GenerateNextPositionNo()
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT TOP 1 PositionNo FROM tblPosition WHERE PositionNo LIKE 'EPS-%' ORDER BY PositionNo DESC;", conn))
            {
                conn.Open();
                var last = cmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrEmpty(last) && last.Length >= 10)
                {
                    var numPart = last.Substring(4);
                    if (int.TryParse(numPart, out int num))
                        return "EPS-" + (num + 1).ToString("D6");
                }
                return "EPS-000001";
            }
        }

        private void LoadLookupLists()
        {
            Jobs.Clear(); Departments.Clear(); Titles.Clear(); PositionTypes.Clear(); EmailLookups.Clear();
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT JobID, JobTitle+' ('+JobCode+')' FROM tblJob WHERE IsActive=1 ORDER BY JobTitle;", conn))
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read()) Jobs.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });

                using (var cmd = new SqlCommand("SELECT DepartmentID, DepartmentName FROM tblDepartment WHERE IsActive=1 ORDER BY DepartmentName;", conn))
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read()) Departments.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });

                using (var cmd = new SqlCommand("SELECT DesignationLevelID, DesignationLevelName FROM tblDesignationLevel WHERE IsActive=1 ORDER BY DesignationLevelName;", conn))
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read()) Titles.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });

                using (var cmd = new SqlCommand("SELECT EmploymentTypeID, EmploymentTypeName FROM tblEmploymentType WHERE IsActive=1 ORDER BY EmploymentTypeName;", conn))
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read()) PositionTypes.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });

                using (var cmd = new SqlCommand(@"
SELECT e.EmployeeID, c.ContactValue,
       e.EmployeeCode+' – '+e.FirstName+' '+e.LastName
FROM tblEmployee e
INNER JOIN tblEmployeeContact c ON c.EmployeeID=e.EmployeeID
WHERE e.Status='Active' AND c.ContactType='OfficialEmail'
  AND c.ContactValue IS NOT NULL AND LTRIM(RTRIM(c.ContactValue))<>''
ORDER BY e.FirstName;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var email = dr.GetString(1);
                        EmailLookups.Add(new EmailLookupItem
                        {
                            EmployeeID = dr.GetInt32(0),
                            Email = email,
                            Label = dr.GetString(2) + " (" + email + ")"
                        });
                    }
                }
            }
        }

        private void LoadReportToPositions()
        {
            ReportToPositions.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT PositionID, PositionNo+ISNULL(' – '+NULLIF(Description,''),'')
FROM tblPosition WHERE IsActive=1 AND (@ExcludeId=0 OR PositionID<>@ExcludeId) ORDER BY PositionNo;", conn))
            {
                cmd.Parameters.AddWithValue("@ExcludeId", Input.PositionID);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        ReportToPositions.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
            }
        }

        private void LoadPosition(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT PositionID, PositionNo, Description, ISNULL(EmailEmployeeID,0), ISNULL(JobID,0),
       ISNULL(DepartmentID,0), ISNULL(ReportsToPositionID,0), ISNULL(TitleID,0), ISNULL(PositionTypeID,0),
       PositionDuration, PositionStartDate, PositionEndDate, IsActive
FROM tblPosition WHERE PositionID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;
                    Input = new PositionInput
                    {
                        PositionID = id,
                        PositionNo = dr.GetString(1),
                        Description = dr.IsDBNull(2) ? "" : dr.GetString(2),
                        EmailEmployeeID = Convert.ToInt32(dr.GetValue(3)),
                        JobID = Convert.ToInt32(dr.GetValue(4)),
                        DepartmentID = Convert.ToInt32(dr.GetValue(5)),
                        ReportsToPositionID = Convert.ToInt32(dr.GetValue(6)),
                        TitleID = Convert.ToInt32(dr.GetValue(7)),
                        PositionTypeID = Convert.ToInt32(dr.GetValue(8)),
                        PositionDuration = dr.IsDBNull(9) ? "" : dr.GetString(9),
                        PositionStartDate = dr.IsDBNull(10) ? "" : Convert.ToDateTime(dr.GetValue(10)).ToString("yyyy-MM-dd"),
                        PositionEndDate = dr.IsDBNull(11) ? "" : Convert.ToDateTime(dr.GetValue(11)).ToString("yyyy-MM-dd"),
                        IsActive = Convert.ToBoolean(dr.GetValue(12))
                    };
                }
            }
        }

        private void LoadPositionList()
        {
            Positions.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT p.PositionID, p.PositionNo, p.Description, ISNULL(j.JobTitle,''), ISNULL(d.DepartmentName,''),
       ISNULL(t.DesignationLevelName,''), ISNULL(et.EmploymentTypeName,''), ISNULL(p.PositionDuration,''),
       p.PositionStartDate, p.PositionEndDate, ISNULL(ec.ContactValue,''), p.IsActive
FROM tblPosition p
LEFT JOIN tblJob j ON j.JobID=p.JobID
LEFT JOIN tblDepartment d ON d.DepartmentID=p.DepartmentID
LEFT JOIN tblDesignationLevel t ON t.DesignationLevelID=p.TitleID
LEFT JOIN tblEmploymentType et ON et.EmploymentTypeID=p.PositionTypeID
LEFT JOIN tblEmployeeContact ec ON ec.EmployeeID=p.EmailEmployeeID AND ec.ContactType='OfficialEmail'
ORDER BY p.IsActive DESC, p.PositionNo;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Positions.Add(new PositionListItem
                        {
                            PositionID = dr.GetInt32(0),
                            PositionNo = dr.GetString(1),
                            Description = dr.IsDBNull(2) ? "" : dr.GetString(2),
                            JobTitle = dr.GetString(3),
                            DepartmentName = dr.GetString(4),
                            TitleName = dr.GetString(5),
                            PositionTypeName = dr.GetString(6),
                            PositionDuration = dr.GetString(7),
                            PositionStartDate = dr.IsDBNull(8) ? (DateTime?)null : Convert.ToDateTime(dr.GetValue(8)),
                            PositionEndDate = dr.IsDBNull(9) ? (DateTime?)null : Convert.ToDateTime(dr.GetValue(9)),
                            Email = dr.GetString(10),
                            IsActive = Convert.ToBoolean(dr.GetValue(11))
                        });
                    }
                }
            }
        }
    }
}
