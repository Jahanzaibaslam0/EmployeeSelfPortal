using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class LookupRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string AliasName { get; set; } = "";
        public bool IsActive { get; set; }
    }

    public abstract class LookupSetupBasePage : AppBasePage
    {
        protected abstract string TableName { get; }
        protected abstract string IdColumn { get; }
        protected abstract string NameColumn { get; }
        protected virtual string AliasColumn => null;

        public abstract string PageTitle { get; }
        public abstract string ItemLabel { get; }
        public abstract string PagePath { get; }
        public virtual string AliasLabel => "Alias";
        public virtual int AliasMaxLength => 50;
        public virtual bool ShowAlias => AliasColumn != null;

        public List<LookupRecord> Records { get; set; } = new List<LookupRecord>();
        public LookupRecord Input { get; set; } = new LookupRecord { IsActive = true };
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? Request.QueryString["handler"];
                if (string.Equals(handler, "Save", StringComparison.OrdinalIgnoreCase))
                    SaveRecord();
                else if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                    DeleteRecord();
            }

            LoadAlertState();
            if (!IsPostBack)
            {
                var editId = QueryInt("editId");
                if (editId.HasValue && editId.Value > 0)
                    LoadForEdit(editId.Value);
            }
            LoadRecords();
        }

        private void LoadAlertState()
        {
            if (Session["Alert"] == null) return;
            AlertMessage = Session["Alert"] as string ?? "";
            AlertType = Session["AlertType"] as string ?? "success";
            Session.Remove("Alert");
            Session.Remove("AlertType");
        }

        private void SaveRecord()
        {
            var formKey = ResolveFormKey();
            if (!string.IsNullOrEmpty(formKey) && !Perms.CanWrite(formKey))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect(PagePath + ".aspx");
                return;
            }

            var itemId = 0;
            int.TryParse(FormString("itemId"), out itemId);
            var itemName = FormString("itemName");
            var aliasName = FormString("aliasName");
            var isActive = FormBool("isActive");

            if (string.IsNullOrWhiteSpace(itemName))
            {
                SetAlert($"{ItemLabel} is required.", "error");
                Response.Redirect(PagePath + ".aspx");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (itemId > 0)
                    {
                        using (var cmd = new SqlCommand($@"
                            UPDATE {TableName}
                            SET {NameColumn} = @Name,
                                {AliasUpdateSql}
                                IsActive = @IsActive,
                                ModifiedOn = GETDATE(),
                                ModifiedByUserID = @ModifiedByUserID
                            WHERE {IdColumn} = @Id;", conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", itemId);
                            cmd.Parameters.AddWithValue("@Name", itemName);
                            AddAliasParameter(cmd, aliasName);
                            cmd.Parameters.AddWithValue("@IsActive", isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert($"{ItemLabel} updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand($@"
                            INSERT INTO {TableName} ({NameColumn}{AliasInsertColumns}, IsActive, CreatedOn, CreatedByUserID)
                            VALUES (@Name{AliasInsertValues}, @IsActive, GETDATE(), @CreatedByUserID);", conn))
                        {
                            cmd.Parameters.AddWithValue("@Name", itemName);
                            AddAliasParameter(cmd, aliasName);
                            cmd.Parameters.AddWithValue("@IsActive", isActive);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert($"{ItemLabel} added successfully.");
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                SetAlert($"{ItemLabel} already exists.", "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }

            Response.Redirect(PagePath + ".aspx");
        }

        private void DeleteRecord()
        {
            var formKey = ResolveFormKey();
            if (!string.IsNullOrEmpty(formKey) && !Perms.CanDelete(formKey) && !Perms.CanWrite(formKey))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect(PagePath + ".aspx");
                return;
            }

            var deleteId = 0;
            int.TryParse(FormString("deleteId"), out deleteId);
            if (deleteId <= 0) return;

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand($@"
                        UPDATE {TableName}
                        SET IsActive = 0,
                            ModifiedOn = GETDATE(),
                            ModifiedByUserID = @ModifiedByUserID
                        WHERE {IdColumn} = @Id;", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", deleteId);
                        AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                        cmd.ExecuteNonQuery();
                    }
                }
                SetAlert($"{ItemLabel} removed successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error removing record: " + ex.Message, "error");
            }

            Response.Redirect(PagePath + ".aspx");
        }

        private string ResolveFormKey()
        {
            var form = AppForms.FindByPath(PagePath);
            return form?.Key ?? "";
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand($@"
                SELECT {IdColumn}, {NameColumn}{AliasSelectSql}, IsActive
                FROM {TableName}
                WHERE {IdColumn} = @Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;
                    Input = new LookupRecord
                    {
                        Id = Convert.ToInt32(dr[IdColumn]),
                        Name = dr[NameColumn].ToString() ?? "",
                        AliasName = ReadAlias(dr),
                        IsActive = Convert.ToBoolean(dr["IsActive"])
                    };
                }
            }
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand($@"
                SELECT {IdColumn}, {NameColumn}{AliasSelectSql}, IsActive
                FROM {TableName}
                ORDER BY IsActive DESC, {NameColumn};", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Records.Add(new LookupRecord
                        {
                            Id = Convert.ToInt32(dr[IdColumn]),
                            Name = dr[NameColumn].ToString() ?? "",
                            AliasName = ReadAlias(dr),
                            IsActive = Convert.ToBoolean(dr["IsActive"])
                        });
                    }
                }
            }
        }

        private string AliasSelectSql => AliasColumn == null ? "" : $", {AliasColumn}";
        private string AliasUpdateSql => AliasColumn == null ? "" : $"{AliasColumn} = @AliasName,";
        private string AliasInsertColumns => AliasColumn == null ? "" : $", {AliasColumn}";
        private string AliasInsertValues => AliasColumn == null ? "" : ", @AliasName";

        private void AddAliasParameter(SqlCommand cmd, string aliasName)
        {
            if (AliasColumn == null) return;
            cmd.Parameters.AddWithValue("@AliasName",
                string.IsNullOrWhiteSpace(aliasName) ? (object)DBNull.Value : aliasName);
        }

        private string ReadAlias(SqlDataReader dr)
        {
            return AliasColumn == null || dr[AliasColumn] == DBNull.Value
                ? ""
                : dr[AliasColumn]?.ToString() ?? "";
        }
    }
}
