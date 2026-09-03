using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class BankCurrencyItem
    {
        public string CurrencyCode { get; set; } = "";
        public string CurrencyName { get; set; } = "";
    }

    public class BankRecord
    {
        public int BankID { get; set; }
        public string BankName { get; set; } = "";
        public string BankCode { get; set; } = "";
        public int BankGroupID { get; set; }
        public string BankGroupName { get; set; } = "";
        public string LocationName { get; set; } = "";
        public string AccountTitle { get; set; } = "";
        public string IBAN { get; set; } = "";
        public string SwiftBICCode { get; set; } = "";
        public string CurrencyCode { get; set; } = "";
        public string AccountVerificationStatus { get; set; } = "Pending";
        public bool IsActive { get; set; } = true;
    }

    public partial class BankSetupPage : AppBasePage
    {
        private const string TableName = "tblBankMaster";

        public string PageTitle => "Bank Setup";
        public BankRecord Input { get; set; } = new BankRecord { IsActive = true };
        public List<BankRecord> Banks { get; set; } = new List<BankRecord>();
        public List<LookupItem> BankGroups { get; set; } = new List<LookupItem>();
        public List<BankCurrencyItem> Currencies { get; set; } = new List<BankCurrencyItem>();
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
            AlertMessage = msg;
            AlertType = typ;
            try
            {
                EnsureBankSchema();
                LoadBankGroups();
                LoadCurrencies();
                if (editId.HasValue && editId > 0)
                    LoadForEdit(editId.Value);
                LoadBanks();
            }
            catch (Exception ex)
            {
                AlertMessage = "Error loading Bank Setup: " + ex.Message;
                AlertType = "error";
            }
        }

        private void EnsureBankSchema()
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"
IF COL_LENGTH(N'tblBankMaster', N'AccountTitle') IS NULL
    ALTER TABLE tblBankMaster ADD AccountTitle NVARCHAR(150) NULL;
IF COL_LENGTH(N'tblBankMaster', N'CreatedByUserID') IS NULL
    ALTER TABLE tblBankMaster ADD CreatedByUserID INT NULL;
