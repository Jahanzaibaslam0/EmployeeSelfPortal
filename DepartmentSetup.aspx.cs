using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class DepartmentRecord
    {
        public int Id { get; set; }
        public int DivisionID { get; set; }
        public string DivisionName { get; set; } = "";
        public int DepartmentManagerID { get; set; }
        public string DepartmentManagerName { get; set; } = "";
        public string Name { get; set; } = "";
        public string AliasName { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public partial class DepartmentSetupPage : AppBasePage
    {
        public string PageTitle => "Department Setup";
        public DepartmentRecord Input { get; set; } = new DepartmentRecord { IsActive = true };
        public List<DepartmentRecord> Records { get; set; } = new List<DepartmentRecord>();
        public List<LookupItem> Divisions { get; set; } = new List<LookupItem>();
        public List<LookupItem> Employees { get; set; } = new List<LookupItem>();
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
                Save();
                return;
            }
            LoadPage(QueryInt("editId"));
        }

        private void LoadPage(int? editId)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg; AlertType = typ;
            LoadDivisions();
            LoadEmployees();
            if (editId.HasValue && editId > 0) LoadForEdit(editId.Value);
            LoadRecords();
        }

        private void Save()
        {
            if (!Perms.CanWrite("DepartmentSetup"))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect("~/DepartmentSetup.aspx");
                return;
            }

            var itemId = int.TryParse(Request.Form["itemId"], out var id) ? id : 0;
            var divisionId = int.TryParse(Request.Form["divisionID"], out var did) ? did : 0;
            var managerId = int.TryParse(Request.Form["departmentManagerID"], out var mid) ? mid : 0;
            var name = FormString("itemName");
            var alias = FormString("aliasName");
            var isActive = FormBool("isActive");
            if (string.IsNullOrWhiteSpace(name))
            {
                SetAlert("Department name is required.", "error");
                Response.Redirect("~/DepartmentSetup.aspx");
                return;
            }
            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var ensure = new SqlCommand(@"
IF COL_LENGTH(N'tblDepartment', N'DepartmentManagerID') IS NULL
    ALTER TABLE tblDepartment ADD DepartmentManagerID INT NULL;", conn))
                        ensure.ExecuteNonQuery();
                    if (itemId > 0)
                    {
                        using (var cmd = new SqlCommand(@"
UPDATE tblDepartment SET DivisionID=@DivisionID, DepartmentManagerID=@ManagerID, DepartmentName=@Name, AliasName=@Alias,
  IsActive=@IsActive, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE DepartmentID=@ID;", conn))
                        {
                            cmd.Parameters.AddWithValue("@ID", itemId);
                            cmd.Parameters.AddWithValue("@DivisionID", divisionId <= 0 ? (object)DBNull.Value : divisionId);
                            cmd.Parameters.AddWithValue("@ManagerID", managerId <= 0 ? (object)DBNull.Value : managerId);
                            cmd.Parameters.AddWithValue("@Name", name);
                            cmd.Parameters.AddWithValue("@Alias", string.IsNullOrWhiteSpace(alias) ? (object)DBNull.Value : alias);
                            cmd.Parameters.AddWithValue("@IsActive", isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Department updated.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
INSERT INTO tblDepartment (DivisionID, DepartmentManagerID, DepartmentName, AliasName, IsActive, CreatedOn, CreatedByUserID)
VALUES (@DivisionID,@ManagerID,@Name,@Alias,@IsActive,GETDATE(),@CreatedByUserID);", conn))
                        {
                            cmd.Parameters.AddWithValue("@DivisionID", divisionId <= 0 ? (object)DBNull.Value : divisionId);
                            cmd.Parameters.AddWithValue("@ManagerID", managerId <= 0 ? (object)DBNull.Value : managerId);
                            cmd.Parameters.AddWithValue("@Name", name);
                            cmd.Parameters.AddWithValue("@Alias", string.IsNullOrWhiteSpace(alias) ? (object)DBNull.Value : alias);
                            cmd.Parameters.AddWithValue("@IsActive", isActive);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Department added.");
                    }
                }
            }
            catch (Exception ex) { SetAlert("Error: " + ex.Message, "error"); }
            Response.Redirect("~/DepartmentSetup.aspx");
        }

        private void SoftDelete(int id)
        {
            if (!Perms.CanDelete("DepartmentSetup") && !Perms.CanWrite("DepartmentSetup"))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect("~/DepartmentSetup.aspx");
                return;
            }
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand("UPDATE tblDepartment SET IsActive=0, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE DepartmentID=@ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Department removed.");
            }
            catch (Exception ex) { SetAlert("Error: " + ex.Message, "error"); }
            Response.Redirect("~/DepartmentSetup.aspx");
        }

        private void LoadDivisions()
        {
            Divisions.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT DivisionID, DivisionName FROM tblDivision WHERE IsActive=1 ORDER BY DivisionName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        Divisions.Add(new LookupItem { Id = Convert.ToInt32(dr["DivisionID"]), Name = dr["DivisionName"].ToString() ?? "" });
            }
        }

        private void LoadEmployees()
        {
            Employees.Clear();
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
IF COL_LENGTH(N'tblDepartment', N'DepartmentManagerID') IS NULL
    ALTER TABLE tblDepartment ADD DepartmentManagerID INT NULL;", conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }

            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT EmployeeID, EmployeeCode + N' – ' + LTRIM(RTRIM(ISNULL(FirstName,'') + N' ' + ISNULL(LastName,'')))
FROM tblEmployee WHERE Status = N'Active' ORDER BY EmployeeCode, FirstName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        Employees.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
            }
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT d.DepartmentID, ISNULL(d.DivisionID,0) DivisionID, ISNULL(v.DivisionName,'') DivisionName,
       ISNULL(d.DepartmentManagerID,0) DepartmentManagerID,
       LTRIM(RTRIM(ISNULL(e.FirstName,'') + N' ' + ISNULL(e.LastName,''))) ManagerName,
       d.DepartmentName, ISNULL(d.AliasName,'') AliasName, d.IsActive
