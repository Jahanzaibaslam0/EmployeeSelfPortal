using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class PerformanceListItem
{
    public int PerformanceID { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string PerformanceReviewCycle { get; set; } = "";
    public DateTime? LastReviewDate { get; set; }
    public string LastReviewRating { get; set; } = "";
    public decimal? LastReviewScore { get; set; }
    public DateTime? NextReviewDue { get; set; }
}

public class PerformanceInput
{
    public int PerformanceID { get; set; }
    public int EmployeeID { get; set; }
    public string PerformanceReviewCycle { get; set; } = "";
    public string LastReviewDate { get; set; } = "";
    public string LastReviewRating { get; set; } = "";
    public string LastReviewScore { get; set; } = "";
    public string NextReviewDue { get; set; } = "";
    public bool KPIsAssigned { get; set; }
    public string GoalAchievementPercent { get; set; } = "";
    public bool PerformanceImprovementPlan { get; set; }
    public string CareerPath { get; set; } = "";
    public bool PromotionReady { get; set; }
    public bool SuccessionPool { get; set; }
}

public class PerformanceMasterModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly DataAccessScopeService _dataScope;

    public static readonly string[] ReviewCycleOptions =
        { "Annual", "Semi-Annual", "Quarterly", "Probation", "Ad-hoc" };

    public static readonly string[] RatingOptions =
        { "Outstanding", "Exceeds Expectations", "Meets Expectations", "Needs Improvement", "Unsatisfactory" };

    public PerformanceMasterModel(IConfiguration config, AuthService auth, DataAccessScopeService dataScope)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
        _dataScope = dataScope;
    }

    public List<PerformanceListItem> Records { get; set; } = new();
    public List<LookupItem> Employees { get; set; } = new();
    public PerformanceInput Input { get; set; } = new();
    public IReadOnlyList<string> ReviewCycleOptionsList => ReviewCycleOptions;
    public IReadOnlyList<string> RatingOptionsList => RatingOptions;
    public bool EditMode { get; set; }
    public bool ShowForm { get; set; }
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet([FromQuery] int? editId, [FromQuery] bool? newRecord)
    {
        LoadAlert();
        ShowForm = (editId.HasValue && editId > 0) || newRecord == true;

        if (ShowForm)
        {
            LoadEmployees();
            if (editId.HasValue && editId > 0)
            {
                LoadForEdit(editId.Value);
                EditMode = true;
            }
        }
        else
        {
            LoadRecords();
        }
    }

    public IActionResult OnPost(
        int PerformanceID, bool EditMode, int EmployeeID, string PerformanceReviewCycle,
        string LastReviewDate, string LastReviewRating, string LastReviewScore, string NextReviewDue,
        bool KPIsAssigned, string GoalAchievementPercent, bool PerformanceImprovementPlan,
        string CareerPath, bool PromotionReady, bool SuccessionPool)
    {
        this.EditMode = EditMode;
        Input = new PerformanceInput
        {
            PerformanceID = PerformanceID,
            EmployeeID = EmployeeID,
            PerformanceReviewCycle = PerformanceReviewCycle ?? "",
            LastReviewDate = LastReviewDate ?? "",
            LastReviewRating = LastReviewRating ?? "",
            LastReviewScore = LastReviewScore ?? "",
            NextReviewDue = NextReviewDue ?? "",
            KPIsAssigned = KPIsAssigned,
            GoalAchievementPercent = GoalAchievementPercent ?? "",
            PerformanceImprovementPlan = PerformanceImprovementPlan,
            CareerPath = CareerPath ?? "",
            PromotionReady = PromotionReady,
            SuccessionPool = SuccessionPool
        };

        if (EmployeeID <= 0)
        {
            AlertMessage = "Employee is required.";
            AlertType = "error";
            LoadEmployees();
            ShowForm = true;
            return Page();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            SaveRecord(conn, Input);

            TempData["Alert"] = EditMode ? "Performance record updated successfully." : "Performance record added successfully.";
            TempData["AlertType"] = "success";
            return RedirectToPage(new { editId = Input.PerformanceID });
        }
        catch (Exception ex)
        {
            AlertMessage = "Error: " + ex.Message;
            AlertType = "error";
            LoadEmployees();
            ShowForm = true;
            return Page();
        }
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM tblEmployeePerformance WHERE PerformanceID = @Id;", conn);
            cmd.Parameters.AddWithValue("@Id", deleteId);
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Performance record deleted successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error deleting record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private void SaveRecord(SqlConnection conn, PerformanceInput input)
    {
        if (input.PerformanceID > 0)
        {
            using var cmd = new SqlCommand(@"
                UPDATE tblEmployeePerformance SET
                    EmployeeID = @EmployeeID,
                    PerformanceReviewCycle = @PerformanceReviewCycle,
                    LastReviewDate = @LastReviewDate,
                    LastReviewRating = @LastReviewRating,
                    LastReviewScore = @LastReviewScore,
                    NextReviewDue = @NextReviewDue,
                    KPIsAssigned = @KPIsAssigned,
                    GoalAchievementPercent = @GoalAchievementPercent,
                    PerformanceImprovementPlan = @PerformanceImprovementPlan,
                    CareerPath = @CareerPath,
                    PromotionReady = @PromotionReady,
                    SuccessionPool = @SuccessionPool,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE PerformanceID = @PerformanceID;", conn);

            BindParams(cmd, input);
            cmd.Parameters.AddWithValue("@PerformanceID", input.PerformanceID);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();
            return;
        }

        using var ins = new SqlCommand(@"
            INSERT INTO tblEmployeePerformance
                (EmployeeID, PerformanceReviewCycle, LastReviewDate, LastReviewRating, LastReviewScore,
                 NextReviewDue, KPIsAssigned, GoalAchievementPercent, PerformanceImprovementPlan,
                 CareerPath, PromotionReady, SuccessionPool, CreatedOn, CreatedByUserID)
            VALUES
                (@EmployeeID, @PerformanceReviewCycle, @LastReviewDate, @LastReviewRating, @LastReviewScore,
                 @NextReviewDue, @KPIsAssigned, @GoalAchievementPercent, @PerformanceImprovementPlan,
                 @CareerPath, @PromotionReady, @SuccessionPool, GETDATE(), @CreatedByUserID);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);

        BindParams(ins, input);
        AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
        input.PerformanceID = (int)ins.ExecuteScalar()!;
    }

    private static void BindParams(SqlCommand cmd, PerformanceInput input)
    {
        cmd.Parameters.AddWithValue("@EmployeeID", input.EmployeeID);
        cmd.Parameters.AddWithValue("@PerformanceReviewCycle", NullStr(input.PerformanceReviewCycle));
        cmd.Parameters.AddWithValue("@LastReviewDate", ParseDate(input.LastReviewDate));
        cmd.Parameters.AddWithValue("@LastReviewRating", NullStr(input.LastReviewRating));
        cmd.Parameters.AddWithValue("@LastReviewScore", ParseDecimal(input.LastReviewScore));
        cmd.Parameters.AddWithValue("@NextReviewDue", ParseDate(input.NextReviewDue));
        cmd.Parameters.AddWithValue("@KPIsAssigned", input.KPIsAssigned);
        cmd.Parameters.AddWithValue("@GoalAchievementPercent", ParseDecimal(input.GoalAchievementPercent));
        cmd.Parameters.AddWithValue("@PerformanceImprovementPlan", input.PerformanceImprovementPlan);
        cmd.Parameters.AddWithValue("@CareerPath", NullStr(input.CareerPath));
        cmd.Parameters.AddWithValue("@PromotionReady", input.PromotionReady);
        cmd.Parameters.AddWithValue("@SuccessionPool", input.SuccessionPool);
    }

    private void LoadRecords()
    {
        var scope = _dataScope.GetEmployeeFilter("e");
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT p.PerformanceID, e.EmployeeCode,
                   e.FirstName + ' ' + e.LastName AS EmployeeName,
                   ISNULL(p.PerformanceReviewCycle, ''), p.LastReviewDate,
                   ISNULL(p.LastReviewRating, ''), p.LastReviewScore, p.NextReviewDue
            FROM tblEmployeePerformance p
            INNER JOIN tblEmployee e ON e.EmployeeID = p.EmployeeID
            WHERE 1=1 {scope.Sql}
            ORDER BY p.PerformanceID DESC;", conn);
        scope.ApplyTo(cmd);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Records.Add(new PerformanceListItem
            {
                PerformanceID = dr.GetInt32(0),
                EmployeeCode = dr.IsDBNull(1) ? "" : dr.GetString(1),
                EmployeeName = dr.GetString(2),
                PerformanceReviewCycle = dr.GetString(3),
                LastReviewDate = dr.IsDBNull(4) ? null : dr.GetDateTime(4),
                LastReviewRating = dr.GetString(5),
                LastReviewScore = dr.IsDBNull(6) ? null : dr.GetDecimal(6),
                NextReviewDue = dr.IsDBNull(7) ? null : dr.GetDateTime(7)
            });
        }
    }

    private void LoadForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT PerformanceID, EmployeeID, PerformanceReviewCycle, LastReviewDate, LastReviewRating,
                   LastReviewScore, NextReviewDue, KPIsAssigned, GoalAchievementPercent,
                   PerformanceImprovementPlan, CareerPath, PromotionReady, SuccessionPool
            FROM tblEmployeePerformance WHERE PerformanceID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (!dr.Read()) return;

        Input = new PerformanceInput
        {
            PerformanceID = dr.GetInt32(0),
            EmployeeID = dr.GetInt32(1),
            PerformanceReviewCycle = dr.IsDBNull(2) ? "" : dr.GetString(2),
            LastReviewDate = dr.IsDBNull(3) ? "" : dr.GetDateTime(3).ToString("yyyy-MM-dd"),
            LastReviewRating = dr.IsDBNull(4) ? "" : dr.GetString(4),
            LastReviewScore = dr.IsDBNull(5) ? "" : dr.GetDecimal(5).ToString("0.##"),
            NextReviewDue = dr.IsDBNull(6) ? "" : dr.GetDateTime(6).ToString("yyyy-MM-dd"),
            KPIsAssigned = !dr.IsDBNull(7) && dr.GetBoolean(7),
            GoalAchievementPercent = dr.IsDBNull(8) ? "" : dr.GetDecimal(8).ToString("0.##"),
            PerformanceImprovementPlan = !dr.IsDBNull(9) && dr.GetBoolean(9),
            CareerPath = dr.IsDBNull(10) ? "" : dr.GetString(10),
            PromotionReady = !dr.IsDBNull(11) && dr.GetBoolean(11),
            SuccessionPool = !dr.IsDBNull(12) && dr.GetBoolean(12)
        };
    }

    private void LoadEmployees()
    {
        Employees = new List<LookupItem>();
        var scope = _dataScope.GetEmployeeFilter("e");
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT e.EmployeeID, e.EmployeeCode, e.FirstName, e.LastName
            FROM tblEmployee e WHERE e.Status = 'Active' {scope.Sql} ORDER BY e.FirstName, e.LastName;", conn);
        scope.ApplyTo(cmd);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
            var name = $"{dr.GetString(2)} {dr.GetString(3)}".Trim();
            Employees.Add(new LookupItem { Id = dr.GetInt32(0), Name = string.IsNullOrEmpty(code) ? name : $"{code} – {name}" });
        }
    }

    private void LoadAlert()
    {
        if (TempData["Alert"] is string msg) AlertMessage = msg;
        if (TempData["AlertType"] is string type) AlertType = type;
    }

    private static object NullStr(string? v) => string.IsNullOrWhiteSpace(v) ? DBNull.Value : v.Trim();
    private static object ParseDate(string? v) => string.IsNullOrWhiteSpace(v) ? DBNull.Value : (object)DateTime.Parse(v);
    private static object ParseDecimal(string? v) => string.IsNullOrWhiteSpace(v) ? DBNull.Value : (object)decimal.Parse(v);
}
