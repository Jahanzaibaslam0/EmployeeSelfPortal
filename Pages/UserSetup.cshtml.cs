using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class UserRecord
{
    public int UserID { get; set; }
    public string UserCode { get; set; } = "";
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; }
}

public class UserInput
{
    public int UserID { get; set; }
    public string UserCode { get; set; } = "";
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; }
}

public class UserSetupModel : PageModel
{
    private readonly string _conn;
    private readonly AuthService _auth;
    private readonly PermissionService _perms;
    private readonly DataAccessScopeService _dataScope;

    public UserSetupModel(IConfiguration config, AuthService auth, PermissionService perms, DataAccessScopeService dataScope)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _auth = auth;
        _perms = perms;
        _dataScope = dataScope;
    }

    public string PageTitle => "User Setup";
    public UserInput Input { get; set; } = new() { IsActive = true };
    public List<UserRecord> Users { get; set; } = new();
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? editId)
    {
        LoadAlert();
        if (editId.HasValue && editId > 0)
            LoadForEdit(editId.Value);
        else
            Input.UserCode = GenerateNextUserCode();
        LoadUsers();
    }

    public IActionResult OnPostSave(
        int userId, string userCode, string username, string fullName,
        string email, string newPassword, bool isActive, bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName))
        {
            TempData["Alert"] = "Username and Full Name are required.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = userId > 0 ? userId : (int?)null });
        }

        if (!InputValidators.TryValidateEmail(email, out var emailError, required: false, "Email"))
        {
            TempData["Alert"] = emailError;
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = userId > 0 ? userId : (int?)null });
        }

        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();

            if (userId > 0)
            {
                var sql = @"
                    UPDATE tblUser
                    SET Username = @Username,
                        FullName = @FullName,
                        Email = @Email,
                        IsActive = @IsActive,
                        IsAdmin = @IsAdmin,
                        ModifiedOn = GETDATE(),
                        ModifiedByUserID = @ModifiedByUserID";
                if (!string.IsNullOrWhiteSpace(newPassword))
                    sql += ", PasswordHash = @PasswordHash";
                sql += " WHERE UserID = @UserID;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@Username", username.Trim());
                cmd.Parameters.AddWithValue("@FullName", fullName.Trim());
                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email.Trim());
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);
                AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
                if (!string.IsNullOrWhiteSpace(newPassword))
                    cmd.Parameters.AddWithValue("@PasswordHash", PasswordHelper.HashPassword(newPassword));
                cmd.ExecuteNonQuery();

                TempData["Alert"] = "User updated successfully.";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    TempData["Alert"] = "Password is required for new users.";
                    TempData["AlertType"] = "error";
                    return RedirectToPage();
                }

                var code = string.IsNullOrWhiteSpace(userCode) ? GenerateNextUserCode() : userCode.Trim();
                using var cmd = new SqlCommand(@"
                    INSERT INTO tblUser (UserCode, Username, PasswordHash, FullName, Email, IsActive, IsAdmin, CreatedOn, CreatedByUserID)
                    VALUES (@UserCode, @Username, @PasswordHash, @FullName, @Email, @IsActive, @IsAdmin, GETDATE(), @CreatedByUserID);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
                cmd.Parameters.AddWithValue("@UserCode", code);
                cmd.Parameters.AddWithValue("@Username", username.Trim());
                cmd.Parameters.AddWithValue("@PasswordHash", PasswordHelper.HashPassword(newPassword));
                cmd.Parameters.AddWithValue("@FullName", fullName.Trim());
                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email.Trim());
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);
                AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                var newUserId = Convert.ToInt32(cmd.ExecuteScalar());

                if (!isAdmin)
                {
                    _perms.GrantDefaultUserPermissions(newUserId);
                    _dataScope.EnsureDefaultScope(newUserId);
                }

                TempData["Alert"] = "User added successfully.";
            }

            TempData["AlertType"] = "success";
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            TempData["Alert"] = "Username already exists.";
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = userId > 0 ? userId : (int?)null });
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
            return RedirectToPage(new { editId = userId > 0 ? userId : (int?)null });
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int deleteId)
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            conn.Open();
            using var cmd = new SqlCommand(@"
                UPDATE tblUser SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                WHERE UserID = @UserID;", conn);
            cmd.Parameters.AddWithValue("@UserID", deleteId);
            AuditHelper.AddModifiedBy(cmd, _auth.CurrentUserId);
            cmd.ExecuteNonQuery();

            TempData["Alert"] = "User deactivated successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage();
    }

    private void LoadForEdit(int id)
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT UserID, UserCode, Username, FullName, Email, IsActive, IsAdmin
            FROM tblUser WHERE UserID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            Input = new UserInput
            {
                UserID = dr.GetInt32(0),
                UserCode = dr.IsDBNull(1) ? "" : dr.GetString(1),
                Username = dr.GetString(2),
                FullName = dr.GetString(3),
                Email = dr.IsDBNull(4) ? "" : dr.GetString(4),
                IsActive = dr.GetBoolean(5),
                IsAdmin = dr.GetBoolean(6)
            };
        }
    }

    private void LoadUsers()
    {
        Users.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT UserID, UserCode, Username, FullName, Email, IsActive, IsAdmin
            FROM tblUser ORDER BY IsActive DESC, FullName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Users.Add(new UserRecord
            {
                UserID = dr.GetInt32(0),
                UserCode = dr.IsDBNull(1) ? "" : dr.GetString(1),
                Username = dr.GetString(2),
                FullName = dr.GetString(3),
                Email = dr.IsDBNull(4) ? "" : dr.GetString(4),
                IsActive = dr.GetBoolean(5),
                IsAdmin = dr.GetBoolean(6)
            });
        }
    }

    private string GenerateNextUserCode()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT TOP 1 UserCode FROM tblUser WHERE UserCode LIKE 'USR-%' ORDER BY UserCode DESC;", conn);
        conn.Open();
        var last = cmd.ExecuteScalar()?.ToString();
        if (!string.IsNullOrEmpty(last) && last.Length >= 7 && int.TryParse(last[4..], out int num))
            return $"USR-{(num + 1):D3}";
        return "USR-001";
    }

    private void LoadAlert()
    {
        if (TempData["Alert"] is string msg) AlertMessage = msg;
        if (TempData["AlertType"] is string type) AlertType = type;
    }
}