FROM tblDepartment d
LEFT JOIN tblDivision v ON v.DivisionID=d.DivisionID
LEFT JOIN tblEmployee e ON e.EmployeeID = d.DepartmentManagerID
WHERE d.DepartmentID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    if (dr.Read())
                        Input = new DepartmentRecord
                        {
                            Id = Convert.ToInt32(dr["DepartmentID"]),
                            DivisionID = Convert.ToInt32(dr["DivisionID"]),
                            DivisionName = dr["DivisionName"].ToString() ?? "",
                            DepartmentManagerID = Convert.ToInt32(dr["DepartmentManagerID"]),
                            DepartmentManagerName = dr["ManagerName"] == DBNull.Value ? "" : dr["ManagerName"].ToString() ?? "",
                            Name = dr["DepartmentName"].ToString() ?? "",
                            AliasName = dr["AliasName"].ToString() ?? "",
                            IsActive = Convert.ToBoolean(dr["IsActive"])
                        };
            }
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT d.DepartmentID, ISNULL(d.DivisionID,0) DivisionID, ISNULL(v.DivisionName,'') DivisionName,
       ISNULL(d.DepartmentManagerID,0) DepartmentManagerID,
       LTRIM(RTRIM(ISNULL(e.FirstName,'') + N' ' + ISNULL(e.LastName,''))) ManagerName,
       d.DepartmentName, ISNULL(d.AliasName,'') AliasName, d.IsActive
FROM tblDepartment d
LEFT JOIN tblDivision v ON v.DivisionID=d.DivisionID
LEFT JOIN tblEmployee e ON e.EmployeeID = d.DepartmentManagerID
ORDER BY d.IsActive DESC, d.DepartmentName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        Records.Add(new DepartmentRecord
                        {
                            Id = Convert.ToInt32(dr["DepartmentID"]),
                            DivisionID = Convert.ToInt32(dr["DivisionID"]),
                            DivisionName = dr["DivisionName"].ToString() ?? "",
                            DepartmentManagerID = Convert.ToInt32(dr["DepartmentManagerID"]),
                            DepartmentManagerName = dr["ManagerName"] == DBNull.Value ? "" : dr["ManagerName"].ToString() ?? "",
                            Name = dr["DepartmentName"].ToString() ?? "",
                            AliasName = dr["AliasName"].ToString() ?? "",
                            IsActive = Convert.ToBoolean(dr["IsActive"])
                        });
            }
        }
    }
}
