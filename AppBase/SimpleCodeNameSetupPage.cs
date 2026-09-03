using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    /// <summary>Shared CRUD for Code/Name/Alias/IsActive master tables.</summary>
    public abstract class SimpleCodeNameSetupPage : AppBasePage
    {
        public class Row
        {
            public int Id { get; set; }
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string AliasName { get; set; } = "";
            public string Extra { get; set; } = "";
            public bool IsActive { get; set; } = true;
        }

        protected abstract string TableName { get; }
        protected abstract string IdColumn { get; }
        protected abstract string CodeColumn { get; }
        protected abstract string NameColumn { get; }
        protected virtual string AliasColumn => "AliasName";
        protected virtual string ExtraColumn => null; // optional Description etc.
        protected virtual string CodePrefix => null;
        public abstract string PageTitle { get; }
        public abstract string ItemLabel { get; }
        public abstract string PagePath { get; }

        public Row Input { get; set; } = new Row { IsActive = true };
        public List<Row> Records { get; set; } = new List<Row>();
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
                Save(
                    int.TryParse(Request.Form["itemId"], out var id) ? id : 0,
                    FormString("itemCode"), FormString("itemName"), FormString("aliasName"),
                    FormString("extra"), FormBool("isActive"));
                return;
            }
            LoadPage(QueryInt("editId"));
        }

        private void LoadPage(int? editId)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg; AlertType = typ;
            if (editId.HasValue && editId.Value > 0) LoadForEdit(editId.Value);
            else if (!string.IsNullOrEmpty(CodePrefix)) Input.Code = GenerateNextCode();
            LoadRecords();
        }

        private void Save(int id, string code, string name, string alias, string extra, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                SetAlert(ItemLabel + " code and name are required.", "error");
                Response.Redirect("~/" + PagePath + ".aspx" + (id > 0 ? "?editId=" + id : ""));
                return;
            }
            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    var extraSet = ExtraColumn != null ? ", " + ExtraColumn + " = @Extra" : "";
                    var extraIns = ExtraColumn != null ? ", " + ExtraColumn : "";
                    var extraVal = ExtraColumn != null ? ", @Extra" : "";
                    if (id > 0)
                    {
                        using (var cmd = new SqlCommand(
                            "UPDATE " + TableName + " SET " + CodeColumn + "=@Code, " + NameColumn + "=@Name, " + AliasColumn + "=@Alias, IsActive=@IsActive" +
                            extraSet + ", ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE " + IdColumn + "=@ID;", conn))
                        {
                            AddParams(cmd, id, code, name, alias, extra, isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert(ItemLabel + " updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(
                            "INSERT INTO " + TableName + " (" + CodeColumn + ", " + NameColumn + ", " + AliasColumn + extraIns +
                            ", IsActive, CreatedOn, CreatedByUserID) VALUES (@Code,@Name,@Alias" + extraVal +
                            ",@IsActive,GETDATE(),@CreatedByUserID);", conn))
                        {
                            AddParams(cmd, 0, code, name, alias, extra, isActive);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert(ItemLabel + " added successfully.");
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                SetAlert("A record with this code already exists.", "error");
                Response.Redirect("~/" + PagePath + ".aspx" + (id > 0 ? "?editId=" + id : ""));
                return;
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
                Response.Redirect("~/" + PagePath + ".aspx" + (id > 0 ? "?editId=" + id : ""));
                return;
            }
            Response.Redirect("~/" + PagePath + ".aspx");
        }

        private void SoftDelete(int id)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(
                    "UPDATE " + TableName + " SET IsActive=0, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID WHERE " + IdColumn + "=@ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert(ItemLabel + " removed successfully.");
            }
            catch (Exception ex) { SetAlert("Error: " + ex.Message, "error"); }
            Response.Redirect("~/" + PagePath + ".aspx");
        }

        private void LoadForEdit(int id)
        {
            var extraSel = ExtraColumn != null ? ", " + ExtraColumn : ", '' AS ExtraCol";
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(
                "SELECT " + IdColumn + ", " + CodeColumn + ", " + NameColumn + ", " + AliasColumn + extraSel +
                ", IsActive FROM " + TableName + " WHERE " + IdColumn + "=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    if (dr.Read()) Input = Read(dr);
            }
        }

        private void LoadRecords()
        {
            Records.Clear();
            var extraSel = ExtraColumn != null ? ", " + ExtraColumn : ", '' AS ExtraCol";
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(
                "SELECT " + IdColumn + ", " + CodeColumn + ", " + NameColumn + ", " + AliasColumn + extraSel +
                ", IsActive FROM " + TableName + " ORDER BY IsActive DESC, " + CodeColumn + ";", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read()) Records.Add(Read(dr));
            }
        }

        private Row Read(SqlDataReader dr)
        {
            object extraObj = ExtraColumn != null ? dr[ExtraColumn] : dr["ExtraCol"];
            return new Row
            {
                Id = Convert.ToInt32(dr[IdColumn]),
                Code = dr[CodeColumn].ToString() ?? "",
                Name = dr[NameColumn].ToString() ?? "",
                AliasName = dr[AliasColumn] == DBNull.Value ? "" : dr[AliasColumn].ToString() ?? "",
                Extra = extraObj == DBNull.Value || extraObj == null ? "" : extraObj.ToString() ?? "",
                IsActive = Convert.ToBoolean(dr["IsActive"])
            };
        }

        private void AddParams(SqlCommand cmd, int id, string code, string name, string alias, string extra, bool isActive)
        {
            if (id > 0) cmd.Parameters.AddWithValue("@ID", id);
            cmd.Parameters.AddWithValue("@Code", code.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@Name", name.Trim());
            cmd.Parameters.AddWithValue("@Alias", string.IsNullOrWhiteSpace(alias) ? (object)DBNull.Value : alias.Trim());
            if (ExtraColumn != null)
                cmd.Parameters.AddWithValue("@Extra", string.IsNullOrWhiteSpace(extra) ? (object)DBNull.Value : extra.Trim());
            cmd.Parameters.AddWithValue("@IsActive", isActive);
        }

        private string GenerateNextCode()
        {
            if (string.IsNullOrEmpty(CodePrefix)) return "";
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(
                "SELECT TOP 1 " + CodeColumn + " FROM " + TableName + " WHERE " + CodeColumn + " LIKE @P ORDER BY " + CodeColumn + " DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@P", CodePrefix + "%");
                conn.Open();
                var last = cmd.ExecuteScalar() as string;
                var prefixLen = CodePrefix.Length;
                if (!string.IsNullOrEmpty(last) && last.Length > prefixLen && int.TryParse(last.Substring(prefixLen), out int num))
                    return CodePrefix + (num + 1).ToString("D3");
                return CodePrefix + "001";
            }
        }
    }
}