IF COL_LENGTH(N'tblBankMaster', N'ModifiedByUserID') IS NULL
    ALTER TABLE tblBankMaster ADD ModifiedByUserID INT NULL;", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    DbSchemaHelper.ClearCache();
                }
            }
            catch (SqlException)
            {
                // App identity may lack ALTER rights; run Production_BankSetup_Patch.sql instead.
                DbSchemaHelper.ClearCache();
            }
        }

        private void Save()
        {
            if (!Perms.CanWrite("BankSetup"))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect("~/BankSetup.aspx");
                return;
            }

            var bankID = int.TryParse(Request.Form["bankID"], out var id) ? id : 0;
            var bankName = FormString("bankName");
            var bankCode = FormString("bankCode");
            var locationName = FormString("locationName");
            var accountTitle = FormString("accountTitle");
            var bankGroupID = int.TryParse(Request.Form["bankGroupID"], out var gid) ? gid : 0;
            var iban = FormString("iban");
            var swiftBICCode = FormString("swiftBICCode");
            var currencyCode = FormString("currencyCode");
            var accountVerificationStatus = FormString("accountVerificationStatus");
            var isActive = FormBool("isActive");

            if (string.IsNullOrWhiteSpace(bankName))
            {
                SetAlert("Bank Name is required.", "error");
                Response.Redirect("~/BankSetup.aspx");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    EnsureBankSchema();

                    var hasAccountTitle = DbSchemaHelper.HasColumn(conn, null, TableName, "AccountTitle");
                    var hasBankGroup = DbSchemaHelper.HasColumn(conn, null, TableName, "BankGroupID");
                    var hasCreatedBy = DbSchemaHelper.HasColumn(conn, null, TableName, "CreatedByUserID");
                    var hasModifiedBy = DbSchemaHelper.HasColumn(conn, null, TableName, "ModifiedByUserID");
                    var hasModifiedOn = DbSchemaHelper.HasColumn(conn, null, TableName, "ModifiedOn");
                    var hasCreatedOn = DbSchemaHelper.HasColumn(conn, null, TableName, "CreatedOn");

                    if (bankID > 0)
                    {
                        var setParts = new List<string>
                        {
                            "BankName=@BankName",
                            "BankCode=@BankCode",
                            "LocationName=@LocationName",
                            "IBAN=@IBAN",
                            "SwiftBICCode=@SwiftBICCode",
                            "CurrencyCode=@CurrencyCode",
                            "AccountVerificationStatus=@AccountVerificationStatus",
                            "IsActive=@IsActive"
                        };
                        if (hasAccountTitle) setParts.Add("AccountTitle=@AccountTitle");
                        if (hasBankGroup) setParts.Add("BankGroupID=@BankGroupID");
                        if (hasModifiedOn) setParts.Add("ModifiedOn=GETDATE()");
                        if (hasModifiedBy) setParts.Add("ModifiedByUserID=@ModifiedByUserID");

                        using (var cmd = new SqlCommand(
                            "UPDATE " + TableName + " SET " + string.Join(", ", setParts) + " WHERE BankID=@BankID;", conn))
                        {
                            AddSaveParameters(cmd, bankID, bankName, bankCode, locationName, accountTitle,
                                bankGroupID, iban, swiftBICCode, currencyCode, accountVerificationStatus, isActive,
                                hasAccountTitle, hasBankGroup);
                            if (hasModifiedBy) AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Bank Master record updated successfully.");
                    }
                    else
                    {
                        var cols = new List<string>
                        {
                            "BankName", "BankCode", "LocationName", "IBAN", "SwiftBICCode",
                            "CurrencyCode", "AccountVerificationStatus", "IsActive"
                        };
                        var vals = new List<string>
                        {
                            "@BankName", "@BankCode", "@LocationName", "@IBAN", "@SwiftBICCode",
                            "@CurrencyCode", "@AccountVerificationStatus", "@IsActive"
                        };
                        if (hasAccountTitle) { cols.Add("AccountTitle"); vals.Add("@AccountTitle"); }
                        if (hasBankGroup) { cols.Add("BankGroupID"); vals.Add("@BankGroupID"); }
                        if (hasCreatedOn) { cols.Add("CreatedOn"); vals.Add("GETDATE()"); }
                        if (hasCreatedBy) { cols.Add("CreatedByUserID"); vals.Add("@CreatedByUserID"); }

                        using (var cmd = new SqlCommand(
                            "INSERT INTO " + TableName + " (" + string.Join(", ", cols) + ") VALUES ("
                            + string.Join(", ", vals) + ");", conn))
                        {
                            AddSaveParameters(cmd, bankID, bankName, bankCode, locationName, accountTitle,
                                bankGroupID, iban, swiftBICCode, currencyCode, accountVerificationStatus, isActive,
                                hasAccountTitle, hasBankGroup);
                            if (hasCreatedBy) AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Bank Master record added successfully.");
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                SetAlert("This bank/account combination already exists.", "error");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }
            Response.Redirect("~/BankSetup.aspx");
        }

        private void SoftDelete(int deleteId)
        {
            if (!Perms.CanDelete("BankSetup") && !Perms.CanWrite("BankSetup"))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect("~/BankSetup.aspx");
                return;
            }
            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    var hasModifiedBy = DbSchemaHelper.HasColumn(conn, null, TableName, "ModifiedByUserID");
                    var hasModifiedOn = DbSchemaHelper.HasColumn(conn, null, TableName, "ModifiedOn");
                    var sql = new StringBuilder("UPDATE " + TableName + " SET IsActive=0");
                    if (hasModifiedOn) sql.Append(", ModifiedOn=GETDATE()");
                    if (hasModifiedBy) sql.Append(", ModifiedByUserID=@ModifiedByUserID");
                    sql.Append(" WHERE BankID=@BankID;");

                    using (var cmd = new SqlCommand(sql.ToString(), conn))
                    {
                        cmd.Parameters.AddWithValue("@BankID", deleteId);
                        if (hasModifiedBy) AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                        cmd.ExecuteNonQuery();
                    }
                }
                SetAlert("Bank Master record removed successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error removing record: " + ex.Message, "error");
            }
            Response.Redirect("~/BankSetup.aspx");
        }

        private static void AddSaveParameters(
            SqlCommand cmd,
            int bankID,
            string bankName,
            string bankCode,
            string locationName,
            string accountTitle,
            int bankGroupID,
            string iban,
            string swiftBICCode,
            string currencyCode,
            string accountVerificationStatus,
            bool isActive,
            bool includeAccountTitle,
            bool includeBankGroup)
        {
            cmd.Parameters.AddWithValue("@BankID", bankID);
            cmd.Parameters.AddWithValue("@BankName", bankName.Trim());
            cmd.Parameters.AddWithValue("@BankCode", string.IsNullOrWhiteSpace(bankCode) ? (object)DBNull.Value : bankCode.Trim());
            cmd.Parameters.AddWithValue("@LocationName", string.IsNullOrWhiteSpace(locationName) ? (object)DBNull.Value : locationName.Trim());
            if (includeAccountTitle)
                cmd.Parameters.AddWithValue("@AccountTitle", string.IsNullOrWhiteSpace(accountTitle) ? (object)DBNull.Value : accountTitle.Trim());
            if (includeBankGroup)
                cmd.Parameters.AddWithValue("@BankGroupID", bankGroupID > 0 ? (object)bankGroupID : DBNull.Value);
            cmd.Parameters.AddWithValue("@IBAN", string.IsNullOrWhiteSpace(iban) ? (object)DBNull.Value : iban.Trim());
            cmd.Parameters.AddWithValue("@SwiftBICCode", string.IsNullOrWhiteSpace(swiftBICCode) ? (object)DBNull.Value : swiftBICCode.Trim());
            cmd.Parameters.AddWithValue("@CurrencyCode", string.IsNullOrWhiteSpace(currencyCode) ? (object)DBNull.Value : currencyCode.Trim());
            cmd.Parameters.AddWithValue("@AccountVerificationStatus", string.IsNullOrWhiteSpace(accountVerificationStatus) ? "Pending" : accountVerificationStatus.Trim());
            cmd.Parameters.AddWithValue("@IsActive", isActive);
        }

        private void LoadForEdit(int bankID)
        {
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(BuildSelectSql() + " WHERE b.BankID = @BankID;", conn))
                {
                    cmd.Parameters.AddWithValue("@BankID", bankID);
                    using (var dr = cmd.ExecuteReader())
                        if (dr.Read())
                            Input = ReadBank(dr);
                }
            }
        }

        private void LoadBanks()
        {
            Banks.Clear();
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    BuildSelectSql() + " ORDER BY b.IsActive DESC, b.BankName, b.LocationName;", conn))
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        Banks.Add(ReadBank(dr));
            }
        }

        private string BuildSelectSql()
        {
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                var hasAccountTitle = DbSchemaHelper.HasColumn(conn, null, TableName, "AccountTitle");
                var accountTitleExpr = hasAccountTitle ? "b.AccountTitle" : "CAST(NULL AS NVARCHAR(150)) AS AccountTitle";
                return @"
SELECT b.BankID, b.BankName, b.BankCode, ISNULL(b.BankGroupID, 0) AS BankGroupID,
       ISNULL(g.BankGroupName, '') AS BankGroupName,
       b.LocationName, " + accountTitleExpr + @", b.IBAN, b.SwiftBICCode,
       b.CurrencyCode, b.AccountVerificationStatus, b.IsActive
FROM tblBankMaster b
LEFT JOIN tblBankGroup g ON g.BankGroupID = b.BankGroupID";
            }
        }

        private void LoadBankGroups()
        {
            BankGroups.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT BankGroupID, BankGroupName FROM tblBankGroup WHERE IsActive=1 ORDER BY BankGroupName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        BankGroups.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
            }
        }

        private void LoadCurrencies()
        {
            Currencies.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT CurrencyCode, CurrencyName FROM tblCurrency WHERE IsActive=1 ORDER BY CurrencyCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        Currencies.Add(new BankCurrencyItem { CurrencyCode = dr.GetString(0), CurrencyName = dr.GetString(1) });
            }
        }

        private static BankRecord ReadBank(SqlDataReader dr)
        {
            return new BankRecord
            {
                BankID = Convert.ToInt32(dr["BankID"]),
                BankName = dr["BankName"].ToString() ?? "",
                BankCode = dr["BankCode"] == DBNull.Value ? "" : dr["BankCode"].ToString() ?? "",
                BankGroupID = dr["BankGroupID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["BankGroupID"]),
                BankGroupName = dr["BankGroupName"].ToString() ?? "",
                LocationName = dr["LocationName"] == DBNull.Value ? "" : dr["LocationName"].ToString() ?? "",
                AccountTitle = dr["AccountTitle"] == DBNull.Value ? "" : dr["AccountTitle"].ToString() ?? "",
                IBAN = dr["IBAN"] == DBNull.Value ? "" : dr["IBAN"].ToString() ?? "",
                SwiftBICCode = dr["SwiftBICCode"] == DBNull.Value ? "" : dr["SwiftBICCode"].ToString() ?? "",
                CurrencyCode = dr["CurrencyCode"] == DBNull.Value ? "" : dr["CurrencyCode"].ToString() ?? "",
                AccountVerificationStatus = dr["AccountVerificationStatus"] == DBNull.Value ? "Pending" : dr["AccountVerificationStatus"].ToString() ?? "Pending",
                IsActive = Convert.ToBoolean(dr["IsActive"])
            };
        }
    }
}
