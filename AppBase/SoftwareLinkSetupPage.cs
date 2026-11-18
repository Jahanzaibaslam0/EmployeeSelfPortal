using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class SoftwareLinkRecord
    {
        public int SoftwareLinkID { get; set; }
        public string SoftwareName { get; set; } = "";
        public string SoftwareUrl { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public int SortOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }

    public class SoftwareLinkSetupPage : AppBasePage
    {
        public string PageTitle => "Software Link Setup";
        public SoftwareLinkRecord Input { get; private set; } = new SoftwareLinkRecord { IsActive = true, SortOrder = 1 };
        public List<SoftwareLinkRecord> Records { get; private set; } = new List<SoftwareLinkRecord>();
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            EnsureTable();

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
            var editId = QueryInt("editId");
            if (editId.HasValue && editId.Value > 0)
                LoadForEdit(editId.Value);
            LoadRecords();
        }

        private void OnPostSave()
        {
            if (!Auth.IsAdmin && !Perms.CanWrite("SoftwareLinkSetup"))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect("~/SoftwareLinkSetup.aspx");
                return;
            }

            var id = FormInt("softwareLinkID");
            var name = FormString("softwareName");
            var url = FormString("softwareUrl");
            var category = FormString("category");
            var description = FormString("description");
            var sortOrder = FormInt("sortOrder");
            var isActive = FormBool("isActive");
            if (sortOrder <= 0) sortOrder = 1;

            if (string.IsNullOrWhiteSpace(name))
            {
                SetAlert("Software name is required.", "error");
                Response.Redirect("~/SoftwareLinkSetup.aspx");
                return;
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                SetAlert("Software URL is required.", "error");
                Response.Redirect("~/SoftwareLinkSetup.aspx");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (id > 0)
                    {
                        using (var cmd = new SqlCommand(@"
UPDATE tblSoftwareLink SET
    SoftwareName = @SoftwareName, SoftwareUrl = @SoftwareUrl, Category = @Category,
    Description = @Description, SortOrder = @SortOrder, IsActive = @IsActive,
    ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
WHERE SoftwareLinkID = @SoftwareLinkID;", conn))
                        {
                            AddParams(cmd, id, name, url, category, description, sortOrder, isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Software link updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
INSERT INTO tblSoftwareLink
    (SoftwareName, SoftwareUrl, Category, Description, SortOrder, IsActive, CreatedOn, CreatedByUserID)
VALUES
    (@SoftwareName, @SoftwareUrl, @Category, @Description, @SortOrder, @IsActive, GETDATE(), @CreatedByUserID);", conn))
                        {
                            AddParams(cmd, 0, name, url, category, description, sortOrder, isActive);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Software link added successfully.");
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                SetAlert("This software link already exists.", "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }

            Response.Redirect("~/SoftwareLinkSetup.aspx");
        }

        private void OnPostDelete()
        {
            if (!Auth.IsAdmin && !Perms.CanWrite("SoftwareLinkSetup") && !Perms.CanDelete("SoftwareLinkSetup"))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect("~/SoftwareLinkSetup.aspx");
                return;
            }

            var deleteId = FormInt("deleteId");
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
UPDATE tblSoftwareLink SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
WHERE SoftwareLinkID = @SoftwareLinkID;", conn))
                {
                    cmd.Parameters.AddWithValue("@SoftwareLinkID", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Software link deactivated successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }
            Response.Redirect("~/SoftwareLinkSetup.aspx");
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT SoftwareLinkID, SoftwareName, SoftwareUrl, Category, Description, SortOrder, IsActive
FROM tblSoftwareLink WHERE SoftwareLinkID = @SoftwareLinkID;", conn))
            {
                cmd.Parameters.AddWithValue("@SoftwareLinkID", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                        Input = ReadRecord(dr);
                }
            }
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT SoftwareLinkID, SoftwareName, SoftwareUrl, Category, Description, SortOrder, IsActive
FROM tblSoftwareLink
ORDER BY IsActive DESC, SortOrder, SoftwareName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Records.Add(ReadRecord(dr));
                }
            }
        }

        private static void AddParams(SqlCommand cmd, int id, string name, string url,
            string category, string description, int sortOrder, bool isActive)
        {
            cmd.Parameters.AddWithValue("@SoftwareLinkID", id);
            cmd.Parameters.AddWithValue("@SoftwareName", name.Trim());
            cmd.Parameters.AddWithValue("@SoftwareUrl", url.Trim());
            cmd.Parameters.AddWithValue("@Category", string.IsNullOrWhiteSpace(category) ? (object)DBNull.Value : category.Trim());
            cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());
            cmd.Parameters.AddWithValue("@SortOrder", sortOrder);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
        }

        private static SoftwareLinkRecord ReadRecord(SqlDataReader dr)
        {
            return new SoftwareLinkRecord
            {
                SoftwareLinkID = Convert.ToInt32(dr["SoftwareLinkID"]),
                SoftwareName = dr["SoftwareName"].ToString() ?? "",
                SoftwareUrl = dr["SoftwareUrl"].ToString() ?? "",
                Category = dr["Category"] == DBNull.Value ? "" : dr["Category"].ToString() ?? "",
                Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
                SortOrder = dr["SortOrder"] == DBNull.Value ? 1 : Convert.ToInt32(dr["SortOrder"]),
                IsActive = Convert.ToBoolean(dr["IsActive"])
            };
        }

        private void EnsureTable()
        {
            if (string.IsNullOrWhiteSpace(Conn)) return;
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.tblSoftwareLink', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblSoftwareLink (
        SoftwareLinkID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SoftwareName NVARCHAR(100) NOT NULL,
        SoftwareUrl NVARCHAR(500) NOT NULL,
        Category NVARCHAR(50) NULL,
        Description NVARCHAR(250) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_tblSoftwareLink_Sort DEFAULT (1),
        IsActive BIT NOT NULL CONSTRAINT DF_tblSoftwareLink_Active DEFAULT (1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_tblSoftwareLink_Created DEFAULT (GETDATE()),
        ModifiedOn DATETIME NULL,
        CreatedByUserID INT NULL,
        ModifiedByUserID INT NULL
    );
END
IF COL_LENGTH(N'dbo.tblSoftwareLink', N'CreatedByUserID') IS NULL
    ALTER TABLE dbo.tblSoftwareLink ADD CreatedByUserID INT NULL;
IF COL_LENGTH(N'dbo.tblSoftwareLink', N'ModifiedByUserID') IS NULL
    ALTER TABLE dbo.tblSoftwareLink ADD ModifiedByUserID INT NULL;", conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }
    }
}
