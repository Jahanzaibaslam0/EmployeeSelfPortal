using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public partial class CitySetupPage : AppBasePage
    {
        public string PageTitle => "City Setup";
        public CityRecord Input { get; set; } = new CityRecord { IsActive = true };
        public List<CityRecord> Records { get; set; } = new List<CityRecord>();
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? Request.QueryString["handler"] ?? "Save";
                if (string.Equals(handler, "Save", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostSave(
                        int.TryParse(Request.Form["cityID"], out var id) ? id : 0,
                        FormString("cityCode"),
                        FormString("cityName"),
                        FormString("aliasName"),
                        FormBool("isActive"));
                    return;
                }
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostDelete(int.TryParse(Request.Form["deleteId"], out var del) ? del : 0);
                    return;
                }
            }

            if (!IsPostBack)
                OnGet(QueryInt("editId"));
        }

        private void OnGet(int? editId)
        {
            LoadAlert();
            if (editId.HasValue && editId > 0)
                LoadForEdit(editId.Value);
            else
                Input.CityCode = GenerateNextCode();
            LoadRecords();
        }

        private void OnPostSave(int cityID, string cityCode, string cityName, string aliasName, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(cityCode))
            {
                SetAlert("City Code is required.", "error");
                Response.Redirect("~/CitySetup.aspx" + (cityID > 0 ? "?editId=" + cityID : ""));
                return;
            }
            if (string.IsNullOrWhiteSpace(cityName))
            {
                SetAlert("City Name is required.", "error");
                Response.Redirect("~/CitySetup.aspx" + (cityID > 0 ? "?editId=" + cityID : ""));
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (cityID > 0)
                    {
                        using (var cmd = new SqlCommand(@"
                    UPDATE tblCity SET CityCode = @Code, CityName = @Name, AliasName = @AliasName,
                        IsActive = @IsActive, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                    WHERE CityID = @ID;", conn))
                        {
                            AddParams(cmd, cityID, cityCode, cityName, aliasName, isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("City updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
                    INSERT INTO tblCity (CityCode, CityName, AliasName, IsActive, CreatedOn, CreatedByUserID)
                    VALUES (@Code, @Name, @AliasName, @IsActive, GETDATE(), @CreatedByUserID);", conn))
                        {
                            AddParams(cmd, 0, cityCode, cityName, aliasName, isActive);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("City added successfully.");
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                SetAlert("A city with this code already exists.", "error");
                Response.Redirect("~/CitySetup.aspx" + (cityID > 0 ? "?editId=" + cityID : ""));
                return;
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
                Response.Redirect("~/CitySetup.aspx" + (cityID > 0 ? "?editId=" + cityID : ""));
                return;
            }
            Response.Redirect("~/CitySetup.aspx");
        }

        private void OnPostDelete(int deleteId)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"UPDATE tblCity SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID WHERE CityID = @ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("City removed successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error removing record: " + ex.Message, "error");
            }
            Response.Redirect("~/CitySetup.aspx");
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"SELECT CityID, CityCode, CityName, AliasName, IsActive FROM tblCity WHERE CityID = @ID;", conn))
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
            using (var cmd = new SqlCommand(@"SELECT CityID, CityCode, CityName, AliasName, IsActive FROM tblCity ORDER BY IsActive DESC, CityCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read()) Records.Add(ReadRecord(dr));
                }
            }
        }

        private static CityRecord ReadRecord(SqlDataReader dr)
        {
            return new CityRecord
            {
                CityID = Convert.ToInt32(dr["CityID"]),
                CityCode = dr["CityCode"].ToString() ?? "",
                CityName = dr["CityName"].ToString() ?? "",
                AliasName = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
                IsActive = Convert.ToBoolean(dr["IsActive"])
            };
        }

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
            using (var cmd = new SqlCommand(@"SELECT TOP 1 CityCode FROM tblCity WHERE CityCode LIKE 'CTY-%' ORDER BY CityCode DESC;", conn))
            {
                conn.Open();
                var last = cmd.ExecuteScalar() as string;
                if (!string.IsNullOrEmpty(last) && last.Length >= 7 && int.TryParse(last.Substring(4), out int num))
                    return "CTY-" + (num + 1).ToString("D3");
                return "CTY-001";
            }
        }

        private void LoadAlert()
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
        }
    }
}
