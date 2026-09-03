using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class LeaveListItem
    {
        public int LeaveID { get; set; }
        public DateTime ApplyingDate { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string LeaveType { get; set; } = "";
        public string LeaveCategoryName { get; set; } = "";
        public DateTime LeaveFromDate { get; set; }
        public DateTime LeaveToDate { get; set; }
        public int NumberOfDays { get; set; }
    }

    public class LeaveInput
    {
        public int LeaveID { get; set; }
        public string ApplyingDate { get; set; } = "";
        public int EmployeeID { get; set; }
        public string LeaveType { get; set; } = "Planned";
        public bool IsFutureUnplannedLeave { get; set; }
        public int LeaveCategoryID { get; set; }
        public string LeaveFromDate { get; set; } = "";
        public string LeaveToDate { get; set; } = "";
        public int NumberOfDays { get; set; }
        public string ReasonForLeave { get; set; } = "";
        public int TempResponsibleEmployeeID { get; set; }
        public int PermanentResponsibleEmployeeID { get; set; }
    }

    public class EmployeeLeaveSnapshot
    {
        public string EmployeeName { get; set; } = "";
        public string FatherName { get; set; } = "";
        public string Division { get; set; } = "";
        public string Department { get; set; } = "";
        public string Designation { get; set; } = "";
        public string Region { get; set; } = "";
        public string Location { get; set; } = "";
    }

    public partial class LeaveMasterPage : AppBasePage
    {
        private readonly DataAccessScopeService _dataScope = new DataAccessScopeService();

        public static readonly string[] LeaveTypes = { "Planned", "Unplanned", "Future Unplanned Leave" };

        public string PageTitle => "Leave Master";
        public List<LeaveListItem> Leaves { get; set; } = new List<LeaveListItem>();
        public List<LookupItem> Employees { get; set; } = new List<LookupItem>();
        public List<LookupItem> LeaveCategories { get; set; } = new List<LookupItem>();
        public LeaveInput Input { get; set; } = new LeaveInput();
        public EmployeeLeaveSnapshot EmployeeInfo { get; set; } = new EmployeeLeaveSnapshot();
        public bool EditMode { get; set; }
        public bool ShowForm { get; set; }
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
                    OnPostDelete(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0);
                    return;
                }
                OnPostSave();
                return;
            }

            var newLeave = Request.QueryString["newLeave"] == "1" || Request.QueryString["newLeave"] == "true";
            OnGet(QueryInt("editId"), newLeave);
        }

        private void OnGet(int? editId, bool newLeave)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg; AlertType = typ;
            ShowForm = (editId.HasValue && editId > 0) || newLeave;

            if (ShowForm)
            {
                if (editId.HasValue && editId > 0 && !CanAccessLeave(editId.Value))
                {
                    SetAlert(DataAccessScopeService.AccessDeniedMessage, "error");
                    Response.Redirect("~/LeaveMaster.aspx");
                    return;
                }
                if (newLeave && !Perms.CanWrite("LeaveMaster"))
                {
                    SetAlert(PermissionService.AccessRestrictedMessage, "error");
                    Response.Redirect("~/LeaveMaster.aspx");
                    return;
                }
                LoadLookups();
                if (editId.HasValue && editId > 0)
                {
                    LoadForEdit(editId.Value);
                    EditMode = true;
                }
                else Input.ApplyingDate = DateTime.Today.ToString("yyyy-MM-dd");

                if (Input.EmployeeID > 0)
                    EmployeeInfo = LoadEmployeeSnapshot(Input.EmployeeID);
            }
            else LoadLeaves();
        }

        private void OnPostSave()
        {
            EditMode = FormBool("EditMode");
            Input = new LeaveInput
            {
                LeaveID = int.TryParse(Request.Form["LeaveID"], out var lid) ? lid : 0,
                ApplyingDate = FormString("ApplyingDate"),
                EmployeeID = int.TryParse(Request.Form["EmployeeID"], out var eid) ? eid : 0,
                LeaveType = string.IsNullOrWhiteSpace(FormString("LeaveType")) ? "Planned" : FormString("LeaveType"),
                IsFutureUnplannedLeave = FormBool("IsFutureUnplannedLeave"),
                LeaveCategoryID = int.TryParse(Request.Form["LeaveCategoryID"], out var cid) ? cid : 0,
                LeaveFromDate = FormString("LeaveFromDate"),
                LeaveToDate = FormString("LeaveToDate"),
                ReasonForLeave = FormString("ReasonForLeave"),
                TempResponsibleEmployeeID = int.TryParse(Request.Form["TempResponsibleEmployeeID"], out var tid) ? tid : 0,
                PermanentResponsibleEmployeeID = int.TryParse(Request.Form["PermanentResponsibleEmployeeID"], out var pid) ? pid : 0
            };

            if (Input.EmployeeID <= 0) { SetFormError("Employee is required."); return; }
            if (!_dataScope.CanAccessEmployee(Input.EmployeeID)) { SetFormError(DataAccessScopeService.AccessDeniedMessage); return; }
            if (EditMode && Input.LeaveID > 0 && !CanAccessLeave(Input.LeaveID)) { SetFormError(DataAccessScopeService.AccessDeniedMessage); return; }
            if (!Perms.CanWrite("LeaveMaster")) { SetFormError(PermissionService.AccessRestrictedMessage); return; }
            if (Input.LeaveCategoryID <= 0) { SetFormError("Leave category is required."); return; }

            var fromDate = ParseDate(Input.LeaveFromDate);
            var toDate = ParseDate(Input.LeaveToDate);
            if (!fromDate.HasValue || !toDate.HasValue) { SetFormError("Leave from and to dates are required."); return; }
            if (toDate.Value < fromDate.Value) { SetFormError("Leave to date cannot be before from date."); return; }
            Input.NumberOfDays = CalculateDays(fromDate.Value, toDate.Value);

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    SaveRecord(conn, Input);
                }
                SetAlert(EditMode ? "Leave application updated successfully." : "Leave application submitted successfully.");
                Response.Redirect("~/LeaveMaster.aspx?editId=" + Input.LeaveID);
            }
            catch (Exception ex) { SetFormError("Error: " + ex.Message); }
        }

        private void OnPostDelete(int deleteId)
        {
            if (!Perms.CanDelete("LeaveMaster") || !CanAccessLeave(deleteId))
            {
                SetAlert(DataAccessScopeService.AccessDeniedMessage, "error");
                Response.Redirect("~/LeaveMaster.aspx");
                return;
            }
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand("DELETE FROM tblLeaveApplication WHERE LeaveID=@Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", deleteId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Leave application deleted successfully.");
            }
            catch (Exception ex) { SetAlert("Error deleting record: " + ex.Message, "error"); }
            Response.Redirect("~/LeaveMaster.aspx");
        }

        private void SetFormError(string message)
        {
            AlertMessage = message; AlertType = "error";
            LoadLookups(); ShowForm = true;
            if (Input.EmployeeID > 0) EmployeeInfo = LoadEmployeeSnapshot(Input.EmployeeID);
        }

        private void SaveRecord(SqlConnection conn, LeaveInput input)
        {
            var applyingDate = ParseDate(input.ApplyingDate) ?? DateTime.Today;
            var fromDate = ParseDate(input.LeaveFromDate).Value;
            var toDate = ParseDate(input.LeaveToDate).Value;
            var days = CalculateDays(fromDate, toDate);

            if (input.LeaveID > 0)
            {
                using (var cmd = new SqlCommand(@"
UPDATE tblLeaveApplication SET ApplyingDate=@ApplyingDate, EmployeeID=@EmployeeID, LeaveType=@LeaveType,
  IsFutureUnplannedLeave=@IsFutureUnplannedLeave, LeaveCategoryID=@LeaveCategoryID,
  LeaveFromDate=@LeaveFromDate, LeaveToDate=@LeaveToDate, NumberOfDays=@NumberOfDays,
  ReasonForLeave=@ReasonForLeave, TempResponsibleEmployeeID=@TempResponsibleEmployeeID,
  PermanentResponsibleEmployeeID=@PermanentResponsibleEmployeeID,
  ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE LeaveID=@LeaveID;", conn))
                {
                    BindParams(cmd, input, applyingDate, fromDate, toDate, days);
                    cmd.Parameters.AddWithValue("@LeaveID", input.LeaveID);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    cmd.ExecuteNonQuery();
                }
                return;
            }

            using (var ins = new SqlCommand(@"
INSERT INTO tblLeaveApplication
 (ApplyingDate, EmployeeID, LeaveType, IsFutureUnplannedLeave, LeaveCategoryID,
  LeaveFromDate, LeaveToDate, NumberOfDays, ReasonForLeave,
  TempResponsibleEmployeeID, PermanentResponsibleEmployeeID, CreatedOn, CreatedByUserID)
VALUES
 (@ApplyingDate, @EmployeeID, @LeaveType, @IsFutureUnplannedLeave, @LeaveCategoryID,
  @LeaveFromDate, @LeaveToDate, @NumberOfDays, @ReasonForLeave,
  @TempResponsibleEmployeeID, @PermanentResponsibleEmployeeID, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
            {
                BindParams(ins, input, applyingDate, fromDate, toDate, days);
                AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                input.LeaveID = (int)ins.ExecuteScalar();
            }
        }

        private static void BindParams(SqlCommand cmd, LeaveInput input, DateTime applyingDate, DateTime fromDate, DateTime toDate, int days)
        {
            cmd.Parameters.AddWithValue("@ApplyingDate", applyingDate);
            cmd.Parameters.AddWithValue("@EmployeeID", input.EmployeeID);
            cmd.Parameters.AddWithValue("@LeaveType", input.LeaveType);
            cmd.Parameters.AddWithValue("@IsFutureUnplannedLeave", input.IsFutureUnplannedLeave);
            cmd.Parameters.AddWithValue("@LeaveCategoryID", input.LeaveCategoryID);
            cmd.Parameters.AddWithValue("@LeaveFromDate", fromDate);
            cmd.Parameters.AddWithValue("@LeaveToDate", toDate);
            cmd.Parameters.AddWithValue("@NumberOfDays", days);
            cmd.Parameters.AddWithValue("@ReasonForLeave", string.IsNullOrWhiteSpace(input.ReasonForLeave) ? (object)DBNull.Value : input.ReasonForLeave);
            cmd.Parameters.AddWithValue("@TempResponsibleEmployeeID", input.TempResponsibleEmployeeID > 0 ? (object)input.TempResponsibleEmployeeID : DBNull.Value);
            cmd.Parameters.AddWithValue("@PermanentResponsibleEmployeeID", input.PermanentResponsibleEmployeeID > 0 ? (object)input.PermanentResponsibleEmployeeID : DBNull.Value);
        }

        private void LoadLeaves()
        {
            Leaves.Clear();
            var scope = _dataScope.GetEmployeeFilter("e");
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand($@"
SELECT l.LeaveID, l.ApplyingDate, e.EmployeeCode, e.FirstName+' '+e.LastName,
       l.LeaveType, ISNULL(c.LeaveCategoryName,''), l.LeaveFromDate, l.LeaveToDate, l.NumberOfDays
FROM tblLeaveApplication l
INNER JOIN tblEmployee e ON e.EmployeeID=l.EmployeeID
LEFT JOIN tblLeaveCategory c ON c.LeaveCategoryID=l.LeaveCategoryID
WHERE 1=1 {scope.Sql} ORDER BY l.LeaveID DESC;", conn))
            {
                scope.ApplyTo(cmd);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Leaves.Add(new LeaveListItem
                        {
                            LeaveID = dr.GetInt32(0),
                            ApplyingDate = dr.GetDateTime(1),
                            EmployeeCode = dr.IsDBNull(2) ? "" : dr.GetString(2),
                            EmployeeName = dr.GetString(3),
                            LeaveType = dr.GetString(4),
                            LeaveCategoryName = dr.GetString(5),
                            LeaveFromDate = dr.GetDateTime(6),
                            LeaveToDate = dr.GetDateTime(7),
                            NumberOfDays = dr.GetInt32(8)
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int leaveId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT LeaveID, ApplyingDate, EmployeeID, LeaveType, IsFutureUnplannedLeave,
       LeaveCategoryID, LeaveFromDate, LeaveToDate, NumberOfDays,
       ReasonForLeave, TempResponsibleEmployeeID, PermanentResponsibleEmployeeID
FROM tblLeaveApplication WHERE LeaveID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", leaveId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;
                    Input = new LeaveInput
                    {
                        LeaveID = dr.GetInt32(0),
                        ApplyingDate = dr.GetDateTime(1).ToString("yyyy-MM-dd"),
                        EmployeeID = dr.GetInt32(2),
                        LeaveType = dr.GetString(3),
                        IsFutureUnplannedLeave = dr.GetBoolean(4),
                        LeaveCategoryID = dr.IsDBNull(5) ? 0 : dr.GetInt32(5),
                        LeaveFromDate = dr.GetDateTime(6).ToString("yyyy-MM-dd"),
                        LeaveToDate = dr.GetDateTime(7).ToString("yyyy-MM-dd"),
                        NumberOfDays = dr.GetInt32(8),
                        ReasonForLeave = dr.IsDBNull(9) ? "" : dr.GetString(9),
                        TempResponsibleEmployeeID = dr.IsDBNull(10) ? 0 : dr.GetInt32(10),
                        PermanentResponsibleEmployeeID = dr.IsDBNull(11) ? 0 : dr.GetInt32(11)
                    };
                }
            }
        }

        private EmployeeLeaveSnapshot LoadEmployeeSnapshot(int employeeId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT e.FirstName+' '+e.LastName, ISNULL(e.FathersHusbandsName,''),
       ISNULL(dv.DivisionName,''), ISNULL(d.DepartmentName,''), ISNULL(e.Designation,''),
       ISNULL(r.RegionName,''), ISNULL(l.LocationName,'')
FROM tblEmployee e
LEFT JOIN tblDepartment d ON d.DepartmentID=e.DepartmentID
LEFT JOIN tblDivision dv ON dv.DivisionID=e.DivisionID
LEFT JOIN tblRegion r ON r.RegionID=e.RegionID
LEFT JOIN tblLocation l ON l.LocationID=e.LocationID
WHERE e.EmployeeID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", employeeId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return new EmployeeLeaveSnapshot();
                    return new EmployeeLeaveSnapshot
                    {
                        EmployeeName = dr.GetString(0),
                        FatherName = dr.GetString(1),
                        Division = dr.GetString(2),
                        Department = dr.GetString(3),
                        Designation = dr.GetString(4),
                        Region = dr.GetString(5),
                        Location = dr.GetString(6)
                    };
                }
            }
        }

        private void LoadLookups()
        {
            Employees = new List<LookupItem>();
            LeaveCategories = new List<LookupItem>();
            var scope = _dataScope.GetEmployeeFilter("e");
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand($@"
SELECT e.EmployeeID, e.EmployeeCode, e.FirstName, e.LastName
FROM tblEmployee e WHERE e.Status='Active' {scope.Sql} ORDER BY e.FirstName, e.LastName;", conn))
                {
                    scope.ApplyTo(cmd);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
                            var name = (dr.GetString(2) + " " + dr.GetString(3)).Trim();
                            Employees.Add(new LookupItem { Id = dr.GetInt32(0), Name = string.IsNullOrEmpty(code) ? name : code + " – " + name });
                        }
                    }
                }
                using (var cmd = new SqlCommand("SELECT LeaveCategoryID, LeaveCategoryName FROM tblLeaveCategory WHERE IsActive=1 ORDER BY LeaveCategoryName;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        LeaveCategories.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
                }
            }
        }

        private bool CanAccessLeave(int leaveId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT EmployeeID FROM tblLeaveApplication WHERE LeaveID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", leaveId);
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return false;
                return _dataScope.CanAccessEmployee(Convert.ToInt32(result));
            }
        }

        private static DateTime? ParseDate(string value) =>
            string.IsNullOrWhiteSpace(value) ? (DateTime?)null : DateTime.Parse(value);

        public static int CalculateDays(DateTime fromDate, DateTime toDate) =>
            (toDate.Date - fromDate.Date).Days + 1;
    }
}
