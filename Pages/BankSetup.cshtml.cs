using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

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

public class BankSetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public BankSetupModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public BankRecord Input { get; set; } = new();
    public List<BankRecord> Banks { get; set; } = new();
    public List<LookupItem> BankGroups { get; set; } = new();
    public List<BankCurrencyItem> Currencies { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        LoadBankGroups();
        LoadCurrencies();
        if (editId.HasValue && editId > 0)
        {
            LoadForEdit(editId.Value);
        }

        LoadBanks();
    }

    public IActionResult OnPostSave(
        int bankID,
        string bankName,
        string bankCode,
        string locationName,
        string accountTitle,
        string iban,
        string swiftBICCode,
        string currencyCode,
        string accountVerificationStatus,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(bankName))
        {
            TempData["Alert"] = "Bank Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (bankID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblBankMaster
                    SET BankName = @BankName,
                        BankCode = @BankCode,
                        LocationName = @LocationName,
                        AccountTitle = @AccountTitle,
                        IBAN = @IBAN,
                        SwiftBICCode = @SwiftBICCode,
                        CurrencyCode = @CurrencyCode,
                        AccountVerificationStatus = @AccountVerificationStatus,
                        IsActive = @IsActive,
                        ModifiedOn = GETDATE(),
                        ModifiedByUserID = @ModifiedByUserID
                    WHERE BankID = @BankID;", conn);
                AddSaveParameters(cmd, bankID, bankName, bankCode, locationName, accountTitle, iban, swiftBICCode, currencyCode, accountVerificationStatus, isActive);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();

                TempData["Alert"] = "Bank Master record updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblBankMaster
                        (BankName, BankCode, LocationName, AccountTitle, IBAN, SwiftBICCode, CurrencyCode, AccountVerificationStatus, IsActive, CreatedOn, CreatedByUserID)
                    VALUES
                        (@BankName, @BankCode, @LocationName, @AccountTitle, @IBAN, @SwiftBICCode, @CurrencyCode, @AccountVerificationStatus, @IsActive, GETDATE(), @CreatedByUserID);", conn);
                AddSaveParameters(cmd, bankID, bankName, bankCode, locationName, accountTitle, iban, swiftBICCode, currencyCode, accountVerificationStatus, isActive);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();

                TempData["Alert"] = "Bank Master record added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "This bank/account combination already exists.";
            TempData["AlertType"] = "error";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                UPDATE tblBankMaster
                SET IsActive = 0,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE BankID = @BankID;", conn);
            cmd.Parameters.AddWithValue("@BankID", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Bank Master record removed successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private static void AddSaveParameters(
        SqlCommand cmd,
        int bankID,
        string bankName,
        string bankCode,
        string locationName,
        string accountTitle,
        string iban,
        string swiftBICCode,
        string currencyCode,
        string accountVerificationStatus,
        bool isActive)
    {
        cmd.Parameters.AddWithValue("@BankID", bankID);
        cmd.Parameters.AddWithValue("@BankName", bankName.Trim());
        cmd.Parameters.AddWithValue("@BankCode", string.IsNullOrWhiteSpace(bankCode) ? DBNull.Value : bankCode.Trim());
        cmd.Parameters.AddWithValue("@LocationName", string.IsNullOrWhiteSpace(locationName) ? DBNull.Value : locationName.Trim());
        cmd.Parameters.AddWithValue("@AccountTitle", string.IsNullOrWhiteSpace(accountTitle) ? DBNull.Value : accountTitle.Trim());
        cmd.Parameters.AddWithValue("@IBAN", string.IsNullOrWhiteSpace(iban) ? DBNull.Value : iban.Trim());
        cmd.Parameters.AddWithValue("@SwiftBICCode", string.IsNullOrWhiteSpace(swiftBICCode) ? DBNull.Value : swiftBICCode.Trim());
        cmd.Parameters.AddWithValue("@CurrencyCode", string.IsNullOrWhiteSpace(currencyCode) ? DBNull.Value : currencyCode.Trim());
        cmd.Parameters.AddWithValue("@AccountVerificationStatus", string.IsNullOrWhiteSpace(accountVerificationStatus) ? "Pending" : accountVerificationStatus.Trim());
        cmd.Parameters.AddWithValue("@IsActive", isActive);
    }

    private void LoadForEdit(int bankID)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT b.BankID, b.BankName, b.BankCode, ISNULL(b.BankGroupID, 0) AS BankGroupID,
                   ISNULL(g.BankGroupName, '') AS BankGroupName,
                   b.LocationName, b.AccountTitle, b.IBAN, b.SwiftBICCode,
                   b.CurrencyCode, b.AccountVerificationStatus, b.IsActive
            FROM tblBankMaster b
            LEFT JOIN tblBankGroup g ON g.BankGroupID = b.BankGroupID
            WHERE b.BankID = @BankID;", conn);
        cmd.Parameters.AddWithValue("@BankID", bankID);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            Input = ReadBank(dr);
        }
    }

    private void LoadBanks()
    {
        Banks.Clear();

        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT b.BankID, b.BankName, b.BankCode, ISNULL(b.BankGroupID, 0) AS BankGroupID,
                   ISNULL(g.BankGroupName, '') AS BankGroupName,
                   b.LocationName, b.AccountTitle, b.IBAN, b.SwiftBICCode,
                   b.CurrencyCode, b.AccountVerificationStatus, b.IsActive
            FROM tblBankMaster b
            LEFT JOIN tblBankGroup g ON g.BankGroupID = b.BankGroupID
            ORDER BY b.IsActive DESC, b.BankName, b.LocationName;", conn);
        conn.Open();

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Banks.Add(ReadBank(dr));
        }
    }

    private void LoadBankGroups()
    {
        BankGroups.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"SELECT BankGroupID, BankGroupName FROM tblBankGroup WHERE IsActive = 1 ORDER BY BankGroupName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            BankGroups.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
    }

    private void LoadCurrencies()
    {
        Currencies.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"SELECT CurrencyCode, CurrencyName FROM tblCurrency WHERE IsActive = 1 ORDER BY CurrencyCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            Currencies.Add(new BankCurrencyItem { CurrencyCode = dr.GetString(0), CurrencyName = dr.GetString(1) });
    }

    private static BankRecord ReadBank(SqlDataReader dr)
    {
        return new BankRecord
        {
            BankID = Convert.ToInt32(dr["BankID"]),
            BankName = dr["BankName"].ToString() ?? "",
            BankCode = dr["BankCode"].ToString() ?? "",
            BankGroupID = dr["BankGroupID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["BankGroupID"]),
            BankGroupName = dr["BankGroupName"].ToString() ?? "",
            LocationName = dr["LocationName"].ToString() ?? "",
            AccountTitle = dr["AccountTitle"].ToString() ?? "",
            IBAN = dr["IBAN"].ToString() ?? "",
            SwiftBICCode = dr["SwiftBICCode"].ToString() ?? "",
            CurrencyCode = dr["CurrencyCode"].ToString() ?? "",
            AccountVerificationStatus = dr["AccountVerificationStatus"].ToString() ?? "Pending",
            IsActive = Convert.ToBoolean(dr["IsActive"])
        };
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;

        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType = TempData["AlertType"]?.ToString() ?? "success";
    }
}
