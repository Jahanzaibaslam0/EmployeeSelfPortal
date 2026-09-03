using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace HRMS.Services
{
    /// <summary>How broadly a user may see employee-linked data.</summary>
    public enum DataScopeMode
    {
        /// <summary>Only the employee record linked to the user account (default).</summary>
        OwnOnly,
        /// <summary>Department and/or location assignments configured by an administrator.</summary>
        Restricted,
        /// <summary>All employee-linked data (within form permissions).</summary>
        All
    }

    public class UserDataScopeSettings
    {
        public DataScopeMode Mode { get; set; } = DataScopeMode.OwnOnly;
        public bool IncludeUnassignedDepartment { get; set; }
        public bool IncludeUnassignedLocation { get; set; }
        public List<int> DepartmentIds { get; set; } = new List<int>();
        public List<int> LocationIds { get; set; } = new List<int>();
    }

    /// <summary>SQL fragment + parameter binder for employee row filtering.</summary>
    public class EmployeeScopeFilter
    {
        public bool IsUnrestricted { get; set; }
        public string Sql { get; set; } = "";
        public Action<SqlCommand> Bind { get; set; } = _ => { };

        public void ApplyTo(SqlCommand cmd) => Bind(cmd);

        public static EmployeeScopeFilter Unrestricted() => new EmployeeScopeFilter { IsUnrestricted = true };
    }

    public class DataAccessScopeService
    {
        public const string AccessDeniedMessage =
            "You are not authorized to access this record.";

        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();
        private UserDataScopeSettings _cachedSettings;
        private int? _cachedLinkedEmployeeId;

        public DataAccessScopeService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public bool BypassesDataScope()
            => _auth.IsAdmin || GetSettings().Mode == DataScopeMode.All;

        public UserDataScopeSettings GetSettings(int? userId = null)
        {
            var uid = userId ?? _auth.CurrentUserId;
            if (!userId.HasValue && _cachedSettings != null)
                return _cachedSettings;

            var settings = new UserDataScopeSettings();
            if (!uid.HasValue || uid.Value <= 0)
                return settings;

            if (_auth.IsAdmin)
            {
                settings.Mode = DataScopeMode.All;
                if (!userId.HasValue) _cachedSettings = settings;
                return settings;
            }

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();

                using (var cmd = new SqlCommand(@"
                    SELECT ScopeMode, IncludeUnassignedDepartment, IncludeUnassignedLocation
                    FROM tblUserDataScope
                    WHERE UserID = @UserID AND IsActive = 1;", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", uid.Value);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            settings.Mode = ParseMode(dr.GetString(0));
                            settings.IncludeUnassignedDepartment = dr.GetBoolean(1);
                            settings.IncludeUnassignedLocation = dr.GetBoolean(2);
                        }
                    }
                }

                using (var deptCmd = new SqlCommand(@"
                    SELECT DepartmentID FROM tblUserDepartmentScope WHERE UserID = @UserID;", conn))
                {
                    deptCmd.Parameters.AddWithValue("@UserID", uid.Value);
                    using (var dr = deptCmd.ExecuteReader())
                    {
                        while (dr.Read())
                            settings.DepartmentIds.Add(dr.GetInt32(0));
                    }
                }

                using (var locCmd = new SqlCommand(@"
                    SELECT LocationID FROM tblUserLocationScope WHERE UserID = @UserID;", conn))
                {
                    locCmd.Parameters.AddWithValue("@UserID", uid.Value);
                    using (var dr = locCmd.ExecuteReader())
                    {
                        while (dr.Read())
                            settings.LocationIds.Add(dr.GetInt32(0));
                    }
                }
            }

            if (!userId.HasValue) _cachedSettings = settings;
            return settings;
        }

        public UserDataScopeSettings GetSettingsForUser(int userId) => GetSettings(userId);

        public void SaveUserDataScope(int userId, UserDataScopeSettings settings, int? savedByUserId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    using (var exists = new SqlCommand("SELECT COUNT(1) FROM tblUserDataScope WHERE UserID = @UserID;", conn, tx))
                    {
                        exists.Parameters.AddWithValue("@UserID", userId);
                        var hasRow = Convert.ToInt32(exists.ExecuteScalar()) > 0;
                        var mode = settings.Mode.ToString();

                        if (hasRow)
                        {
                            using (var upd = new SqlCommand(@"
                                UPDATE tblUserDataScope
                                SET ScopeMode = @ScopeMode,
                                    IncludeUnassignedDepartment = @IncDept,
                                    IncludeUnassignedLocation = @IncLoc,
                                    IsActive = 1,
                                    ModifiedOn = GETDATE(),
                                    ModifiedByUserID = @By
                                WHERE UserID = @UserID;", conn, tx))
                            {
                                upd.Parameters.AddWithValue("@UserID", userId);
                                upd.Parameters.AddWithValue("@ScopeMode", mode);
                                upd.Parameters.AddWithValue("@IncDept", settings.IncludeUnassignedDepartment);
                                upd.Parameters.AddWithValue("@IncLoc", settings.IncludeUnassignedLocation);
                                upd.Parameters.AddWithValue("@By", (object)savedByUserId ?? DBNull.Value);
                                upd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (var ins = new SqlCommand(@"
                                INSERT INTO tblUserDataScope
                                    (UserID, ScopeMode, IncludeUnassignedDepartment, IncludeUnassignedLocation,
                                     IsActive, CreatedOn, CreatedByUserID)
                                VALUES
                                    (@UserID, @ScopeMode, @IncDept, @IncLoc, 1, GETDATE(), @By);", conn, tx))
                            {
                                ins.Parameters.AddWithValue("@UserID", userId);
                                ins.Parameters.AddWithValue("@ScopeMode", mode);
                                ins.Parameters.AddWithValue("@IncDept", settings.IncludeUnassignedDepartment);
                                ins.Parameters.AddWithValue("@IncLoc", settings.IncludeUnassignedLocation);
                                ins.Parameters.AddWithValue("@By", (object)savedByUserId ?? DBNull.Value);
                                ins.ExecuteNonQuery();
                            }
                        }
                    }

                    using (var delDept = new SqlCommand("DELETE FROM tblUserDepartmentScope WHERE UserID = @UserID;", conn, tx))
                    {
                        delDept.Parameters.AddWithValue("@UserID", userId);
                        delDept.ExecuteNonQuery();
                    }
                    foreach (var deptId in settings.DepartmentIds.Distinct().Where(id => id > 0))
                    {
                        using (var ins = new SqlCommand(@"
                            INSERT INTO tblUserDepartmentScope (UserID, DepartmentID, CreatedOn)
                            VALUES (@UserID, @DepartmentID, GETDATE());", conn, tx))
                        {
                            ins.Parameters.AddWithValue("@UserID", userId);
                            ins.Parameters.AddWithValue("@DepartmentID", deptId);
                            ins.ExecuteNonQuery();
                        }
                    }

                    using (var delLoc = new SqlCommand("DELETE FROM tblUserLocationScope WHERE UserID = @UserID;", conn, tx))
                    {
                        delLoc.Parameters.AddWithValue("@UserID", userId);
                        delLoc.ExecuteNonQuery();
                    }
                    foreach (var locId in settings.LocationIds.Distinct().Where(id => id > 0))
                    {
                        using (var ins = new SqlCommand(@"
                            INSERT INTO tblUserLocationScope (UserID, LocationID, CreatedOn)
                            VALUES (@UserID, @LocationID, GETDATE());", conn, tx))
                        {
                            ins.Parameters.AddWithValue("@UserID", userId);
                            ins.Parameters.AddWithValue("@LocationID", locId);
                            ins.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }

            _cachedSettings = null;
        }

        public int? GetLinkedEmployeeId()
        {
            if (_cachedLinkedEmployeeId.HasValue)
                return _cachedLinkedEmployeeId;

            if (_auth.LinkedEmployeeId is int sessionId && sessionId > 0)
            {
                _cachedLinkedEmployeeId = sessionId;
                return sessionId;
            }

            if (!_auth.CurrentUserId.HasValue)
                return null;

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
                SELECT TOP 1 EmployeeID FROM tblEmployee
                WHERE UserID = @UserID ORDER BY EmployeeID;", conn))
            {
                cmd.Parameters.AddWithValue("@UserID", _auth.CurrentUserId.Value);
                conn.Open();
                var result = cmd.ExecuteScalar();
                _cachedLinkedEmployeeId = result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
                return _cachedLinkedEmployeeId;
            }
        }

        /// <summary>Build AND-clause for employee table alias (e.g. "e").</summary>
        public EmployeeScopeFilter GetEmployeeFilter(string alias = "e")
        {
            if (_auth.IsAdmin)
                return EmployeeScopeFilter.Unrestricted();

            var settings = GetSettings();
            if (settings.Mode == DataScopeMode.All)
                return EmployeeScopeFilter.Unrestricted();

            if (settings.Mode == DataScopeMode.OwnOnly)
                return BuildOwnOnlyFilter(alias);

            return BuildRestrictedFilter(alias, settings);
        }

        public bool CanAccessEmployee(int employeeId)
        {
            if (employeeId <= 0) return false;
            if (BypassesDataScope()) return true;

            var filter = GetEmployeeFilter("e");
            if (filter.IsUnrestricted) return true;

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand($@"
                SELECT COUNT(1) FROM tblEmployee e
                WHERE e.EmployeeID = @EmployeeID {filter.Sql};", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                filter.ApplyTo(cmd);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public bool CanAccessEmployeeRecord(int employeeId)
            => CanAccessEmployee(employeeId);

        public string GetScopeDescription()
        {
            if (_auth.IsAdmin) return "Administrator — all data";
            var s = GetSettings();
            switch (s.Mode)
            {
                case DataScopeMode.All: return "All employee data";
                case DataScopeMode.OwnOnly: return "Own employee record only";
                case DataScopeMode.Restricted: return BuildRestrictedDescription(s);
                default: return "Own employee record only";
            }
        }

        private static string BuildRestrictedDescription(UserDataScopeSettings s)
        {
            var parts = new List<string>();
            if (s.DepartmentIds.Count > 0)
                parts.Add($"{s.DepartmentIds.Count} department(s)");
            if (s.LocationIds.Count > 0)
                parts.Add($"{s.LocationIds.Count} location(s)");
            if (parts.Count == 0)
                return "Restricted (own record — no assignments)";
            var combo = s.DepartmentIds.Count > 0 && s.LocationIds.Count > 0 ? "dept + location" : "assigned scope";
            return $"Restricted — {string.Join(", ", parts)} ({combo})";
        }

        private EmployeeScopeFilter BuildOwnOnlyFilter(string alias)
        {
            var linked = GetLinkedEmployeeId();
            if (!linked.HasValue || linked.Value <= 0)
            {
                return new EmployeeScopeFilter
                {
                    Sql = " AND 1 = 0 ",
                    Bind = _ => { }
                };
            }

            var empId = linked.Value;
            return new EmployeeScopeFilter
            {
                Sql = $" AND {alias}.EmployeeID = @ScopeOwnEmployeeId ",
                Bind = cmd => cmd.Parameters.AddWithValue("@ScopeOwnEmployeeId", empId)
            };
        }

        private EmployeeScopeFilter BuildRestrictedFilter(string alias, UserDataScopeSettings settings)
        {
            var clauses = new List<string>();
            var binders = new List<Action<SqlCommand>>();
            var hasDept = settings.DepartmentIds.Count > 0;
            var hasLoc = settings.LocationIds.Count > 0;

            if (!hasDept && !hasLoc)
                return BuildOwnOnlyFilter(alias);

            if (hasDept)
            {
                var deptClause = BuildInClause(alias, "DepartmentID", settings.DepartmentIds, "ScopeDept", out var deptBind);
                if (settings.IncludeUnassignedDepartment)
                    deptClause = $"({deptClause} OR {alias}.DepartmentID IS NULL)";
                clauses.Add(deptClause);
                binders.Add(deptBind);
            }

            if (hasLoc)
            {
                var locClause = BuildInClause(alias, "LocationID", settings.LocationIds, "ScopeLoc", out var locBind);
                if (settings.IncludeUnassignedLocation)
                    locClause = $"({locClause} OR {alias}.LocationID IS NULL)";
                clauses.Add(locClause);
                binders.Add(locBind);
            }

            var sql = " AND " + string.Join(" AND ", clauses);
            return new EmployeeScopeFilter
            {
                Sql = sql,
                Bind = cmd => binders.ForEach(b => b(cmd))
            };
        }

        private static string BuildInClause(
            string alias, string column, List<int> ids, string paramPrefix,
            out Action<SqlCommand> binder)
        {
            var names = new List<string>();
            var captured = new List<(string Name, int Value)>();
            for (var i = 0; i < ids.Count; i++)
            {
                var p = $"@{paramPrefix}{i}";
                names.Add(p);
                captured.Add((p, ids[i]));
            }
            binder = cmd =>
            {
                foreach (var item in captured)
                    cmd.Parameters.AddWithValue(item.Name, item.Value);
            };
            return $"{alias}.{column} IN ({string.Join(", ", names)})";
        }

        private static DataScopeMode ParseMode(string value)
        {
            switch (value?.ToLowerInvariant())
            {
                case "all": return DataScopeMode.All;
                case "restricted": return DataScopeMode.Restricted;
                default: return DataScopeMode.OwnOnly;
            }
        }

        public void EnsureDefaultScope(int userId)
        {
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM tblUserDataScope WHERE UserID = @UserID)
                INSERT INTO tblUserDataScope
                    (UserID, ScopeMode, IncludeUnassignedDepartment, IncludeUnassignedLocation, IsActive, CreatedOn)
                VALUES (@UserID, 'OwnOnly', 0, 0, 1, GETDATE());", conn))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
