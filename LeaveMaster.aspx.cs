using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;
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
        public string Status { get; set; } = "";
        public string StatusDisplay => LeaveApprovalStatuses.DisplayName(Status);
        public string StatusBadgeClass => LeaveApprovalStatuses.BadgeClass(Status);
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
        public string Status { get; set; } = LeaveApprovalStatuses.Draft;
        public int DepartmentHeadEmployeeID { get; set; }

        public string ApplicationNumber =>
            LeaveID > 0 ? "LV-" + LeaveID.ToString("D7") : "(New)";
    }

    public class EmployeeLeaveSnapshot
    {
        public string EmployeeCode { get; set; } = "";
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
        private readonly EmployeeProfileAccessService _profileAccess = new EmployeeProfileAccessService();
        private readonly LeaveApprovalService _approval = new LeaveApprovalService();

        public static readonly string[] LeaveTypes = { "Planned", "Unplanned", "Future Unplanned Leave" };

        public string PageTitle => "Leave Application Form";
        public List<LeaveListItem> Leaves { get; set; } = new List<LeaveListItem>();
        public List<LookupItem> Employees { get; set; } = new List<LookupItem>();
        public List<LookupItem> LeaveCategories { get; set; } = new List<LookupItem>();
        public LeaveInput Input { get; set; } = new LeaveInput();
        public EmployeeLeaveSnapshot EmployeeInfo { get; set; } = new EmployeeLeaveSnapshot();
        public bool EditMode { get; set; }
        public bool ShowForm { get; set; }
        public bool LockEmployeeSelection { get; set; }
        public bool CanApproveCurrent { get; set; }
        public bool CanEditApplication { get; set; } = true;
        public bool IsHrApprover { get; set; }
        public string Inbox { get; set; } = "all";
        public List<LeaveApprovalActionItem> ApprovalHistory { get; set; } = new List<LeaveApprovalActionItem>();
        public List<LeaveAlertItem> LeaveAlerts { get; set; } = new List<LeaveAlertItem>();
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                EnsureLeaveSchema();
                _approval.EnsureSchema();
            }
            catch
            {
                // DDL may be blocked; production patch can be applied separately.
            }

            // AJAX: employee profile snapshot for auto-fill
            if (string.Equals(Request.QueryString["ajax"], "employeeInfo", StringComparison.OrdinalIgnoreCase))
            {
                WriteEmployeeInfoJson();
                return;
            }

            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostDelete(FormInt("deleteId"));
                    return;
                }
                if (string.Equals(handler, "Approve", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostApproveReject(true);
                    return;
                }
                if (string.Equals(handler, "Reject", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostApproveReject(false);
                    return;
                }
                OnPostSave();
                return;
            }

            var newLeave = Request.QueryString["newLeave"] == "1" || Request.QueryString["newLeave"] == "true";
            OnGet(QueryInt("editId"), newLeave);
        }

        private void WriteEmployeeInfoJson()
        {
            Response.Clear();
            Response.ContentType = "application/json";
            var id = 0;
            int.TryParse(Request.QueryString["employeeId"], out id);
            var snap = (id > 0 && _dataScope.CanAccessEmployee(id))
                ? LoadEmployeeSnapshot(id)
                : new EmployeeLeaveSnapshot();
            Response.Write(new JavaScriptSerializer().Serialize(snap));
            Response.End();
        }

        private void OnGet(int? editId, bool newLeave)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            Inbox = (Request.QueryString["inbox"] ?? "all").Trim();
            LeaveAlerts = _approval.GetUnreadAlerts(10);
            IsHrApprover = _approval.CanActAsHr();
            ShowForm = (editId.HasValue && editId > 0) || newLeave;

            if (ShowForm)
            {
                if (editId.HasValue && editId > 0 && !CanAccessLeave(editId.Value))
                {
                    SetAlert(DataAccessScopeService.AccessDeniedMessage, "error");
                    Response.Redirect("~/LeaveMaster.aspx");
                    return;
                }
                if (newLeave && !Perms.CanWrite("LeaveMaster") && !_profileAccess.IsEmployeeProfileSynchronized())
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
                    _approval.MarkAlertsRead(editId.Value);
                    ApprovalHistory = _approval.GetHistory(editId.Value);
                    CanApproveCurrent = _approval.CanApprove(Input.Status, Input.DepartmentHeadEmployeeID);
                    CanEditApplication = CanEditLeave(Input.Status, Input.DepartmentHeadEmployeeID);
                }
                else
                {
                    Input.ApplyingDate = DateTime.Today.ToString("yyyy-MM-dd");
                    // Self-service: default to linked employee
                    if (!_dataScope.BypassesDataScope() && !_profileAccess.HasFullEmployeeMasterAccess())
                    {
                        var ownId = _profileAccess.GetLinkedEmployeeId();
                        if (ownId.HasValue && ownId.Value > 0)
                        {
                            Input.EmployeeID = ownId.Value;
                            LockEmployeeSelection = true;
                        }
                    }
                }

                if (Input.EmployeeID > 0)
                {
                    EmployeeInfo = LoadEmployeeSnapshot(Input.EmployeeID);
                    if (!_dataScope.BypassesDataScope() && !_profileAccess.HasFullEmployeeMasterAccess())
                        LockEmployeeSelection = true;
                }
            }
            else
            {
                LoadLeaves();
            }
        }

        private void OnPostSave()
        {
            EditMode = FormBool("EditMode") || FormInt("LeaveID") > 0;
            Input = new LeaveInput
            {
                LeaveID = FormInt("LeaveID"),
                ApplyingDate = FormString("ApplyingDate"),
                EmployeeID = FormInt("EmployeeID"),
                LeaveType = string.IsNullOrWhiteSpace(FormString("LeaveType")) ? "Planned" : FormString("LeaveType"),
                IsFutureUnplannedLeave = FormBool("IsFutureUnplannedLeave"),
                LeaveCategoryID = FormInt("LeaveCategoryID"),
                LeaveFromDate = FormString("LeaveFromDate"),
                LeaveToDate = FormString("LeaveToDate"),
                ReasonForLeave = FormString("ReasonForLeave"),
                TempResponsibleEmployeeID = FormInt("TempResponsibleEmployeeID"),
                PermanentResponsibleEmployeeID = FormInt("PermanentResponsibleEmployeeID")
            };

            // Non-admin / non–full-scope users cannot switch to another employee
            if (!_dataScope.BypassesDataScope() && !_profileAccess.HasFullEmployeeMasterAccess())
            {
                var ownId = _profileAccess.GetLinkedEmployeeId();
                if (!ownId.HasValue || ownId.Value <= 0)
                {
                    SetFormError(EmployeeProfileAccessService.NotSynchronizedMessage);
                    return;
                }
                Input.EmployeeID = ownId.Value;
                LockEmployeeSelection = true;
            }

            if (Input.EmployeeID <= 0) { SetFormError("Employee is required."); return; }
            if (!_dataScope.CanAccessEmployee(Input.EmployeeID)) { SetFormError(DataAccessScopeService.AccessDeniedMessage); return; }
            if (EditMode && Input.LeaveID > 0 && !CanAccessLeave(Input.LeaveID)) { SetFormError(DataAccessScopeService.AccessDeniedMessage); return; }
            if (EditMode && Input.LeaveID > 0)
            {
                string existingStatus;
                int existingDh;
                LoadApprovalState(Input.LeaveID, out existingStatus, out existingDh);
                if (!CanEditLeave(existingStatus, existingDh))
                {
                    SetFormError("This leave request cannot be edited while it is in the approval workflow.");
                    return;
                }
            }
            if (!Perms.CanWrite("LeaveMaster") && !_profileAccess.IsEmployeeProfileSynchronized()) { SetFormError(PermissionService.AccessRestrictedMessage); return; }
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
            }
            catch (Exception ex)
            {
                SetFormError("Error: " + ex.Message);
                return;
            }

            SetAlert("Leave application submitted. Status is now Pending Department Head approval.");
            Response.Redirect("~/LeaveMaster.aspx?inbox=all");
        }

        private void OnPostApproveReject(bool approve)
        {
            var leaveId = FormInt("LeaveID");
            var comments = FormString("approvalComments");
            try
            {
                if (approve) _approval.Approve(leaveId, comments);
                else _approval.Reject(leaveId, comments);
                SetAlert(approve
                    ? "Leave request updated. Approval recorded."
                    : "Leave request rejected. The requester has been notified.");
            }
            catch (Exception ex)
            {
                SetAlert(ex.Message, "error");
                Response.Redirect(leaveId > 0 ? "~/LeaveMaster.aspx?editId=" + leaveId : "~/LeaveMaster.aspx");
                return;
            }
            Response.Redirect(leaveId > 0 ? "~/LeaveMaster.aspx?editId=" + leaveId : "~/LeaveMaster.aspx");
        }

        private static bool CanEditLeave(string status, int departmentHeadEmployeeId)
        {
            if (string.Equals(status, LeaveApprovalStatuses.Draft, StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(status, LeaveApprovalStatuses.Approved, StringComparison.OrdinalIgnoreCase)
                && departmentHeadEmployeeId <= 0)
                return true;
            return false;
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
            AlertMessage = message;
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            if (Input.LeaveID > 0)
            {
                ApprovalHistory = _approval.GetHistory(Input.LeaveID);
                CanApproveCurrent = _approval.CanApprove(Input.Status, Input.DepartmentHeadEmployeeID);
                CanEditApplication = CanEditLeave(Input.Status, Input.DepartmentHeadEmployeeID);
            }
            if (Input.EmployeeID > 0)
                EmployeeInfo = LoadEmployeeSnapshot(Input.EmployeeID);
            if (!_dataScope.BypassesDataScope() && !_profileAccess.HasFullEmployeeMasterAccess())
                LockEmployeeSelection = true;
        }

        private void SaveRecord(SqlConnection conn, LeaveInput input)
        {
            var applyingDate = ParseDate(input.ApplyingDate) ?? DateTime.Today;
            var fromDate = ParseDate(input.LeaveFromDate).Value;
            var toDate = ParseDate(input.LeaveToDate).Value;
            var days = CalculateDays(fromDate, toDate);
            var dhId = _approval.ResolveDepartmentHeadEmployeeId(input.EmployeeID);
            if (dhId <= 0)
                throw new InvalidOperationException(
                    "Department Head is not assigned for this employee's department. Set Department Head on Department Setup before submitting leave.");

            if (input.LeaveID > 0)
            {
                using (var cmd = new SqlCommand(@"
UPDATE tblLeaveApplication SET ApplyingDate=@ApplyingDate, EmployeeID=@EmployeeID, LeaveType=@LeaveType,
  IsFutureUnplannedLeave=@IsFutureUnplannedLeave, LeaveCategoryID=@LeaveCategoryID,
  LeaveFromDate=@LeaveFromDate, LeaveToDate=@LeaveToDate, NumberOfDays=@NumberOfDays,
  ReasonForLeave=@ReasonForLeave, TempResponsibleEmployeeID=@TempResponsibleEmployeeID,
  PermanentResponsibleEmployeeID=@PermanentResponsibleEmployeeID,
  Status=@Status, DepartmentHeadEmployeeID=@DepartmentHeadEmployeeID,
  ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE LeaveID=@LeaveID;", conn))
                {
                    BindParams(cmd, input, applyingDate, fromDate, toDate, days);
                    cmd.Parameters.AddWithValue("@Status", LeaveApprovalStatuses.PendingDepartmentHead);
                    cmd.Parameters.AddWithValue("@DepartmentHeadEmployeeID", dhId);
                    cmd.Parameters.AddWithValue("@LeaveID", input.LeaveID);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                using (var ins = new SqlCommand(@"
INSERT INTO tblLeaveApplication
 (ApplyingDate, EmployeeID, LeaveType, IsFutureUnplannedLeave, LeaveCategoryID,
  LeaveFromDate, LeaveToDate, NumberOfDays, ReasonForLeave,
  TempResponsibleEmployeeID, PermanentResponsibleEmployeeID, Status, DepartmentHeadEmployeeID,
  CreatedOn, CreatedByUserID)
VALUES
 (@ApplyingDate, @EmployeeID, @LeaveType, @IsFutureUnplannedLeave, @LeaveCategoryID,
  @LeaveFromDate, @LeaveToDate, @NumberOfDays, @ReasonForLeave,
  @TempResponsibleEmployeeID, @PermanentResponsibleEmployeeID, @Status, @DepartmentHeadEmployeeID,
  GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
                {
                    BindParams(ins, input, applyingDate, fromDate, toDate, days);
                    ins.Parameters.AddWithValue("@Status", LeaveApprovalStatuses.PendingDepartmentHead);
                    ins.Parameters.AddWithValue("@DepartmentHeadEmployeeID", dhId);
                    AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                    var idObj = ins.ExecuteScalar();
                    input.LeaveID = idObj == null || idObj == DBNull.Value ? 0 : Convert.ToInt32(idObj);
                }

                if (input.LeaveID <= 0)
                {
                    using (var idCmd = new SqlCommand(@"
SELECT MAX(LeaveID) FROM tblLeaveApplication WHERE EmployeeID=@EmployeeID AND CreatedByUserID=@UserID;", conn))
                    {
                        idCmd.Parameters.AddWithValue("@EmployeeID", input.EmployeeID);
                        idCmd.Parameters.AddWithValue("@UserID", (object)Auth.CurrentUserId ?? DBNull.Value);
                        var idObj = idCmd.ExecuteScalar();
                        input.LeaveID = idObj == null || idObj == DBNull.Value ? 0 : Convert.ToInt32(idObj);
                    }
                }
            }

            using (var stamp = new SqlCommand(@"
UPDATE tblLeaveApplication
SET Status = @Status,
    DepartmentHeadEmployeeID = @DepartmentHeadEmployeeID,
    ModifiedOn = GETDATE()
WHERE LeaveID = @LeaveID;", conn))
            {
                stamp.Parameters.AddWithValue("@Status", LeaveApprovalStatuses.PendingDepartmentHead);
                stamp.Parameters.AddWithValue("@DepartmentHeadEmployeeID", dhId);
                stamp.Parameters.AddWithValue("@LeaveID", input.LeaveID);
                stamp.ExecuteNonQuery();
            }

            input.Status = LeaveApprovalStatuses.PendingDepartmentHead;
            input.DepartmentHeadEmployeeID = dhId;
            try
            {
                _approval.OnSubmitted(conn, input.LeaveID, input.EmployeeID, dhId);
            }
            catch
            {
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
            var linked = _approval.LinkedEmployeeId;
            var isHr = _approval.CanActAsHr() ? 1 : 0;
            var isAdmin = Auth.IsAdmin ? 1 : 0;
            var canRead = Perms.CanRead("LeaveMaster") ? 1 : 0;
            var inboxSql = _approval.StatusSqlFilter(Inbox, linked, isHr == 1);
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand($@"
SELECT l.LeaveID, l.ApplyingDate, e.EmployeeCode, e.FirstName+' '+e.LastName,
       l.LeaveType, ISNULL(c.LeaveCategoryName,''), l.LeaveFromDate, l.LeaveToDate, l.NumberOfDays,
       ISNULL(l.Status, N'Approved')
FROM tblLeaveApplication l
INNER JOIN tblEmployee e ON e.EmployeeID=l.EmployeeID
LEFT JOIN tblLeaveCategory c ON c.LeaveCategoryID=l.LeaveCategoryID
WHERE (
        l.EmployeeID = @Linked
     OR ISNULL(l.DepartmentHeadEmployeeID,0) = @Linked
     OR @IsHr = 1
     OR @IsAdmin = 1
     OR (@CanRead = 1 {scope.Sql})
      )
      {inboxSql}
ORDER BY l.LeaveID DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@Linked", linked);
                cmd.Parameters.AddWithValue("@IsHr", isHr);
                cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);
                cmd.Parameters.AddWithValue("@CanRead", canRead);
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
                            NumberOfDays = dr.GetInt32(8),
                            Status = dr.IsDBNull(9) ? LeaveApprovalStatuses.Approved : dr.GetString(9)
                        });
                    }
                }
            }
        }

        private void LoadApprovalState(int leaveId, out string status, out int departmentHeadEmployeeId)
        {
            status = LeaveApprovalStatuses.Approved;
            departmentHeadEmployeeId = 0;
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT ISNULL(Status, N'Approved'), ISNULL(DepartmentHeadEmployeeID, 0)
FROM tblLeaveApplication WHERE LeaveID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", leaveId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;
                    status = dr.GetString(0);
                    departmentHeadEmployeeId = Convert.ToInt32(dr[1]);
                }
            }
        }

        private void LoadForEdit(int leaveId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT LeaveID, ApplyingDate, EmployeeID, LeaveType, IsFutureUnplannedLeave,
       LeaveCategoryID, LeaveFromDate, LeaveToDate, NumberOfDays,
       ReasonForLeave, TempResponsibleEmployeeID, PermanentResponsibleEmployeeID,
       ISNULL(Status, N'Approved'), ISNULL(DepartmentHeadEmployeeID, 0)
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
                        PermanentResponsibleEmployeeID = dr.IsDBNull(11) ? 0 : dr.GetInt32(11),
                        Status = dr.IsDBNull(12) ? LeaveApprovalStatuses.Approved : dr.GetString(12),
                        DepartmentHeadEmployeeID = dr.IsDBNull(13) ? 0 : Convert.ToInt32(dr[13])
                    };
                }
            }
        }

        private EmployeeLeaveSnapshot LoadEmployeeSnapshot(int employeeId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT ISNULL(e.EmployeeCode,''),
       LTRIM(RTRIM(ISNULL(e.FirstName,'') + ' ' + ISNULL(e.LastName,''))),
       ISNULL(e.FathersHusbandsName,''),
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
                        EmployeeCode = dr.IsDBNull(0) ? "" : dr.GetString(0),
                        EmployeeName = dr.IsDBNull(1) ? "" : dr.GetString(1),
                        FatherName = dr.IsDBNull(2) ? "" : dr.GetString(2),
                        Division = dr.IsDBNull(3) ? "" : dr.GetString(3),
                        Department = dr.IsDBNull(4) ? "" : dr.GetString(4),
                        Designation = dr.IsDBNull(5) ? "" : dr.GetString(5),
                        Region = dr.IsDBNull(6) ? "" : dr.GetString(6),
                        Location = dr.IsDBNull(7) ? "" : dr.GetString(7)
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
FROM tblEmployee e WHERE e.Status='Active' {scope.Sql} ORDER BY e.EmployeeCode, e.FirstName, e.LastName;", conn))
                {
                    scope.ApplyTo(cmd);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
                            var name = ((dr.IsDBNull(2) ? "" : dr.GetString(2)) + " " + (dr.IsDBNull(3) ? "" : dr.GetString(3))).Trim();
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
            using (var cmd = new SqlCommand(@"
SELECT EmployeeID, ISNULL(DepartmentHeadEmployeeID,0), ISNULL(Status, N'Approved')
FROM tblLeaveApplication WHERE LeaveID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", leaveId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return false;
                    var empId = Convert.ToInt32(dr[0]);
                    var dhId = Convert.ToInt32(dr[1]);
                    var status = dr.GetString(2);
                    if (_approval.CanViewLeave(empId, dhId, status)) return true;
                    return _dataScope.CanAccessEmployee(empId);
                }
            }
        }

        private void EnsureLeaveSchema()
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'tblLeaveCategory', N'U') IS NULL
CREATE TABLE tblLeaveCategory (
    LeaveCategoryID INT IDENTITY(1,1) PRIMARY KEY,
    LeaveCategoryName NVARCHAR(150) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn DATETIME NULL,
    CreatedByUserID INT NULL,
    ModifiedByUserID INT NULL
);
IF OBJECT_ID(N'tblLeaveApplication', N'U') IS NULL
CREATE TABLE tblLeaveApplication (
    LeaveID INT IDENTITY(1,1) PRIMARY KEY,
    ApplyingDate DATE NOT NULL,
    EmployeeID INT NOT NULL,
    LeaveType NVARCHAR(50) NOT NULL,
    IsFutureUnplannedLeave BIT NOT NULL DEFAULT 0,
    LeaveCategoryID INT NULL,
    LeaveFromDate DATE NOT NULL,
    LeaveToDate DATE NOT NULL,
    NumberOfDays INT NOT NULL DEFAULT 0,
    ReasonForLeave NVARCHAR(1000) NULL,
    TempResponsibleEmployeeID INT NULL,
    PermanentResponsibleEmployeeID INT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT N'Approved',
    DepartmentHeadEmployeeID INT NULL,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedOn DATETIME NULL,
    CreatedByUserID INT NULL,
    ModifiedByUserID INT NULL
);
IF OBJECT_ID(N'tblLeaveCategory', N'U') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM tblLeaveCategory)
BEGIN
    INSERT INTO tblLeaveCategory (LeaveCategoryName) VALUES
    (N'Casual'), (N'Annual'), (N'Maternity'), (N'Sick'), (N'Leave without pay');
END;", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // App identity may lack DDL; run Production_LeaveApplication_Patch.sql instead.
            }
        }

        private static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            DateTime parsed;
            if (DateTime.TryParse(value, out parsed)) return parsed.Date;
            return null;
        }

        public static int CalculateDays(DateTime fromDate, DateTime toDate) =>
            (toDate.Date - fromDate.Date).Days + 1;

        public string EmpName(int employeeId)
        {
            if (employeeId <= 0) return "";
            var match = Employees.Find(e => e.Id == employeeId);
            return match != null ? match.Name : "";
        }
    }
}
