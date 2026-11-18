using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class CurrencyRecord
{
    public int CurrencyID { get; set; }
    public string CurrencyCode { get; set; } = "";
    public string CurrencyName { get; set; } = "";
    public string AliasName { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class CurrencySetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;

    public CurrencySetupModel(IConfiguration config, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
    }

    public string PageTitle => "Currency Setup";
    public CurrencyRecord Input { get; set; } = new() { IsActive = true };
    public List<CurrencyRecord> Records { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        LoadRecords();
    }

    public IActionResult OnPostSave(int currencyID, string currencyCode, string currencyName, string aliasName, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            TempData["Alert"] = "Currency Code is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = currencyID > 0 ? currencyID : (int?)null });
        }

        if (string.IsNullOrWhiteSpace(currencyName))
        {
            TempData["Alert"] = "Currency Name is required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = currencyID > 0 ? currencyID : (int?)null });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (currencyID > 0)
            {
                using var cmd = new SqlCommand(@"
                    UPDATE tblCurrency
                    SET CurrencyCode = @Code,
                        CurrencyName = @Name,
                        AliasName = @AliasName,
                        IsActive = @IsActive,
                        ModifiedOn = GETDATE(),
                        ModifiedByUserID = @ModifiedByUserID
                    WHERE CurrencyID = @ID;", conn);
                AddParams(cmd, currencyID, currencyCode, currencyName, aliasName, isActive);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Currency updated successfully.";
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblCurrency (CurrencyCode, CurrencyName, AliasName, IsActive, CreatedOn, CreatedByUserID)
                    VALUES (@Code, @Name, @AliasName, @IsActive, GETDATE(), @CreatedByUserID);", conn);
                AddParams(cmd, 0, currencyCode, currencyName, aliasName, isActive);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                cmd.ExecuteNonQuery();
                TempData["Alert"] = "Currency added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "A currency with this code already exists.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = currencyID > 0 ? currencyID : (int?)null });
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = currencyID > 0 ? currencyID : (int?)null });
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                UPDATE tblCurrency
                SET IsActive = 0,
                    ModifiedOn = GETDATE(),
                    ModifiedByUserID = @ModifiedByUserID
                WHERE CurrencyID = @ID;", conn);
            cmd.Parameters.AddWithValue("@ID", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "Currency removed successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error removing record: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private void LoadForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT CurrencyID, CurrencyCode, CurrencyName, AliasName, IsActive
            FROM tblCurrency
            WHERE CurrencyID = @ID;", conn);
        cmd.Parameters.AddWithValue("@ID", id);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read()) Input = ReadRecord(dr);
    }

    private void LoadRecords()
    {
        Records.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT CurrencyID, CurrencyCode, CurrencyName, AliasName, IsActive
            FROM tblCurrency
            ORDER BY IsActive DESC, CurrencyCode;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read()) Records.Add(ReadRecord(dr));
    }

    private static CurrencyRecord ReadRecord(SqlDataReader dr) => new()
    {
        CurrencyID = Convert.ToInt32(dr["CurrencyID"]),
        CurrencyCode = dr["CurrencyCode"].ToString() ?? "",
        CurrencyName = dr["CurrencyName"].ToString() ?? "",
        AliasName = dr["AliasName"] == DBNull.Value ? "" : dr["AliasName"].ToString() ?? "",
        IsActive = Convert.ToBoolean(dr["IsActive"])
    };

    private static void AddParams(SqlCommand cmd, int id, string code, string name, string alias, bool isActive)
    {
        if (id > 0) cmd.Parameters.AddWithValue("@ID", id);
        cmd.Parameters.AddWithValue("@Code", code.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("@Name", name.Trim());
        cmd.Parameters.AddWithValue("@AliasName", string.IsNullOrWhiteSpace(alias) ? DBNull.Value : alias.Trim());
        cmd.Parameters.AddWithValue("@IsActive", isActive);
    }

    private void LoadAlert()
    {
        if (!TempData.ContainsKey("Alert")) return;
        AlertMessage = TempData["Alert"]?.ToString() ?? "";
        AlertType = TempData["AlertType"]?.ToString() ?? "success";
    }
}
