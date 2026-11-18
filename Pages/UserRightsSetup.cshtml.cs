using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace HRMS.Pages;

public class UserRightsUserItem
{
    public int UserID { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
}

public class PermissionRow
{
    public string FormKey { get; set; } = "";
    public string FormName { get; set; } = "";
    public string Category { get; set; } = "";
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
    public bool CanExport { get; set; }
}

public class PermissionSaveItem
{
    public string FormKey { get; set; } = "";
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
    public bool CanExport { get; set; }
}

public class ScopeLookupItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class UserRightsSetupModel : PageModel
{
    private readonly string _conn;
    private readonly PermissionService _perms;
    private readonly DataAccessScopeService _dataScope;
    private readonly AuthService _auth;

    public UserRightsSetupModel(IConfiguration config, PermissionService perms, DataAccessScopeService dataScope, AuthService auth)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
        _perms = perms;
        _dataScope = dataScope;
        _auth = auth;
    }

    public string PageTitle => "User Rights Setup";
    public List<UserRightsUserItem> Users { get; set; } = new();
    public List<PermissionRow> Permissions { get; set; } = new();
    public List<ScopeLookupItem> Departments { get; set; } = new();
    public List<ScopeLookupItem> Locations { get; set; } = new();
    public UserDataScopeSettings DataScope { get; set; } = new();
    public int SelectedUserId { get; set; }
    public string SelectedFullName { get; set; } = "";
    public bool SelectedIsAdmin { get; set; }
    public string AlertMessage { get; set; } = "";
    public string AlertType { get; set; } = "success";

    public void OnGet(int? userId)
    {
        LoadAlert();
        LoadUsers();
        LoadScopeLookups();
        if (userId.HasValue && userId > 0)
        {
            SelectedUserId = userId.Value;
            LoadSelectedUser();
            LoadPermissions();
            DataScope = _dataScope.GetSettingsForUser(SelectedUserId);
        }
    }

    public IActionResult OnPostSave(int userId, string permissionsJson, string? scopeJson)
    {
        if (userId <= 0)
        {
            TempData["Alert"] = "Select a user first.";
            TempData["AlertType"] = "error";
            return RedirectToPage();
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<PermissionSaveItem>>(permissionsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            var permissions = items.Select(p => new FormPermission
            {
                FormKey  = p.FormKey,
                CanRead  = p.CanRead,
                CanWrite = p.CanWrite,
                CanDelete = p.CanDelete,
                CanApprove = p.CanApprove,
                CanExport = p.CanExport
            });

            _perms.SaveUserPermissions(userId, permissions);

            if (!string.IsNullOrWhiteSpace(scopeJson))
            {
                var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                jsonOpts.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                var scope = JsonSerializer.Deserialize<UserDataScopeSettings>(scopeJson, jsonOpts) ?? new UserDataScopeSettings();
                _dataScope.SaveUserDataScope(userId, scope, _auth.CurrentUserId);
            }

            TempData["Alert"] = "User rights and data access scope saved successfully.";
            TempData["AlertType"] = "success";
        }
        catch (Exception ex)
        {
            TempData["Alert"] = "Error saving rights: " + ex.Message;
            TempData["AlertType"] = "error";
        }

        return RedirectToPage(new { userId });
    }

    private void LoadUsers()
    {
        Users.Clear();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT UserID, Username, FullName FROM tblUser WHERE IsActive = 1 ORDER BY FullName;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            Users.Add(new UserRightsUserItem
            {
                UserID = dr.GetInt32(0),
                Username = dr.GetString(1),
                FullName = dr.GetString(2)
            });
        }
    }

    private void LoadSelectedUser()
    {
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT FullName, IsAdmin FROM tblUser WHERE UserID = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", SelectedUserId);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            SelectedFullName = dr.GetString(0);
            SelectedIsAdmin = dr.GetBoolean(1);
        }
    }

    private void LoadPermissions()
    {
        var existing = new Dictionary<string, (bool Read, bool Write, bool Delete, bool Approve, bool Export)>(StringComparer.OrdinalIgnoreCase);
        using (var conn = new SqlConnection(_conn))
        {
            using var cmd = new SqlCommand(@"
                SELECT FormKey, CanRead, CanWrite, CanDelete,
                       ISNULL(CanApprove, 0), ISNULL(CanExport, 0)
                FROM tblUserPermission WHERE UserID = @UserID;", conn);
            cmd.Parameters.AddWithValue("@UserID", SelectedUserId);
            conn.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                existing[dr.GetString(0)] = (
                    dr.GetBoolean(1), dr.GetBoolean(2), dr.GetBoolean(3),
                    dr.GetBoolean(4), dr.GetBoolean(5));
            }
        }

        Permissions = AppForms.All
            .Where(f => !f.Key.Equals("Home", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Category).ThenBy(f => f.SortOrder)
            .Select(f =>
            {
                existing.TryGetValue(f.Key, out var perm);
                return new PermissionRow
                {
                    FormKey = f.Key,
                    FormName = f.Name,
                    Category = f.Category,
                    CanRead = perm.Read,
                    CanWrite = perm.Write,
                    CanDelete = perm.Delete,
                    CanApprove = perm.Approve,
                    CanExport = perm.Export
                };
            })
            .ToList();
    }

    private void LoadScopeLookups()
    {
        Departments.Clear();
        Locations.Clear();
        using var conn = new SqlConnection(_conn);
        conn.Open();

        using (var cmd = new SqlCommand(@"
            SELECT DepartmentID, DepartmentName FROM tblDepartment ORDER BY DepartmentName;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
                Departments.Add(new ScopeLookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
        }

        using (var cmd = new SqlCommand(@"
            SELECT LocationID, LocationName FROM tblLocation WHERE IsActive = 1 ORDER BY LocationName;", conn))
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
                Locations.Add(new ScopeLookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
        }
    }

    private void LoadAlert()
    {
        if (TempData["Alert"] is string msg) AlertMessage = msg;
        if (TempData["AlertType"] is string type) AlertType = type;
    }
}
