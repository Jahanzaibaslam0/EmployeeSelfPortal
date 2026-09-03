using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace HRMS.Services
{
    public class EmployeeProfileAccessService
    {
        public const string UserProfileFormKey = "UserProfile";
        public const string EmployeeMasterFormKey = "EmployeeMaster";
        public const string MyDocumentsFormKey = "MyDocuments";
        public const string NotSynchronizedMessage =
            "Your user account is not linked to an employee profile. Please contact HR or system administrator.";

        private static readonly string[] EmployeeRelatedForms =
        {
            UserProfileFormKey,
            EmployeeMasterFormKey,
            MyDocumentsFormKey,
            "PerformanceMaster",
            "TrainingMaster",
            "ExpenseMaster",
            "RecruitmentMaster",
            "EmployeeReport"
        };

        private readonly AuthService _auth = new AuthService();
        private readonly PermissionService _perms = new PermissionService();
        private readonly string _conn;

        public EmployeeProfileAccessService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public bool HasFullEmployeeMasterAccess()
            => _auth.IsAdmin || _perms.CanRead(EmployeeMasterFormKey);

        public bool IsProfileOnlyUser()
            => !HasFullEmployeeMasterAccess() && IsEmployeeProfileSynchronized();

        public bool IsEmployeeProfileSynchronized()
            => GetLinkedEmployeeId() is int id && id > 0;

        public bool IsEmployeeRelatedForm(string formKey)
            => EmployeeRelatedForms.Any(f => f.Equals(formKey, StringComparison.OrdinalIgnoreCase));

        public bool CanAccessEmployeeMasterPage()
            => HasFullEmployeeMasterAccess() || IsEmployeeProfileSynchronized();

        /// <summary>
        /// My Documents page access: system administrators, or any user linked to an employee profile.
        /// </summary>
        public bool CanAccessMyDocuments()
            => _auth.IsAdmin || IsEmployeeProfileSynchronized();

        /// <summary>
        /// Only system administrators may browse every employee's documents.
        /// Non-admin users (including those with Employee Master rights) are limited to their own linked employee.
        /// </summary>
        public bool CanViewAllEmployeeDocuments()
            => _auth.IsAdmin;

        public bool CanViewEmployeeList()
            => HasFullEmployeeMasterAccess();

        public bool CanCreateEmployee()
            => HasFullEmployeeMasterAccess() && _perms.CanWrite(EmployeeMasterFormKey);

        public bool CanDeleteEmployee()
            => HasFullEmployeeMasterAccess() && _perms.CanDelete(EmployeeMasterFormKey);

        public bool CanViewEmployee(int employeeId)
            => HasFullEmployeeMasterAccess() || OwnsEmployee(employeeId);

        public bool CanEditEmployee(int employeeId)
        {
            if (HasFullEmployeeMasterAccess())
                return _perms.CanWrite(EmployeeMasterFormKey);
            return OwnsEmployee(employeeId) && _perms.CanWrite(UserProfileFormKey);
        }

        public bool OwnsEmployee(int employeeId)
        {
            var linked = GetLinkedEmployeeId();
            return linked.HasValue && linked.Value == employeeId;
        }

        public int? GetLinkedEmployeeId()
        {
            if (_auth.LinkedEmployeeId is int sessionId && sessionId > 0)
                return sessionId;

            if (!_auth.CurrentUserId.HasValue)
                return null;

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
                return result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
            }
        }
    }
}
