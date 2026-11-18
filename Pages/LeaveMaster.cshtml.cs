using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

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

public class LeaveMasterModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly PermissionService _perms;
    private readonly DataAccessScopeService _dataScope;

    public static readonly string[] LeaveTypes = { "Planned", "Unplanned", "Future Unplanned Leave" };

    public LeaveMasterModel(IConfiguration config, AuthService auth, PermissionService perms, DataAccessScopeService dataScope)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
        _perms = perms;
        _dataScope = dataScope;
    }

    public List<LeaveListItem> Leaves { get; set; } = new();
    public List<LookupItem> Employees { get; set; } = new();
    public List<LookupItem> LeaveCategories { get; set; } = new();
    public LeaveInput Input { get; set; } = new();
    public EmployeeLeaveSnapshot EmployeeInfo { get; set; } = new();
    public bool EditMode { get; set; }
    public bool ShowForm { get; set; }
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";
    public IReadOnlyList<string> LeaveTypeOptions => LeaveTypes;

    public IActionResult OnGet([FromQuery] int? editId, [FromQuery] bool? newLeave)
    {
        LoadAlert();
        ShowForm = (editId.HasValue && editId > 0) || newLeave == true;

        if (ShowForm)
        {
            if (editId.HasValue && editId > 0 && !CanAccessLeave(editId.Value))
            {
                TempData["Alert"] = DataAccessScopeService.AccessDeniedMessage;
                TempData["AlertType"] = "error";
                return RedirectToPage();
            }
            if (newLeave == true && !_perms.CanWrite("LeaveMaster"))
            {
                TempData["Alert"] = PermissionService.AccessRestrictedMessage;
                TempData["AlertType"] = "error";
                return RedirectToPage();
            }

            LoadLookups();
            if (editId.HasValue && editId > 0)
            {
                LoadForEdit(editId.Value);
                EditMode = true;
            }
            else
            {
                Input.ApplyingDate = DateTime.Today.ToString("yyyy-MM-dd");
            }

            if (Input.EmployeeID > 0)
                EmployeeInfo = LoadEmployeeSnapshot(Input.EmployeeID);
        }
        else
        {
            LoadLeaves();
        }
        return Page();
    }

    public JsonResult OnGetEmployeeInfo(int employeeId)
    {
        if (employeeId <= 0 || !_dataScope.CanAccessEmployee(employeeId))
            return new JsonResult(new EmployeeLeaveSnapshot());

        return new JsonResult(LoadEmployeeSnapshot(employeeId));
    }

    public IActionResult OnPost(
        int LeaveID,
        bool EditMode,
        string ApplyingDate,
        int EmployeeID,
        string LeaveType,
        bool IsFutureUnplannedLeave,
        int LeaveCategoryID,
        string LeaveFromDate,
        string LeaveToDate,
        int NumberOfDays,
        string ReasonForLeave,
        int TempResponsibleEmployeeID,
        int PermanentResponsibleEmployeeID)
    {
        this.EditMode = EditMode;
        Input = new LeaveInput
        {
            LeaveID = LeaveID,
            ApplyingDate = ApplyingDate ?? "",
            EmployeeID = EmployeeID,
            LeaveType = LeaveType ?? "Planned",
            IsFutureUnplannedLeave = IsFutureUnplannedLeave,
            LeaveCategoryID = LeaveCategoryID,
            LeaveFromDate = LeaveFromDate ?? "",
            LeaveToDate = LeaveToDate ?? "",
            NumberOfDays = NumberOfDays,
            ReasonForLeave = ReasonForLeave?.Trim() ?? "",
            TempResponsibleEmployeeID = TempResponsibleEmployeeID,
            PermanentResponsibleEmployeeID = PermanentResponsibleEmployeeID
        };

        if (EmployeeID <= 0)
        {
            AlertMessage = "Employee is required.";
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }

        if (!_dataScope.CanAccessEmployee(EmployeeID))
        {
            AlertMessage = DataAccessScopeService.AccessDeniedMessage;
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }

        if (EditMode && LeaveID > 0 && !CanAccessLeave(LeaveID))
        {
            AlertMessage = DataAccessScopeService.AccessDeniedMessage;
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }

        if (EditMode && !_perms.CanWrite("LeaveMaster"))
        {
            AlertMessage = PermissionService.AccessRestrictedMessage;
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }

        if (!EditMode && !_perms.CanWrite("LeaveMaster"))
        {
            AlertMessage = PermissionService.AccessRestrictedMessage;
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            return Page();
        }

        if (LeaveCategoryID <= 0)
        {
            AlertMessage = "Leave category is required.";
            AlertType = "error";
            LoadLookups();
            EmployeeInfo = LoadEmployeeSnapshot(EmployeeID);
            ShowForm = true;
            return Page();
        }

        var fromDate = ParseDate(Input.LeaveFromDate);
        var toDate = ParseDate(Input.LeaveToDate);
        if (!fromDate.HasValue || !toDate.HasValue)
        {
            AlertMessage = "Leave from and to dates are required.";
            AlertType = "error";
            LoadLookups();
            EmployeeInfo = LoadEmployeeSnapshot(EmployeeID);
            ShowForm = true;
            return Page();
        }

        if (toDate.Value < fromDate.Value)
        {
            AlertMessage = "Leave to date cannot be before from date.";
            AlertType = "error";
            LoadLookups();
            EmployeeInfo = LoadEmployeeSnapshot(EmployeeID);
            ShowForm = true;
            return Page();
        }

        Input.NumberOfDays = CalculateDays(fromDate.Value, toDate.Value);

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            SaveRecord(conn, Input);

            TempData["Alert"] = EditMode ? "Leave application updated successfully." : "Leave application submitted successfully.";
            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = Input.LeaveID });
        }
        catch (Exception ex)
        {
            AlertMessage = "Error: " + ex.Message;
            AlertType = "error";
            LoadLookups();
            EmployeeInfo = LoadEmployeeSnapshot(EmployeeID);
            ShowForm = true;
            return Page();
        }
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        if (!_perms.CanDelete("LeaveMaster") || !CanAccessLeave(deleteId))
        {
            TempData["Alert"] = DataAccessScopeService.AccessDeniedMessage;
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM tblLeaveApplication WHERE LeaveID = @Id;", conn);
            cmd.Parameters.AddWithValue("@Id", deleteId);
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Leave application deleted successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error deleting record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private void SaveRecord(SqlConnection conn, LeaveInput input)
    {
        var applyingDate = ParseDate(input.ApplyingDate) ?? DateTime.Today;
        var fromDate = ParseDate(input.LeaveFromDate)!.Value;
        var toDate = ParseDate(input.LeaveToDate)!.Value;
        var days = CalculateDays(fromDate, toDate);

        if (input.LeaveID > 0)
        {
            using var cmd = new SqlCommand(@"
                UPDATE tblLeaveApplication
                SET ApplyingDate = @ApplyingDate,
                    EmployeeID = @EmployeeID,
                    LeaveType = @LeaveType,
                    IsFutureUnplannedLeave = @IsFutureUnplannedLeave,
                    LeaveCategoryID = @LeaveCategoryID,
                    LeaveFromDate = @LeaveFromDate,
                    LeaveToDate = @LeaveToDate,
                    NumberOfDays = @NumberOfDays,
                    ReasonForLeave = @ReasonForLeave,
                    TempResponsibleEmployeeID = @TempResponsibleEmployeeID,
                    PermanentResponsibleEmployeeID = @PermanentResponsibleEmployeeID,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE LeaveID = @LeaveID;", conn);

            BindParams(cmd, input, applyingDate, fromDate, toDate, days);
            cmd.Parameters.AddWithValue("@LeaveID", input.LeaveID);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();
            return;
        }

        using var ins = new SqlCommand(@"
            INSERT INTO tblLeaveApplication
                (ApplyingDate, EmployeeID, LeaveType, IsFutureUnplannedLeave, LeaveCategoryID,
                 LeaveFromDate, LeaveToDate, NumberOfDays, ReasonForLeave,
                 TempResponsibleEmployeeID, PermanentResponsibleEmployeeID,
                 CreatedOn, CreatedByUserID)
            VALUES
                (@ApplyingDate, @EmployeeID, @LeaveType, @IsFutureUnplannedLeave, @LeaveCategoryID,
                 @LeaveFromDate, @LeaveToDate, @NumberOfDays, @ReasonForLeave,
                 @TempResponsibleEmployeeID, @PermanentResponsibleEmployeeID,
                 GETDATE(), @CreatedByUserID);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);

        BindParams(ins, input, applyingDate, fromDate, toDate, days);
        AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
        input.LeaveID = (int)ins.ExecuteScalar()!;
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
        cmd.Parameters.AddWithValue("@ReasonForLeave", string.IsNullOrWhiteSpace(input.ReasonForLeave) ? DBNull.Value : input.ReasonForLeave);
        cmd.Parameters.AddWithValue("@TempResponsibleEmployeeID", input.TempResponsibleEmployeeID > 0 ? input.TempResponsibleEmployeeID : DBNull.Value);
        cmd.Parameters.AddWithValue("@PermanentResponsibleEmployeeID", input.PermanentResponsibleEmployeeID > 0 ? input.PermanentResponsibleEmployeeID : DBNull.Value);
    }

    private void LoadLeaves()
    {
        var scope = _dataScope.GetEmployeeFilter("e");
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT
                l.LeaveID,
                l.ApplyingDate,
                e.EmployeeCode,
                e.FirstName + ' ' + e.LastName AS EmployeeName,
                l.LeaveType,
                ISNULL(c.LeaveCategoryName, '') AS LeaveCategoryName,
                l.LeaveFromDate,
                l.LeaveToDate,
                l.NumberOfDays
            FROM tblLeaveApplication l
            INNER JOIN tblEmployee e ON e.EmployeeID = l.EmployeeID
            LEFT JOIN tblLeaveCategory c ON c.LeaveCategoryID = l.LeaveCategoryID
            WHERE 1=1 {scope.Sql}
            ORDER BY l.LeaveID DESC;", conn);
        scope.ApplyTo(cmd);
        conn.Open();
        using var dr = cmd.ExecuteReader();
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

    private void LoadForEdit(int leaveId)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT LeaveID, ApplyingDate, EmployeeID, LeaveType, IsFutureUnplannedLeave,
                   LeaveCategoryID, LeaveFromDate, LeaveToDate, NumberOfDays,
                   ReasonForLeave, TempResponsibleEmployeeID, PermanentResponsibleEmployeeID
            FROM tblLeaveApplication
            WHERE LeaveID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", leaveId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
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

    private EmployeeLeaveSnapshot LoadEmployeeSnapshot(int employeeId)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT
                e.FirstName + ' ' + e.LastName AS EmployeeName,
                ISNULL(e.FathersHusbandsName, '') AS FatherName,
                ISNULL(dv.DivisionName, '') AS Division,
                ISNULL(d.DepartmentName, '') AS Department,
                ISNULL(e.Designation, '') AS Designation,
                ISNULL(r.RegionName, '') AS Region,
                ISNULL(l.LocationName, '') AS Location
            FROM tblEmployee e
            LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
            LEFT JOIN tblDivision dv ON dv.DivisionID = e.DivisionID
            LEFT JOIN tblRegion r ON r.RegionID = e.RegionID
            LEFT JOIN tblLocation l ON l.LocationID = e.LocationID
            WHERE e.EmployeeID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", employeeId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (!dr.Read())
            return new EmployeeLeaveSnapshot();

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

    private void LoadLookups()
    {
        Employees = LoadEmployeeLookup();
        LeaveCategories = LoadLookup("tblLeaveCategory", "LeaveCategoryID", "LeaveCategoryName");
    }

    private List<LookupItem> LoadEmployeeLookup()
    {
        var items = new List<LookupItem>();
        var scope = _dataScope.GetEmployeeFilter("e");
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT e.EmployeeID, e.EmployeeCode, e.FirstName, e.LastName
            FROM tblEmployee e
            WHERE e.Status = 'Active' {scope.Sql}
            ORDER BY e.FirstName, e.LastName;", conn);
        scope.ApplyTo(cmd);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
            var name = $"{dr.GetString(2)} {dr.GetString(3)}".Trim();
            items.Add(new LookupItem
            {
                Id = dr.GetInt32(0),
                Name = string.IsNullOrEmpty(code) ? name : $"{code} – {name}"
            });
        }
        return items;
    }

    private List<LookupItem> LoadLookup(string table, string idCol, string nameCol)
    {
        var items = new List<LookupItem>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT {idCol}, {nameCol}
            FROM {table}
            WHERE IsActive = 1
            ORDER BY {nameCol};", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            items.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
        }
        return items;
    }

    private void LoadAlert()
    {
        if (TempData["Alert"] is string msg)
            AlertMessage = msg;
        if (TempData["AlertType"] is string type)
            AlertType = type;
    }

    private static DateTime? ParseDate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : DateTime.Parse(value);

    private bool CanAccessLeave(int leaveId)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand("SELECT EmployeeID FROM tblLeaveApplication WHERE LeaveID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", leaveId);
        conn.Open();
        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value) return false;
        return _dataScope.CanAccessEmployee(Convert.ToInt32(result));
    }

    public static int CalculateDays(DateTime fromDate, DateTime toDate) =>
        (toDate.Date - fromDate.Date).Days + 1;
}
