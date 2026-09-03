using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class ProvinceSetupPage : AppBasePage
    {
        public string PageTitle => "Province Setup";
        public ProvinceRecord Input { get; set; } = new ProvinceRecord { IsActive = true };
        public List<ProvinceRecord> Records { get; set; } = new List<ProvinceRecord>();
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
                    OnPostDelete(int.TryParse(Request.Form["deleteId"], out var del) ? del : 0);
                    return;
                }
                OnPostSave(
                    int.TryParse(Request.Form["provinceID"], out var id) ? id : 0,
                    FormString("provinceCode"), FormString("provinceName"), FormString("aliasName"), FormBool("isActive"));
                return;
            }
            OnGet(QueryInt("editId"));
        }

        private void OnGet(int? editId)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg; AlertType = typ;
            if (editId.HasValue && editId > 0) LoadForEdit(editId.Value);
            else Input.ProvinceCode = GenerateNextCode();
            LoadRecords();
        }

        private void OnPostSave(int provinceID, string provinceCode, string provinceName, string aliasName, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(provinceCode) || string.IsNullOrWhiteSpace(provinceName))
            {
                SetAlert("Province Code and Name are required.", "error");
                Response.Redirect("~/ProvinceSetup.aspx" + (provinceID > 0 ? "?editId=" + provinceID : ""));
                return;
            }
            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (provinceID > 0)
                    {
                        using (var cmd = new SqlCommand(@"
                    UPDATE tblProvince SET ProvinceCode = @Code, ProvinceName = @Name, AliasName = @AliasName,
                        IsActive = @IsActive, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                    WHERE ProvinceID = @ID;", conn))
                        {
                            AddParams(cmd, provinceID, provinceCode, provinceName, aliasName, isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Province updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
                    INSERT INTO tblProvince (ProvinceCode, ProvinceName, AliasName, IsActive, CreatedOn, CreatedByUserID)
                    VALUES (@Code, @Name, @AliasName, @IsActive, GETDATE(), @CreatedByUserID);", conn))
                        {
                            AddParams(cmd, 0, provinceCode, provinceName, aliasName, isActive);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Province added successfully.");
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                SetAlert("A province with this code already exists.", "error");
                Response.Redirect("~/ProvinceSetup.aspx" + (provinceID > 0 ? "?editId=" + provinceID : ""));
                return;
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
                Response.Redirect("~/ProvinceSetup.aspx" + (provinceID > 0 ? "?editId=" + provinceID : ""));
                return;
            }
            Response.Redirect("~/ProvinceSetup.aspx");
        }

        private void OnPostDelete(int deleteId)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"UPDATE tblProvince SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID WHERE ProvinceID = @ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Province removed successfully.");
            }
            catch (Exception ex) { SetAlert("Error removing record: " + ex.Message, "error"); }
            Response.Redirect("~/ProvinceSetup.aspx");
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"SELECT ProvinceID, ProvinceCode, ProvinceName, AliasName, IsActive FROM tblProvince WHERE ProvinceID = @ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    if (dr.Read()) Input = ReadRecord(dr);
            }
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"SELECT ProvinceID, ProvinceCode, ProvinceName, AliasName, IsActive FROM tblProvince ORDER BY IsActive DESC, ProvinceCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read()) Records.Add(ReadRecord(dr));
            }
        }

        private static ProvinceRecord ReadRecord(SqlDataReader dr) => new ProvinceRecord
        {
            ProvinceID = Convert.ToInt32(dr["ProvinceID"]),
            ProvinceCode = dr["ProvinceCode"].ToString() ?? "",
            ProvinceName = dr["ProvinceName"].ToString() ?? "",
            AliasName = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
            IsActive = Convert.ToBoolean(dr["IsActive"])
        };

        private static void AddParams(SqlCommand cmd, int id, string code, string name, string alias, bool isActive)
        {
            if (id > 0) cmd.Parameters.AddWithValue("@ID", id);
            cmd.Parameters.AddWithValue("@Code", code.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@Name", name.Trim());
            cmd.Parameters.AddWithValue("@AliasName", string.IsNullOrWhiteSpace(alias) ? (object)DBNull.Value : alias.Trim());
            cmd.Parameters.AddWithValue("@IsActive", isActive);
        }

        private string GenerateNextCode()
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"SELECT TOP 1 ProvinceCode FROM tblProvince WHERE ProvinceCode LIKE 'PRV-%' ORDER BY ProvinceCode DESC;", conn))
            {
                conn.Open();
                var last = cmd.ExecuteScalar() as string;
                if (!string.IsNullOrEmpty(last) && last.Length >= 7 && int.TryParse(last.Substring(4), out int num))
                    return "PRV-" + (num + 1).ToString("D3");
                return "PRV-001";
            }
        }
    }
}
