using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
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

    public partial class PerformanceMasterPage : AppBasePage
    {
        private readonly DataAccessScopeService _dataScope = new DataAccessScopeService();

        public static readonly string[] ReviewCycleOptions =
            { "Annual", "Semi-Annual", "Quarterly", "Probation", "Ad-hoc" };

        public static readonly string[] RatingOptions =
            { "Outstanding", "Exceeds Expectations", "Meets Expectations", "Needs Improvement", "Unsatisfactory" };

        public string PageTitle => "Employee Performance";
        public List<PerformanceListItem> Records { get; set; } = new List<PerformanceListItem>();
        public List<LookupItem> Employees { get; set; } = new List<LookupItem>();
        public PerformanceInput Input { get; set; } = new PerformanceInput();
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
                    OnPostDelete(FormInt("deleteId"));
                    return;
                }
                OnPostSave();
                return;
            }

            var newRecord = Request.QueryString["newRecord"] == "1"
                || string.Equals(Request.QueryString["newRecord"], "true", StringComparison.OrdinalIgnoreCase);
            OnGet(QueryInt("editId"), newRecord);
        }

        private void OnGet(int? editId, bool newRecord)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            ShowForm = (editId.HasValue && editId > 0) || newRecord;

            if (ShowForm)
            {
                if (editId.HasValue && editId > 0 && !CanAccessPerformance(editId.Value))
                {
                    SetAlert(DataAccessScopeService.AccessDeniedMessage, "error");
                    Response.Redirect("~/PerformanceMaster.aspx");
                    return;
                }
                if (newRecord && !Perms.CanWrite("PerformanceMaster"))
                {
                    SetAlert(PermissionService.AccessRestrictedMessage, "error");
                    Response.Redirect("~/PerformanceMaster.aspx");
                    return;
                }

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

        private void OnPostSave()
        {
            EditMode = FormBool("EditMode") || FormInt("PerformanceID") > 0;
            Input = new PerformanceInput
            {
                PerformanceID = FormInt("PerformanceID"),
                EmployeeID = FormInt("EmployeeID"),
                PerformanceReviewCycle = FormString("PerformanceReviewCycle"),
                LastReviewDate = FormString("LastReviewDate"),
                LastReviewRating = FormString("LastReviewRating"),
                LastReviewScore = FormString("LastReviewScore"),
                NextReviewDue = FormString("NextReviewDue"),
                KPIsAssigned = FormBool("KPIsAssigned"),
                GoalAchievementPercent = FormString("GoalAchievementPercent"),
                PerformanceImprovementPlan = FormBool("PerformanceImprovementPlan"),
                CareerPath = FormString("CareerPath"),
                PromotionReady = FormBool("PromotionReady"),
                SuccessionPool = FormBool("SuccessionPool")
            };

            if (Input.EmployeeID <= 0)
            {
                SetFormError("Employee is required.");
                return;
            }
            if (!_dataScope.CanAccessEmployee(Input.EmployeeID))
            {
                SetFormError(DataAccessScopeService.AccessDeniedMessage);
                return;
            }
            if (EditMode && Input.PerformanceID > 0 && !CanAccessPerformance(Input.PerformanceID))
            {
                SetFormError(DataAccessScopeService.AccessDeniedMessage);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    SaveRecord(conn, Input);
                }
                SetAlert(EditMode ? "Performance record updated successfully." : "Performance record added successfully.");
                Response.Redirect("~/PerformanceMaster.aspx?editId=" + Input.PerformanceID);
            }
            catch (Exception ex)
            {
                SetFormError("Error: " + ex.Message);
            }
        }

        private void OnPostDelete(int deleteId)
        {
            if (!Perms.CanDelete("PerformanceMaster") || !CanAccessPerformance(deleteId))
            {
                SetAlert(DataAccessScopeService.AccessDeniedMessage, "error");
                Response.Redirect("~/PerformanceMaster.aspx");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand("DELETE FROM tblEmployeePerformance WHERE PerformanceID = @Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", deleteId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Performance record deleted successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error deleting record: " + ex.Message, "error");
            }
            Response.Redirect("~/PerformanceMaster.aspx");
        }

        private void SaveRecord(SqlConnection conn, PerformanceInput input)
        {
            if (input.PerformanceID > 0)
            {
                using (var cmd = new SqlCommand(@"
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
WHERE PerformanceID = @PerformanceID;", conn))
                {
                    BindParams(cmd, input);
                    cmd.Parameters.AddWithValue("@PerformanceID", input.PerformanceID);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    cmd.ExecuteNonQuery();
                }
                return;
            }

            using (var ins = new SqlCommand(@"
INSERT INTO tblEmployeePerformance
    (EmployeeID, PerformanceReviewCycle, LastReviewDate, LastReviewRating, LastReviewScore,
     NextReviewDue, KPIsAssigned, GoalAchievementPercent, PerformanceImprovementPlan,
     CareerPath, PromotionReady, SuccessionPool, CreatedOn, CreatedByUserID)
VALUES
    (@EmployeeID, @PerformanceReviewCycle, @LastReviewDate, @LastReviewRating, @LastReviewScore,
     @NextReviewDue, @KPIsAssigned, @GoalAchievementPercent, @PerformanceImprovementPlan,
     @CareerPath, @PromotionReady, @SuccessionPool, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
            {
                BindParams(ins, input);
                AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                input.PerformanceID = Convert.ToInt32(ins.ExecuteScalar());
            }
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
            Records.Clear();
            var scope = _dataScope.GetEmployeeFilter("e");
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT p.PerformanceID, e.EmployeeCode,
       e.FirstName + ' ' + e.LastName AS EmployeeName,
       ISNULL(p.PerformanceReviewCycle, ''), p.LastReviewDate,
       ISNULL(p.LastReviewRating, ''), p.LastReviewScore, p.NextReviewDue
FROM tblEmployeePerformance p
INNER JOIN tblEmployee e ON e.EmployeeID = p.EmployeeID
WHERE 1=1 " + scope.Sql + @"
ORDER BY p.PerformanceID DESC;", conn))
            {
                scope.ApplyTo(cmd);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Records.Add(new PerformanceListItem
                        {
                            PerformanceID = dr.GetInt32(0),
                            EmployeeCode = dr.IsDBNull(1) ? "" : dr.GetString(1),
                            EmployeeName = dr.GetString(2),
                            PerformanceReviewCycle = dr.GetString(3),
                            LastReviewDate = dr.IsDBNull(4) ? (DateTime?)null : dr.GetDateTime(4),
                            LastReviewRating = dr.GetString(5),
                            LastReviewScore = dr.IsDBNull(6) ? (decimal?)null : dr.GetDecimal(6),
                            NextReviewDue = dr.IsDBNull(7) ? (DateTime?)null : dr.GetDateTime(7)
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT PerformanceID, EmployeeID, PerformanceReviewCycle, LastReviewDate, LastReviewRating,
       LastReviewScore, NextReviewDue, KPIsAssigned, GoalAchievementPercent,
       PerformanceImprovementPlan, CareerPath, PromotionReady, SuccessionPool
FROM tblEmployeePerformance WHERE PerformanceID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
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
            }
        }

        private void LoadEmployees()
        {
            Employees = new List<LookupItem>();
            var scope = _dataScope.GetEmployeeFilter("e");
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT e.EmployeeID, e.EmployeeCode, e.FirstName, e.LastName
FROM tblEmployee e WHERE e.Status = 'Active' " + scope.Sql + @"
ORDER BY e.FirstName, e.LastName;", conn))
            {
                scope.ApplyTo(cmd);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var code = dr.IsDBNull(1) ? "" : dr.GetString(1);
                        var name = (dr.GetString(2) + " " + dr.GetString(3)).Trim();
                        Employees.Add(new LookupItem
                        {
                            Id = dr.GetInt32(0),
                            Name = string.IsNullOrEmpty(code) ? name : (code + " - " + name)
                        });
                    }
                }
            }
        }

        private bool CanAccessPerformance(int performanceId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(
                "SELECT EmployeeID FROM tblEmployeePerformance WHERE PerformanceID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", performanceId);
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return false;
                return _dataScope.CanAccessEmployee(Convert.ToInt32(result));
            }
        }

        private void SetFormError(string message)
        {
            AlertMessage = message;
            AlertType = "error";
            ShowForm = true;
            EditMode = Input.PerformanceID > 0;
            LoadEmployees();
        }

        private int FormInt(string name)
        {
            int.TryParse(Request.Form[name], out var id);
            return id;
        }

        private static object NullStr(string v) =>
            string.IsNullOrWhiteSpace(v) ? (object)DBNull.Value : v.Trim();

        private static object ParseDate(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return DBNull.Value;
            DateTime dt;
            if (DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)
                || DateTime.TryParse(v, out dt))
                return dt.Date;
            return DBNull.Value;
        }

        private static object ParseDecimal(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return DBNull.Value;
            decimal d;
            if (decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out d)
                || decimal.TryParse(v, out d))
                return d;
            return DBNull.Value;
        }
    }
}
