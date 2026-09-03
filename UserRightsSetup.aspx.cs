using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
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

    public class ScopeJsonDto
    {
        public string Mode { get; set; } = "OwnOnly";
        public bool IncludeUnassignedDepartment { get; set; }
        public bool IncludeUnassignedLocation { get; set; }
        public List<int> DepartmentIds { get; set; } = new List<int>();
        public List<int> LocationIds { get; set; } = new List<int>();
    }

    public partial class UserRightsSetupPage : AppBasePage
    {
        private readonly DataAccessScopeService _dataScope = new DataAccessScopeService();

        public string PageTitle => "User Rights Setup";
        public List<UserRightsUserItem> Users { get; set; } = new List<UserRightsUserItem>();
        public List<PermissionRow> Permissions { get; set; } = new List<PermissionRow>();
        public List<ScopeLookupItem> Departments { get; set; } = new List<ScopeLookupItem>();
        public List<ScopeLookupItem> Locations { get; set; } = new List<ScopeLookupItem>();
        public UserDataScopeSettings DataScope { get; set; } = new UserDataScopeSettings();
        public int SelectedUserId { get; set; }
        public string SelectedFullName { get; set; } = "";
        public bool SelectedIsAdmin { get; set; }
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (!Auth.IsAdmin)
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            if (IsPostBack)
            {
                OnPostSave();
                return;
            }

            LoadAlert(out var msg, out var typ);
            AlertMessage = msg; AlertType = typ;
            LoadUsers();
            LoadScopeLookups();
            var userId = QueryInt("userId");
            if (userId.HasValue && userId > 0)
            {
                SelectedUserId = userId.Value;
                LoadSelectedUser();
                LoadPermissions();
                DataScope = _dataScope.GetSettingsForUser(SelectedUserId);
            }
        }

        private void OnPostSave()
        {
            var userId = int.TryParse(Request.Form["userId"], out var uid) ? uid : 0;
            if (userId <= 0)
            {
                SetAlert("Select a user first.", "error");
                Response.Redirect("~/UserRightsSetup.aspx");
                return;
            }

            try
            {
                var items = WebFormsJson.DeserializeList<PermissionSaveItem>(Request.Form["permissionsJson"]);
                var permissions = items.Select(p => new FormPermission
                {
                    FormKey = p.FormKey,
                    CanRead = p.CanRead,
                    CanWrite = p.CanWrite,
                    CanDelete = p.CanDelete,
                    CanApprove = p.CanApprove,
                    CanExport = p.CanExport
                });
                Perms.SaveUserPermissions(userId, permissions);

                var scopeJson = Request.Form["scopeJson"];
                if (!string.IsNullOrWhiteSpace(scopeJson))
                {
                    var dto = WebFormsJson.Deserialize<ScopeJsonDto>(scopeJson);
                    var scope = new UserDataScopeSettings
                    {
                        Mode = ParseMode(dto.Mode),
                        IncludeUnassignedDepartment = dto.IncludeUnassignedDepartment,
                        IncludeUnassignedLocation = dto.IncludeUnassignedLocation,
                        DepartmentIds = dto.DepartmentIds ?? new List<int>(),
                        LocationIds = dto.LocationIds ?? new List<int>()
                    };
                    _dataScope.SaveUserDataScope(userId, scope, Auth.CurrentUserId);
                }

                SetAlert("User rights and data access scope saved successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error saving rights: " + ex.Message, "error");
            }
            Response.Redirect("~/UserRightsSetup.aspx?userId=" + userId);
        }

        private static DataScopeMode ParseMode(string mode)
        {
            if (string.Equals(mode, "All", StringComparison.OrdinalIgnoreCase)) return DataScopeMode.All;
            if (string.Equals(mode, "Restricted", StringComparison.OrdinalIgnoreCase)) return DataScopeMode.Restricted;
            return DataScopeMode.OwnOnly;
        }

        private void LoadUsers()
        {
            Users.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT UserID, Username, FullName FROM tblUser WHERE IsActive=1 ORDER BY FullName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
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
            }
        }

        private void LoadSelectedUser()
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT FullName, IsAdmin FROM tblUser WHERE UserID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", SelectedUserId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        SelectedFullName = dr.GetString(0);
                        SelectedIsAdmin = dr.GetBoolean(1);
                    }
                }
            }
        }

        private void LoadPermissions()
        {
            var existing = new Dictionary<string, PermissionRow>(StringComparer.OrdinalIgnoreCase);
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT FormKey, CanRead, CanWrite, CanDelete, ISNULL(CanApprove,0), ISNULL(CanExport,0)
FROM tblUserPermission WHERE UserID=@UserID;", conn))
            {
                cmd.Parameters.AddWithValue("@UserID", SelectedUserId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        existing[dr.GetString(0)] = new PermissionRow
                        {
                            CanRead = dr.GetBoolean(1),
                            CanWrite = dr.GetBoolean(2),
                            CanDelete = dr.GetBoolean(3),
                            CanApprove = dr.GetBoolean(4),
                            CanExport = dr.GetBoolean(5)
                        };
                    }
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
                        CanRead = perm?.CanRead ?? false,
                        CanWrite = perm?.CanWrite ?? false,
                        CanDelete = perm?.CanDelete ?? false,
                        CanApprove = perm?.CanApprove ?? false,
                        CanExport = perm?.CanExport ?? false
                    };
                }).ToList();
        }

        private void LoadScopeLookups()
        {
            Departments.Clear();
            Locations.Clear();
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT DepartmentID, DepartmentName FROM tblDepartment ORDER BY DepartmentName;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Departments.Add(new ScopeLookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
                }
                using (var cmd = new SqlCommand("SELECT LocationID, LocationName FROM tblLocation WHERE IsActive=1 ORDER BY LocationName;", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        Locations.Add(new ScopeLookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
                }
            }
        }
    }
}
