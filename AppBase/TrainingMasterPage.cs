using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class TrainingListItem
    {
        public int EmployeeTrainingID { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string TrainingName { get; set; } = "";
        public string TrainingCode { get; set; } = "";
        public string MandatoryTrainingStatus { get; set; } = "";
        public string TrainingDepartment { get; set; } = "";
        public DateTime? LastTrainingDate { get; set; }
        public DateTime? NextTrainingDue { get; set; }
        public decimal? TrainingHoursYTD { get; set; }
        public decimal? TrainingHoursRequiredAnnual { get; set; }
    }

    public class TrainingInput
    {
        public int EmployeeTrainingID { get; set; }
        public int EmployeeID { get; set; }
        public string MandatoryTrainingStatus { get; set; } = "";
        public string SafetyTrainingValidTill { get; set; } = "";
        public string GMPTrainingValidTill { get; set; } = "";
        public string TrainingHoursYTD { get; set; } = "";
        public string TrainingHoursRequiredAnnual { get; set; } = "";
        public string LastTrainingDate { get; set; } = "";
        public string NextTrainingDue { get; set; } = "";
        public string TrainingName { get; set; } = "";
        public string TrainingCode { get; set; } = "";
        public string TrainingDepartment { get; set; } = "All";
    }

    /// <summary>
    /// Training Master logic (partial). Code-behind stub lives in TrainingMaster.aspx.cs.
    /// </summary>
    public partial class TrainingMasterPage : AppBasePage
    {
        private readonly DataAccessScopeService _dataScope = new DataAccessScopeService();

        public static readonly string[] MandatoryStatuses =
        {
            "Completed", "In Progress", "Not Started", "Overdue", "Exempt", "Not Applicable"
        };

        public string PageTitle => "Training Master";
        public List<TrainingListItem> Records { get; set; } = new List<TrainingListItem>();
        public List<LookupItem> Employees { get; set; } = new List<LookupItem>();
        public List<LookupItem> Departments { get; set; } = new List<LookupItem>();
        public TrainingInput Input { get; set; } = new TrainingInput();
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
                if (editId.HasValue && editId > 0 && !CanAccessTraining(editId.Value))
                {
                    SetAlert(DataAccessScopeService.AccessDeniedMessage, "error");
                    Response.Redirect("~/TrainingMaster.aspx");
                    return;
                }
                if (newRecord && !Perms.CanWrite("TrainingMaster"))
                {
                    SetAlert(PermissionService.AccessRestrictedMessage, "error");
                    Response.Redirect("~/TrainingMaster.aspx");
                    return;
                }

                LoadEmployees();
                LoadDepartments();
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
            EditMode = FormBool("EditMode") || FormInt("EmployeeTrainingID") > 0;
            Input = new TrainingInput
            {
                EmployeeTrainingID = FormInt("EmployeeTrainingID"),
                EmployeeID = FormInt("EmployeeID"),
                MandatoryTrainingStatus = FormString("MandatoryTrainingStatus"),
                SafetyTrainingValidTill = FormString("SafetyTrainingValidTill"),
                GMPTrainingValidTill = FormString("GMPTrainingValidTill"),
                TrainingHoursYTD = FormString("TrainingHoursYTD"),
                TrainingHoursRequiredAnnual = FormString("TrainingHoursRequiredAnnual"),
                LastTrainingDate = FormString("LastTrainingDate"),
                NextTrainingDue = FormString("NextTrainingDue"),
                TrainingName = FormString("TrainingName"),
                TrainingCode = FormString("TrainingCode"),
                TrainingDepartment = string.IsNullOrWhiteSpace(FormString("TrainingDepartment"))
                    ? "All"
                    : FormString("TrainingDepartment")
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
            if (EditMode && Input.EmployeeTrainingID > 0 && !CanAccessTraining(Input.EmployeeTrainingID))
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
                SetAlert(EditMode ? "Training record updated successfully." : "Training record added successfully.");
                Response.Redirect("~/TrainingMaster.aspx?editId=" + Input.EmployeeTrainingID);
            }
            catch (Exception ex)
            {
                SetFormError("Error: " + ex.Message);
            }
        }

        private void OnPostDelete(int deleteId)
        {
            if (!Perms.CanDelete("TrainingMaster") || !CanAccessTraining(deleteId))
            {
                SetAlert(DataAccessScopeService.AccessDeniedMessage, "error");
                Response.Redirect("~/TrainingMaster.aspx");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand("DELETE FROM tblEmployeeTraining WHERE EmployeeTrainingID = @Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", deleteId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Training record deleted successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error deleting record: " + ex.Message, "error");
            }
            Response.Redirect("~/TrainingMaster.aspx");
        }

        private void SaveRecord(SqlConnection conn, TrainingInput input)
        {
            if (input.EmployeeTrainingID > 0)
            {
                using (var cmd = new SqlCommand(@"
UPDATE tblEmployeeTraining SET
    EmployeeID = @EmployeeID,
    MandatoryTrainingStatus = @MandatoryTrainingStatus,
    SafetyTrainingValidTill = @SafetyTrainingValidTill,
    GMPTrainingValidTill = @GMPTrainingValidTill,
    TrainingHoursYTD = @TrainingHoursYTD,
    TrainingHoursRequiredAnnual = @TrainingHoursRequiredAnnual,
    LastTrainingDate = @LastTrainingDate,
    NextTrainingDue = @NextTrainingDue,
    TrainingName = @TrainingName,
    TrainingCode = @TrainingCode,
    TrainingDepartment = @TrainingDepartment,
    ModifiedOn = GETDATE(),
    ModifiedByUserID = @ModifiedByUserID
WHERE EmployeeTrainingID = @EmployeeTrainingID;", conn))
                {
                    BindParams(cmd, input);
                    cmd.Parameters.AddWithValue("@EmployeeTrainingID", input.EmployeeTrainingID);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    cmd.ExecuteNonQuery();
                }
                return;
            }

            using (var ins = new SqlCommand(@"
INSERT INTO tblEmployeeTraining
    (EmployeeID, MandatoryTrainingStatus, SafetyTrainingValidTill, GMPTrainingValidTill,
     TrainingHoursYTD, TrainingHoursRequiredAnnual, LastTrainingDate, NextTrainingDue,
     TrainingName, TrainingCode, TrainingDepartment, CreatedOn, CreatedByUserID)
VALUES
    (@EmployeeID, @MandatoryTrainingStatus, @SafetyTrainingValidTill, @GMPTrainingValidTill,
     @TrainingHoursYTD, @TrainingHoursRequiredAnnual, @LastTrainingDate, @NextTrainingDue,
     @TrainingName, @TrainingCode, @TrainingDepartment, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
            {
                BindParams(ins, input);
                AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                input.EmployeeTrainingID = Convert.ToInt32(ins.ExecuteScalar());
            }
        }

        private static void BindParams(SqlCommand cmd, TrainingInput input)
        {
            cmd.Parameters.AddWithValue("@EmployeeID", input.EmployeeID);
            cmd.Parameters.AddWithValue("@MandatoryTrainingStatus", NullStr(input.MandatoryTrainingStatus));
            cmd.Parameters.AddWithValue("@SafetyTrainingValidTill", ParseDate(input.SafetyTrainingValidTill));
            cmd.Parameters.AddWithValue("@GMPTrainingValidTill", ParseDate(input.GMPTrainingValidTill));
            cmd.Parameters.AddWithValue("@TrainingHoursYTD", ParseDecimal(input.TrainingHoursYTD));
            cmd.Parameters.AddWithValue("@TrainingHoursRequiredAnnual", ParseDecimal(input.TrainingHoursRequiredAnnual));
            cmd.Parameters.AddWithValue("@LastTrainingDate", ParseDate(input.LastTrainingDate));
            cmd.Parameters.AddWithValue("@NextTrainingDue", ParseDate(input.NextTrainingDue));
            cmd.Parameters.AddWithValue("@TrainingName", NullStr(input.TrainingName));
            cmd.Parameters.AddWithValue("@TrainingCode", NullStr(input.TrainingCode));
            cmd.Parameters.AddWithValue("@TrainingDepartment",
                string.IsNullOrWhiteSpace(input.TrainingDepartment) ? "All" : input.TrainingDepartment);
        }

        private void LoadRecords()
        {
            Records.Clear();
            var scope = _dataScope.GetEmployeeFilter("e");
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT
    t.EmployeeTrainingID,
    e.EmployeeCode,
    e.FirstName + ' ' + e.LastName AS EmployeeName,
    ISNULL(t.TrainingName, '') AS TrainingName,
    ISNULL(t.TrainingCode, '') AS TrainingCode,
    ISNULL(t.MandatoryTrainingStatus, '') AS MandatoryTrainingStatus,
    ISNULL(t.TrainingDepartment, 'All') AS TrainingDepartment,
    t.LastTrainingDate,
    t.NextTrainingDue,
    t.TrainingHoursYTD,
    t.TrainingHoursRequiredAnnual
FROM tblEmployeeTraining t
INNER JOIN tblEmployee e ON e.EmployeeID = t.EmployeeID
WHERE 1=1 " + scope.Sql + @"
ORDER BY t.EmployeeTrainingID DESC;", conn))
            {
                scope.ApplyTo(cmd);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Records.Add(new TrainingListItem
                        {
                            EmployeeTrainingID = dr.GetInt32(0),
                            EmployeeCode = dr.IsDBNull(1) ? "" : dr.GetString(1),
                            EmployeeName = dr.GetString(2),
                            TrainingName = dr.GetString(3),
                            TrainingCode = dr.GetString(4),
                            MandatoryTrainingStatus = dr.GetString(5),
                            TrainingDepartment = dr.GetString(6),
                            LastTrainingDate = dr.IsDBNull(7) ? (DateTime?)null : dr.GetDateTime(7),
                            NextTrainingDue = dr.IsDBNull(8) ? (DateTime?)null : dr.GetDateTime(8),
                            TrainingHoursYTD = dr.IsDBNull(9) ? (decimal?)null : dr.GetDecimal(9),
                            TrainingHoursRequiredAnnual = dr.IsDBNull(10) ? (decimal?)null : dr.GetDecimal(10)
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int trainingId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT EmployeeTrainingID, EmployeeID, MandatoryTrainingStatus, SafetyTrainingValidTill,
       GMPTrainingValidTill, TrainingHoursYTD, TrainingHoursRequiredAnnual,
       LastTrainingDate, NextTrainingDue, TrainingName, TrainingCode, TrainingDepartment
FROM tblEmployeeTraining
WHERE EmployeeTrainingID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", trainingId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;
                    Input = new TrainingInput
                    {
                        EmployeeTrainingID = dr.GetInt32(0),
                        EmployeeID = dr.GetInt32(1),
                        MandatoryTrainingStatus = dr.IsDBNull(2) ? "" : dr.GetString(2),
                        SafetyTrainingValidTill = dr.IsDBNull(3) ? "" : dr.GetDateTime(3).ToString("yyyy-MM-dd"),
                        GMPTrainingValidTill = dr.IsDBNull(4) ? "" : dr.GetDateTime(4).ToString("yyyy-MM-dd"),
                        TrainingHoursYTD = dr.IsDBNull(5) ? "" : dr.GetDecimal(5).ToString("0.##"),
                        TrainingHoursRequiredAnnual = dr.IsDBNull(6) ? "" : dr.GetDecimal(6).ToString("0.##"),
                        LastTrainingDate = dr.IsDBNull(7) ? "" : dr.GetDateTime(7).ToString("yyyy-MM-dd"),
                        NextTrainingDue = dr.IsDBNull(8) ? "" : dr.GetDateTime(8).ToString("yyyy-MM-dd"),
                        TrainingName = dr.IsDBNull(9) ? "" : dr.GetString(9),
                        TrainingCode = dr.IsDBNull(10) ? "" : dr.GetString(10),
                        TrainingDepartment = dr.IsDBNull(11) ? "All" : dr.GetString(11)
                    };
                }
            }
        }

        private void LoadEmployees()
        {
            Employees.Clear();
            var scope = _dataScope.GetEmployeeFilter("e");
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT e.EmployeeID, e.EmployeeCode, e.FirstName, e.LastName
FROM tblEmployee e
WHERE e.Status = 'Active' " + scope.Sql + @"
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
                            Name = string.IsNullOrEmpty(code) ? name : code + " – " + name
                        });
                    }
                }
            }
        }

        private void LoadDepartments()
        {
            Departments = new List<LookupItem> { new LookupItem { Id = 0, Name = "All" } };
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT DepartmentID, DepartmentName
FROM tblDepartment
WHERE IsActive = 1
ORDER BY DepartmentName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Departments.Add(new LookupItem
                        {
                            Id = dr.GetInt32(0),
                            Name = dr.GetString(1)
                        });
                    }
                }
            }
        }

        private bool CanAccessTraining(int trainingId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(
                "SELECT EmployeeID FROM tblEmployeeTraining WHERE EmployeeTrainingID = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", trainingId);
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
            LoadEmployees();
            LoadDepartments();
        }

        private static object NullStr(string value) =>
            string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();

        private static object ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
            DateTime d;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d;
            if (DateTime.TryParse(value, out d)) return d;
            return DBNull.Value;
        }

        private static object ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
            decimal d;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                return d;
            if (decimal.TryParse(value, out d)) return d;
            return DBNull.Value;
        }
    }
}
