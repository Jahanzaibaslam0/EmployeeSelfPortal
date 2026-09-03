using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace HRMS.Services
{
    public class FormPermission
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

    public class PermissionService
    {
        public const string AccessRestrictedMessage =
            "This site is restricted. Please contact the administration.";

        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();

        public PermissionService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public bool CanRead(string formKey)
            => _auth.IsAdmin || HasRight(formKey, p => p.CanRead);

        public bool CanWrite(string formKey)
            => _auth.IsAdmin || HasRight(formKey, p => p.CanWrite);

        public bool CanDelete(string formKey)
            => _auth.IsAdmin || HasRight(formKey, p => p.CanDelete);

        public bool CanApprove(string formKey)
            => _auth.IsAdmin || HasRight(formKey, p => p.CanApprove);

        public bool CanExport(string formKey)
            => _auth.IsAdmin || HasRight(formKey, p => p.CanExport);

        public bool HasFullEmployeeMasterAccess()
            => _auth.IsAdmin || HasRight(EmployeeProfileAccessService.EmployeeMasterFormKey, p => p.CanRead);

        public bool CanShowMasterNavMenu()
            => HasFullEmployeeMasterAccess()
               || CanRead("PositionMaster")
               || CanRead("CustomerMaster")
               || CanRead("VendorMaster")
               || CanRead("ContactMaster")
               || CanRead("ProductMaster")
               || CanRead("InvoiceMaster")
               || CanRead("PurchaseOrderMaster")
               || CanRead("SalesOrderMaster")
               || CanRead("InventoryMaster")
               || CanRead("TaskMaster");

        public bool CanAccessPage(string pagePath)
        {
            if (_auth.IsAdmin) return true;
            if (IsHomePath(pagePath)) return true;

            var form = AppForms.FindByPath(pagePath);
            if (form == null)
            {
                // Fail closed for unregistered pages, except known hubs/utilities.
                return IsUnlistedAllowedPath(pagePath);
            }

            if (!CanAccessEmployeeRelatedForm(form.Key))
                return false;

            if (IsProfileRelatedForm(form.Key) && GetLinkedEmployeeId() > 0)
                return true;

            if (form.Key.Equals(EmployeeProfileAccessService.EmployeeMasterFormKey, StringComparison.OrdinalIgnoreCase)
                && CanRead(EmployeeProfileAccessService.UserProfileFormKey))
                return true;

            if (form.Key.Equals(EmployeeProfileAccessService.UserProfileFormKey, StringComparison.OrdinalIgnoreCase))
                return true;

            if (form.Key.Equals(EmployeeProfileAccessService.MyDocumentsFormKey, StringComparison.OrdinalIgnoreCase))
                return new EmployeeProfileAccessService().CanAccessMyDocuments();

            if (form.Key.Equals(LmsDocumentService.LibraryFormKey, StringComparison.OrdinalIgnoreCase))
                return new LmsDocumentService().CanAccessLibrary();

            return CanRead(form.Key);
        }

        /// <summary>Pages not in AppForms that logged-in users may still open.</summary>
        private static bool IsUnlistedAllowedPath(string pagePath)
        {
            var p = AppForms.NormalizePath(pagePath);
            // Setup hub filters its own tiles by CanRead; allow entry so users can open granted setups.
            if (p == "/setup") return true;
            // Public/bootstrap utilities are gated elsewhere (IsPublicPage / admin checks).
            if (p == "/login" || p == "/resetadmin" || p == "/initdatabase") return true;
            return false;
        }

        private static bool IsHomePath(string pagePath)
        {
            var p = AppForms.NormalizePath(pagePath);
            return string.IsNullOrEmpty(p) || p == "/index" || p == "/home";
        }

        private bool CanAccessEmployeeRelatedForm(string formKey)
        {
            if (_auth.IsAdmin) return true;
            if (!IsEmployeeRelatedForm(formKey)) return true;
            if (formKey.Equals(EmployeeProfileAccessService.UserProfileFormKey, StringComparison.OrdinalIgnoreCase))
                return true;
            return GetLinkedEmployeeId() > 0;
        }

        private static bool IsEmployeeRelatedForm(string formKey)
            => formKey.Equals(EmployeeProfileAccessService.UserProfileFormKey, StringComparison.OrdinalIgnoreCase)
               || formKey.Equals(EmployeeProfileAccessService.EmployeeMasterFormKey, StringComparison.OrdinalIgnoreCase)
               || formKey.Equals(EmployeeProfileAccessService.MyDocumentsFormKey, StringComparison.OrdinalIgnoreCase)
               || formKey.Equals("PerformanceMaster", StringComparison.OrdinalIgnoreCase)
               || formKey.Equals("TrainingMaster", StringComparison.OrdinalIgnoreCase)
               || formKey.Equals("ExpenseMaster", StringComparison.OrdinalIgnoreCase)
               || formKey.Equals("RecruitmentMaster", StringComparison.OrdinalIgnoreCase)
               || formKey.Equals("EmployeeReport", StringComparison.OrdinalIgnoreCase);

        private static bool IsProfileRelatedForm(string formKey)
            => formKey.Equals(EmployeeProfileAccessService.UserProfileFormKey, StringComparison.OrdinalIgnoreCase)
               || formKey.Equals(EmployeeProfileAccessService.EmployeeMasterFormKey, StringComparison.OrdinalIgnoreCase)
               || formKey.Equals(EmployeeProfileAccessService.MyDocumentsFormKey, StringComparison.OrdinalIgnoreCase);

        private int GetLinkedEmployeeId()
        {
            if (_auth.LinkedEmployeeId is int sessionId && sessionId > 0)
                return sessionId;

            if (!_auth.CurrentUserId.HasValue)
                return 0;

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
            SELECT TOP 1 EmployeeID
            FROM tblEmployee
            WHERE UserID = @UserID
            ORDER BY EmployeeID;", conn))
            {
                cmd.Parameters.AddWithValue("@UserID", _auth.CurrentUserId.Value);
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        /// <summary>Grant baseline rights every user needs (Home + own profile).</summary>
        public void GrantDefaultUserPermissions(int userId)
        {
            var defaults = new[]
            {
                new { Key = "Home", Read = true, Write = false, Delete = false },
                new { Key = "UserProfile", Read = true, Write = false, Delete = false },
                new { Key = "MyDocuments", Read = true, Write = false, Delete = false },
                new { Key = "LmsLibrary", Read = true, Write = false, Delete = false },
                new { Key = "Dashboard", Read = true, Write = false, Delete = false },
            };

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();

                foreach (var d in defaults)
                {
                    using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM tblUserPermission WHERE UserID = @UserID AND FormKey = @FormKey)
                INSERT INTO tblUserPermission (UserID, FormKey, CanRead, CanWrite, CanDelete, CreatedOn, CreatedByUserID)
                VALUES (@UserID, @FormKey, @CanRead, @CanWrite, @CanDelete, GETDATE(), @CreatedByUserID);", conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@FormKey", d.Key);
                        cmd.Parameters.AddWithValue("@CanRead", d.Read);
                        cmd.Parameters.AddWithValue("@CanWrite", d.Write);
                        cmd.Parameters.AddWithValue("@CanDelete", d.Delete);
                        AuditHelper.AddCreatedBy(cmd, _auth.CurrentUserId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private bool HasRight(string formKey, Func<FormPermission, bool> selector)
        {
            if (!_auth.CurrentUserId.HasValue) return false;
            var perms = GetUserPermissions(_auth.CurrentUserId.Value);
            var p = perms.FirstOrDefault(x => x.FormKey.Equals(formKey, StringComparison.OrdinalIgnoreCase));
            return p != null && selector(p);
        }

        public List<FormPermission> GetUserPermissions(int userId)
        {
            var map = AppForms.All.ToDictionary(
                f => f.Key,
                f => new FormPermission
                {
                    FormKey = f.Key,
                    FormName = f.Name,
                    Category = f.Category
                },
                StringComparer.OrdinalIgnoreCase);

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
            SELECT FormKey, CanRead, CanWrite, CanDelete,
                   ISNULL(CanApprove, 0) AS CanApprove,
                   ISNULL(CanExport, 0) AS CanExport
            FROM   tblUserPermission
            WHERE  UserID = @UserID;", conn))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                conn.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var key = dr["FormKey"].ToString() ?? "";
                        if (!map.TryGetValue(key, out var perm)) continue;
                        perm.CanRead = Convert.ToBoolean(dr["CanRead"]);
                        perm.CanWrite = Convert.ToBoolean(dr["CanWrite"]);
                        perm.CanDelete = Convert.ToBoolean(dr["CanDelete"]);
                        perm.CanApprove = Convert.ToBoolean(dr["CanApprove"]);
                        perm.CanExport = Convert.ToBoolean(dr["CanExport"]);
                    }
                }
            }

            return AppForms.All
                .Select(f => map[f.Key])
                .OrderBy(p => AppForms.All.First(f => f.Key == p.FormKey).SortOrder)
                .ToList();
        }

        public void SaveUserPermissions(int userId, IEnumerable<FormPermission> permissions)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    using (var del = new SqlCommand("DELETE FROM tblUserPermission WHERE UserID = @UserID;", conn, tx))
                    {
                        del.Parameters.AddWithValue("@UserID", userId);
                        del.ExecuteNonQuery();
                    }

                    foreach (var p in permissions.Where(x => x.CanRead || x.CanWrite || x.CanDelete || x.CanApprove || x.CanExport))
                    {
                        using (var ins = new SqlCommand(@"
                INSERT INTO tblUserPermission
                    (UserID, FormKey, CanRead, CanWrite, CanDelete, CanApprove, CanExport, CreatedOn, CreatedByUserID)
                VALUES
                    (@UserID, @FormKey, @CanRead, @CanWrite, @CanDelete, @CanApprove, @CanExport, GETDATE(), @CreatedByUserID);", conn, tx))
                        {
                            ins.Parameters.AddWithValue("@UserID", userId);
                            ins.Parameters.AddWithValue("@FormKey", p.FormKey);
                            ins.Parameters.AddWithValue("@CanRead", p.CanRead);
                            ins.Parameters.AddWithValue("@CanWrite", p.CanWrite);
                            ins.Parameters.AddWithValue("@CanDelete", p.CanDelete);
                            ins.Parameters.AddWithValue("@CanApprove", p.CanApprove);
                            ins.Parameters.AddWithValue("@CanExport", p.CanExport);
                            AuditHelper.AddCreatedBy(ins, _auth.CurrentUserId);
                            ins.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }

            GrantDefaultUserPermissions(userId);
        }
    }
}
