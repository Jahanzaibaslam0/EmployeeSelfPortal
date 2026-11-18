using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class DesignationRecord
    {
        public int DesignationID { get; set; }
        public int TitleID { get; set; }
        public string TitleName { get; set; } = "";
        public int GradeID { get; set; }
        public string GradeName { get; set; } = "";
        public int EmployeeCategoryID { get; set; }
        public string EmployeeCategoryName { get; set; } = "";
        public string ProbationDays { get; set; } = "";
        public string PrivilegeCode { get; set; } = "NONE";
        public string PrivilegeTier { get; set; } = "None";
        public string PositionTenure { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class DesignationSetupPage : AppBasePage
    {
        public static readonly string[] PrivilegeCodes =
        {
            "NONE", "EXC-C", "EXC-D", "PRM-GM", "PRM-HOD-COM", "PRM-HOD-COR", "PRM-HOD-TEC",
            "STD-FLD-SALES", "STD-FLD-INTERNAL", "STD-TEC", "STD-OFF", "ESS-DC", "ESS-HO",
            "EXCL-FLD", "EXCL-DC", "NON-OPS", "NON-FLD"
        };

        public static readonly string[] PrivilegeTiers =
        {
            "None", "Executive", "Premium", "Standard", "Essential User", "Excluded"
        };

        public static readonly string[] PositionTenures =
        {
            "1–2 Years", "2–3 Years", "3–4 Years", "4–5 Years", "5–6 Years",
            "6–7 Years", "7–8 Years", "8–9 Years", "9–10 Years", "10+ Years"
        };

        private static readonly Regex WholeNumberRegex = new Regex(@"^\d+$", RegexOptions.Compiled);

        public string[] PrivilegeCodeOptions => PrivilegeCodes;
        public string[] PrivilegeTierOptions => PrivilegeTiers;
        public string[] PositionTenureOptions => PositionTenures;

        public string PageTitle => "Designation Setup";
        public DesignationRecord Input { get; private set; } = new DesignationRecord { IsActive = true };
        public List<DesignationRecord> Records { get; private set; } = new List<DesignationRecord>();
        public List<LookupItem> Titles { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Grades { get; private set; } = new List<LookupItem>();
        public List<LookupItem> EmployeeCategories { get; private set; } = new List<LookupItem>();
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            EmployeeCategorySetupPage.EnsureTable(Conn);
            EnsureTable(Conn);

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostDelete();
                    return;
                }
                OnPostSave();
                return;
            }

            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            LoadLookups();

            var editId = QueryInt("editId");
            if (editId.HasValue && editId.Value > 0)
                LoadForEdit(editId.Value);

            LoadRecords();
        }

        private void OnPostSave()
        {
            if (!Auth.IsAdmin && !Perms.CanWrite("DesignationSetup"))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect("~/DesignationSetup.aspx");
                return;
            }

            var designationId = FormInt("designationID");
            var titleId = FormInt("titleID");
            var gradeId = FormInt("gradeID");
            var employeeCategoryId = FormInt("employeeCategoryID");
            var probationDaysRaw = FormString("probationDays");
            var privilegeCode = FormString("privilegeCode");
            var privilegeTier = FormString("privilegeTier");
            var positionTenure = FormString("positionTenure");
            var isActive = FormBool("isActive");

            if (titleId <= 0)
            {
                SetAlert("Title is required.", "error");
                Response.Redirect(RedirectEdit(designationId));
                return;
            }
            if (gradeId <= 0)
            {
                SetAlert("Grade is required.", "error");
                Response.Redirect(RedirectEdit(designationId));
                return;
            }
            if (employeeCategoryId <= 0)
            {
                SetAlert("Employee Category is required.", "error");
                Response.Redirect(RedirectEdit(designationId));
                return;
            }
            if (!IsAllowed(PrivilegeCodes, privilegeCode))
            {
                SetAlert("Select a valid Privilege Code.", "error");
                Response.Redirect(RedirectEdit(designationId));
                return;
            }
            if (!IsAllowed(PrivilegeTiers, privilegeTier))
            {
                SetAlert("Select a valid Privilege Tier.", "error");
                Response.Redirect(RedirectEdit(designationId));
                return;
            }
            if (!string.IsNullOrWhiteSpace(positionTenure) && !IsAllowed(PositionTenures, positionTenure))
            {
                SetAlert("Select a valid Position Tenure.", "error");
                Response.Redirect(RedirectEdit(designationId));
                return;
            }

            int? probationDays = null;
            if (!string.IsNullOrWhiteSpace(probationDaysRaw))
            {
                if (!WholeNumberRegex.IsMatch(probationDaysRaw))
                {
                    SetAlert("Probation Days must be a whole number. Decimal and non-numeric values are not allowed.", "error");
                    Response.Redirect(RedirectEdit(designationId));
                    return;
                }
                probationDays = int.Parse(probationDaysRaw);
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (designationId > 0)
                    {
                        using (var cmd = new SqlCommand(@"
UPDATE tblDesignation SET
    TitleID = @TitleID,
    GradeID = @GradeID,
    EmployeeCategoryID = @EmployeeCategoryID,
    ProbationDays = @ProbationDays,
    PrivilegeCode = @PrivilegeCode,
    PrivilegeTier = @PrivilegeTier,
    PositionTenure = @PositionTenure,
    IsActive = @IsActive,
    ModifiedOn = GETDATE(),
    ModifiedByUserID = @ModifiedByUserID
WHERE DesignationID = @DesignationID;", conn))
                        {
                            AddParams(cmd, designationId, titleId, gradeId, employeeCategoryId, probationDays,
                                privilegeCode, privilegeTier, positionTenure, isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Designation updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
INSERT INTO tblDesignation
    (TitleID, GradeID, EmployeeCategoryID, ProbationDays, PrivilegeCode, PrivilegeTier, PositionTenure, IsActive, CreatedOn, CreatedByUserID)
VALUES
    (@TitleID, @GradeID, @EmployeeCategoryID, @ProbationDays, @PrivilegeCode, @PrivilegeTier, @PositionTenure, @IsActive, GETDATE(), @CreatedByUserID);", conn))
                        {
                            AddParams(cmd, 0, titleId, gradeId, employeeCategoryId, probationDays,
                                privilegeCode, privilegeTier, positionTenure, isActive);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Designation added successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
                Response.Redirect(RedirectEdit(designationId));
                return;
            }

            Response.Redirect("~/DesignationSetup.aspx");
        }

        private void OnPostDelete()
        {
            if (!Auth.IsAdmin && !Perms.CanWrite("DesignationSetup") && !Perms.CanDelete("DesignationSetup"))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect("~/DesignationSetup.aspx");
                return;
            }

            var deleteId = FormInt("deleteId");
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
UPDATE tblDesignation SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
WHERE DesignationID = @DesignationID;", conn))
                {
                    cmd.Parameters.AddWithValue("@DesignationID", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Designation deactivated successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }
            Response.Redirect("~/DesignationSetup.aspx");
        }

        private static string RedirectEdit(int designationId)
            => designationId > 0 ? "~/DesignationSetup.aspx?editId=" + designationId : "~/DesignationSetup.aspx";

        private static bool IsAllowed(string[] options, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            foreach (var option in options)
            {
                if (string.Equals(option, value, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static void AddParams(
            SqlCommand cmd, int designationId, int titleId, int gradeId, int employeeCategoryId,
            int? probationDays, string privilegeCode, string privilegeTier, string positionTenure, bool isActive)
        {
            if (designationId > 0)
                cmd.Parameters.AddWithValue("@DesignationID", designationId);
            cmd.Parameters.AddWithValue("@TitleID", titleId);
            cmd.Parameters.AddWithValue("@GradeID", gradeId);
            cmd.Parameters.AddWithValue("@EmployeeCategoryID", employeeCategoryId);
            cmd.Parameters.AddWithValue("@ProbationDays", probationDays.HasValue ? (object)probationDays.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@PrivilegeCode", privilegeCode);
            cmd.Parameters.AddWithValue("@PrivilegeTier", privilegeTier);
            cmd.Parameters.AddWithValue("@PositionTenure",
                string.IsNullOrWhiteSpace(positionTenure) ? (object)DBNull.Value : positionTenure);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
        }

        private void LoadLookups()
        {
            Titles = LoadLookup("tblTitle", "TitleID", "TitleName");
            Grades = LoadLookup("tblGrade", "GradeID", "GradeName");
            EmployeeCategories = LoadLookup("tblEmployeeCategory", "EmployeeCategoryID", "EmployeeCategoryName");
        }

        private List<LookupItem> LoadLookup(string table, string idColumn, string nameColumn)
        {
            var items = new List<LookupItem>();
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(
                    "SELECT " + idColumn + ", " + nameColumn + " FROM " + table + " WHERE IsActive = 1 ORDER BY " + nameColumn + ";", conn))
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            items.Add(new LookupItem
                            {
                                Id = Convert.ToInt32(dr[idColumn]),
                                Name = dr[nameColumn] == DBNull.Value ? "" : dr[nameColumn].ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch { }
            return items;
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT DesignationID, TitleID, GradeID, EmployeeCategoryID, ProbationDays, PrivilegeCode, PrivilegeTier, PositionTenure, IsActive
FROM tblDesignation WHERE DesignationID = @ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;
                    Input = new DesignationRecord
                    {
                        DesignationID = id,
                        TitleID = IntOrZero(dr["TitleID"]),
                        GradeID = IntOrZero(dr["GradeID"]),
                        EmployeeCategoryID = IntOrZero(dr["EmployeeCategoryID"]),
                        ProbationDays = dr["ProbationDays"] == DBNull.Value ? "" : Convert.ToInt32(dr["ProbationDays"]).ToString(),
                        PrivilegeCode = dr["PrivilegeCode"] == DBNull.Value ? "NONE" : dr["PrivilegeCode"].ToString() ?? "NONE",
                        PrivilegeTier = dr["PrivilegeTier"] == DBNull.Value ? "None" : dr["PrivilegeTier"].ToString() ?? "None",
                        PositionTenure = dr["PositionTenure"] == DBNull.Value ? "" : dr["PositionTenure"].ToString() ?? "",
                        IsActive = Convert.ToBoolean(dr["IsActive"])
                    };
                }
            }
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT d.DesignationID, d.TitleID, ISNULL(t.TitleName, '') AS TitleName,
       d.GradeID, ISNULL(g.GradeName, '') AS GradeName,
       d.EmployeeCategoryID, ISNULL(c.EmployeeCategoryName, '') AS EmployeeCategoryName,
       d.ProbationDays, d.PrivilegeCode, d.PrivilegeTier, d.PositionTenure, d.IsActive
FROM tblDesignation d
LEFT JOIN tblTitle t ON t.TitleID = d.TitleID
LEFT JOIN tblGrade g ON g.GradeID = d.GradeID
LEFT JOIN tblEmployeeCategory c ON c.EmployeeCategoryID = d.EmployeeCategoryID
ORDER BY d.IsActive DESC, t.TitleName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Records.Add(new DesignationRecord
                        {
                            DesignationID = Convert.ToInt32(dr["DesignationID"]),
                            TitleID = IntOrZero(dr["TitleID"]),
                            TitleName = dr["TitleName"].ToString() ?? "",
                            GradeID = IntOrZero(dr["GradeID"]),
                            GradeName = dr["GradeName"].ToString() ?? "",
                            EmployeeCategoryID = IntOrZero(dr["EmployeeCategoryID"]),
                            EmployeeCategoryName = dr["EmployeeCategoryName"].ToString() ?? "",
                            ProbationDays = dr["ProbationDays"] == DBNull.Value ? "" : Convert.ToInt32(dr["ProbationDays"]).ToString(),
                            PrivilegeCode = dr["PrivilegeCode"] == DBNull.Value ? "" : dr["PrivilegeCode"].ToString() ?? "",
                            PrivilegeTier = dr["PrivilegeTier"] == DBNull.Value ? "" : dr["PrivilegeTier"].ToString() ?? "",
                            PositionTenure = dr["PositionTenure"] == DBNull.Value ? "" : dr["PositionTenure"].ToString() ?? "",
                            IsActive = Convert.ToBoolean(dr["IsActive"])
                        });
                    }
                }
            }
        }

        internal static void EnsureTable(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return;
            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.tblDesignation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblDesignation (
        DesignationID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TitleID INT NOT NULL,
        GradeID INT NOT NULL,
        EmployeeCategoryID INT NOT NULL,
        ProbationDays INT NULL,
        PrivilegeCode NVARCHAR(30) NOT NULL CONSTRAINT DF_tblDesignation_PrivilegeCode DEFAULT (N'NONE'),
        PrivilegeTier NVARCHAR(30) NOT NULL CONSTRAINT DF_tblDesignation_PrivilegeTier DEFAULT (N'None'),
        PositionTenure NVARCHAR(30) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_tblDesignation_IsActive DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblDesignation_CreatedOn DEFAULT (GETDATE()),
        ModifiedOn DATETIME NULL,
        CreatedByUserID INT NULL,
        ModifiedByUserID INT NULL
    );
END;", conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private static int IntOrZero(object v) => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
    }
}
