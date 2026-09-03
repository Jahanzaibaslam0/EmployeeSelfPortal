using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class ExtensionRecord
    {
        public int ExtensionID { get; set; }
        public string ExtensionCode { get; set; } = "";
        public string ExtensionName { get; set; } = "";
        public string AliasName { get; set; } = "";
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = "";
        public int LocationID { get; set; }
        public string LocationName { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class ExtensionSetupPage : AppBasePage
    {
        public string PageTitle => "Extension Master Setup";
        public ExtensionRecord Input { get; private set; } = new ExtensionRecord { IsActive = true };
        public List<ExtensionRecord> Records { get; private set; } = new List<ExtensionRecord>();
        public List<LookupItem> Departments { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Locations { get; private set; } = new List<LookupItem>();
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

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

            LoadDepartments();
            LoadLocations();

            var editId = QueryInt("editId");
            if (editId.HasValue && editId.Value > 0)
                LoadForEdit(editId.Value);
            else
                Input.ExtensionCode = GenerateNextCode();

            LoadRecords();
        }

        private void OnPostSave()
        {
            var extensionID = 0;
            int.TryParse(FormString("extensionID"), out extensionID);
            var extensionCode = FormString("extensionCode");
            var extensionName = FormString("extensionName");
            var aliasName = FormString("aliasName");
            var departmentID = 0;
            int.TryParse(FormString("departmentID"), out departmentID);
            var locationID = 0;
            int.TryParse(FormString("locationID"), out locationID);
            var isActive = FormBool("isActive");

            if (string.IsNullOrWhiteSpace(extensionCode))
            {
                SetAlert("Extension Code is required.", "error");
                Response.Redirect(RedirectEdit(extensionID));
                return;
            }
            if (string.IsNullOrWhiteSpace(extensionName))
            {
                SetAlert("Extension Name is required.", "error");
                Response.Redirect(RedirectEdit(extensionID));
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (extensionID > 0)
                    {
                        using (var cmd = new SqlCommand(@"
                    UPDATE tblExtension SET
                        ExtensionCode = @Code,
                        ExtensionName = @Name,
                        AliasName       = @AliasName,
                        DepartmentID    = @DepartmentID,
                        LocationID      = @LocationID,
                        IsActive        = @IsActive,
                        ModifiedOn      = GETDATE(),
                        ModifiedByUserID = @ModifiedByUserID
                    WHERE ExtensionID = @ID;", conn))
                        {
                            AddParams(cmd, extensionID, extensionCode, extensionName, aliasName, departmentID, locationID, isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Extension updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
                    INSERT INTO tblExtension
                        (ExtensionCode, ExtensionName, AliasName, DepartmentID, LocationID, IsActive, CreatedOn, CreatedByUserID)
                    VALUES
                        (@Code, @Name, @AliasName, @DepartmentID, @LocationID, @IsActive, GETDATE(), @CreatedByUserID);", conn))
                        {
                            AddParams(cmd, 0, extensionCode, extensionName, aliasName, departmentID, locationID, isActive);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Extension added successfully.");
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                {
                    SetAlert("An extension with this code already exists.", "error");
                    Response.Redirect(RedirectEdit(extensionID));
                    return;
                }
                SetAlert("Error: " + ex.Message, "error");
                Response.Redirect(RedirectEdit(extensionID));
                return;
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
                Response.Redirect(RedirectEdit(extensionID));
                return;
            }

            Response.Redirect("~/ExtensionSetup.aspx");
        }

        private void OnPostDelete()
        {
            var deleteId = 0;
            int.TryParse(FormString("deleteId"), out deleteId);
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
                UPDATE tblExtension SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                WHERE ExtensionID = @ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Extension removed successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error removing record: " + ex.Message, "error");
            }
            Response.Redirect("~/ExtensionSetup.aspx");
        }

        private string RedirectEdit(int extensionID)
            => extensionID > 0
                ? "~/ExtensionSetup.aspx?editId=" + extensionID
                : "~/ExtensionSetup.aspx";

        private void LoadDepartments()
        {
            Departments.Clear();
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
                            Id = Convert.ToInt32(dr["DepartmentID"]),
                            Name = dr["DepartmentName"].ToString() ?? ""
                        });
                    }
                }
            }
        }

        private void LoadLocations()
        {
            Locations.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT LocationID, LocationName
            FROM tblLocation
            WHERE IsActive = 1
            ORDER BY LocationName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Locations.Add(new LookupItem
                        {
                            Id = Convert.ToInt32(dr["LocationID"]),
                            Name = dr["LocationName"].ToString() ?? ""
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT ExtensionID, ExtensionCode, ExtensionName, AliasName,
                   DepartmentID, LocationID, IsActive
            FROM tblExtension WHERE ExtensionID = @ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read()) Input = ReadRecord(dr);
                }
            }
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT e.ExtensionID, e.ExtensionCode, e.ExtensionName, e.AliasName,
                   e.DepartmentID, d.DepartmentName,
                   e.LocationID, l.LocationName,
                   e.IsActive
            FROM tblExtension e
            LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
            LEFT JOIN tblLocation l ON l.LocationID = e.LocationID
            ORDER BY e.IsActive DESC, e.ExtensionCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var rec = ReadRecord(dr);
                        rec.DepartmentName = dr["DepartmentName"] == DBNull.Value ? "" : dr["DepartmentName"].ToString() ?? "";
                        rec.LocationName = dr["LocationName"] == DBNull.Value ? "" : dr["LocationName"].ToString() ?? "";
                        Records.Add(rec);
                    }
                }
            }
        }

        private static ExtensionRecord ReadRecord(SqlDataReader dr)
        {
            return new ExtensionRecord
            {
                ExtensionID = Convert.ToInt32(dr["ExtensionID"]),
                ExtensionCode = dr["ExtensionCode"].ToString() ?? "",
                ExtensionName = dr["ExtensionName"].ToString() ?? "",
                AliasName = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
                DepartmentID = dr["DepartmentID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["DepartmentID"]),
                LocationID = dr["LocationID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["LocationID"]),
                IsActive = Convert.ToBoolean(dr["IsActive"])
            };
        }

        private static void AddParams(
            SqlCommand cmd, int id, string code, string name, string alias,
            int departmentID, int locationID, bool isActive)
        {
            if (id > 0) cmd.Parameters.AddWithValue("@ID", id);
            cmd.Parameters.AddWithValue("@Code", code.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@Name", name.Trim());
            cmd.Parameters.AddWithValue("@AliasName", string.IsNullOrWhiteSpace(alias) ? (object)DBNull.Value : alias.Trim());
            cmd.Parameters.AddWithValue("@DepartmentID", departmentID <= 0 ? (object)DBNull.Value : departmentID);
            cmd.Parameters.AddWithValue("@LocationID", locationID <= 0 ? (object)DBNull.Value : locationID);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
        }

        private string GenerateNextCode()
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT TOP 1 ExtensionCode FROM tblExtension
            WHERE ExtensionCode LIKE 'EXT-%'
            ORDER BY ExtensionCode DESC;", conn))
            {
                conn.Open();
                var last = cmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrEmpty(last) && last.Length >= 7)
                {
                    int num;
                    if (int.TryParse(last.Substring(4), out num))
                        return "EXT-" + (num + 1).ToString("D3");
                }
                return "EXT-001";
            }
        }
    }
}
