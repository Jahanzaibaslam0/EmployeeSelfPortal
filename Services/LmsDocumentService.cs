using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace HRMS.Services
{
    public static class LmsCategories
    {
        public static readonly string[] All =
        {
            "General",
            "Department",
            "SystemManual",
            "SOP",
            "Policy",
            "Reference"
        };

        public static string DisplayName(string category)
        {
            if (string.Equals(category, "SystemManual", StringComparison.OrdinalIgnoreCase))
                return "System Manuals";
            if (string.Equals(category, "SOP", StringComparison.OrdinalIgnoreCase))
                return "SOPs";
            if (string.Equals(category, "Department", StringComparison.OrdinalIgnoreCase))
                return "Department Documents";
            if (string.Equals(category, "General", StringComparison.OrdinalIgnoreCase))
                return "General Documents";
            if (string.Equals(category, "Policy", StringComparison.OrdinalIgnoreCase))
                return "Policies";
            if (string.Equals(category, "Reference", StringComparison.OrdinalIgnoreCase))
                return "Reference Documents";
            return category ?? "";
        }
    }

    public static class LmsAccessScopes
    {
        public const string Organization = "Organization";
        public const string Department = "Department";
        public const string Job = "Job";
        public const string Restricted = "Restricted";

        public static readonly string[] All =
        {
            Organization, Department, Job, Restricted
        };

        public static string DisplayName(string scope)
        {
            if (string.Equals(scope, Organization, StringComparison.OrdinalIgnoreCase))
                return "Organization-wide (all employees)";
            if (string.Equals(scope, Department, StringComparison.OrdinalIgnoreCase))
                return "Department";
            if (string.Equals(scope, Job, StringComparison.OrdinalIgnoreCase))
                return "Job / Role";
            if (string.Equals(scope, Restricted, StringComparison.OrdinalIgnoreCase))
                return "Restricted (explicit grants only)";
            return scope ?? "";
        }
    }

    public class LmsDocumentItem
    {
        public int DocumentID { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "General";
        public string CategoryDisplay => LmsCategories.DisplayName(Category);
        public string AccessScope { get; set; } = LmsAccessScopes.Organization;
        public string AccessScopeDisplay => LmsAccessScopes.DisplayName(AccessScope);
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = "";
        public int JobID { get; set; }
        public string JobTitle { get; set; } = "";
        public string DocumentPath { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public string VersionLabel { get; set; } = "";
        public string EffectiveDate { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public bool HasFile => !string.IsNullOrWhiteSpace(DocumentPath);
    }

    public class LmsAccessGrant
    {
        public int AccessID { get; set; }
        public string GrantType { get; set; } = "";
        public int EmployeeID { get; set; }
        public int DepartmentID { get; set; }
        public int JobID { get; set; }
        public string DisplayName { get; set; } = "";
    }

    public class LmsViewerContext
    {
        public bool IsAdmin { get; set; }
        public bool CanManage { get; set; }
        public int? EmployeeID { get; set; }
        public int DepartmentID { get; set; }
        public int JobID { get; set; }
    }

    /// <summary>LMS knowledge documents – CRUD helpers and RBAC visibility.</summary>
    public class LmsDocumentService
    {
        public const string LibraryFormKey = "LmsLibrary";
        public const string SetupFormKey = "LmsDocumentSetup";

        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();
        private readonly PermissionService _perms = new PermissionService();
        private readonly EmployeeProfileAccessService _profile = new EmployeeProfileAccessService();

        public LmsDocumentService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public void EnsureSchema()
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'tblLmsDocument', N'U') IS NULL
BEGIN
    CREATE TABLE tblLmsDocument (
        DocumentID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        Category NVARCHAR(50) NOT NULL,
        AccessScope NVARCHAR(50) NOT NULL DEFAULT N'Organization',
        DepartmentID INT NULL,
        JobID INT NULL,
        DocumentPath NVARCHAR(500) NULL,
        OriginalFileName NVARCHAR(255) NULL,
        VersionLabel NVARCHAR(50) NULL,
        EffectiveDate DATE NULL,
        ExpiryDate DATE NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL,
        CreatedByUserID INT NULL,
        ModifiedByUserID INT NULL
    );
END
IF OBJECT_ID(N'tblLmsDocumentAccess', N'U') IS NULL
BEGIN
    CREATE TABLE tblLmsDocumentAccess (
        AccessID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DocumentID INT NOT NULL REFERENCES tblLmsDocument(DocumentID),
        GrantType NVARCHAR(50) NOT NULL,
        EmployeeID INT NULL,
        DepartmentID INT NULL,
        JobID INT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedByUserID INT NULL
    );
END;", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public LmsViewerContext GetViewerContext()
        {
            var ctx = new LmsViewerContext
            {
                IsAdmin = _auth.IsAdmin,
                CanManage = _auth.IsAdmin || _perms.CanWrite(SetupFormKey) || _perms.CanRead(SetupFormKey)
            };

            var empId = _profile.GetLinkedEmployeeId();
            if (empId.HasValue && empId.Value > 0)
            {
                ctx.EmployeeID = empId.Value;
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT ISNULL(DepartmentID, 0), ISNULL(JobID, 0)
FROM tblEmployee WHERE EmployeeID = @EmployeeID;", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", empId.Value);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            ctx.DepartmentID = Convert.ToInt32(dr[0]);
                            ctx.JobID = Convert.ToInt32(dr[1]);
                        }
                    }
                }
            }

            return ctx;
        }

        public bool CanAccessLibrary()
            => _auth.IsAdmin
               || _perms.CanRead(LibraryFormKey)
               || _profile.IsEmployeeProfileSynchronized()
               || _perms.CanRead(SetupFormKey);

        public bool CanManageDocuments()
            => _auth.IsAdmin || _perms.CanWrite(SetupFormKey);

        public bool CanViewDocument(int documentId)
        {
            var ctx = GetViewerContext();
            if (ctx.IsAdmin || ctx.CanManage) return true;
            if (!ctx.EmployeeID.HasValue) return false;

            using (var conn = new SqlConnection(_conn))
            using (var cmd = BuildVisibilityCommand(conn, documentId, null, ctx, activeOnly: true))
            {
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value;
            }
        }

        public List<LmsDocumentItem> ListVisibleDocuments(string categoryFilter = null, string search = null)
        {
            EnsureSchema();
            var ctx = GetViewerContext();
            var list = new List<LmsDocumentItem>();

            using (var conn = new SqlConnection(_conn))
            using (var cmd = BuildVisibilityCommand(conn, null, categoryFilter, ctx, activeOnly: true))
            {
                if (!string.IsNullOrWhiteSpace(search))
                {
                    cmd.CommandText = cmd.CommandText.Replace(
                        "ORDER BY",
                        "AND (d.Title LIKE @Search OR ISNULL(d.Description,'') LIKE @Search OR ISNULL(d.OriginalFileName,'') LIKE @Search) ORDER BY");
                    cmd.Parameters.AddWithValue("@Search", "%" + search.Trim() + "%");
                }

                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        list.Add(ReadDocument(dr));
                }
            }

            return list;
        }

        public List<LmsDocumentItem> ListAllForSetup()
        {
            EnsureSchema();
            var list = new List<LmsDocumentItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT d.DocumentID, d.Title, d.Description, d.Category, d.AccessScope,
       ISNULL(d.DepartmentID, 0) AS DepartmentID, ISNULL(dept.DepartmentName, '') AS DepartmentName,
       ISNULL(d.JobID, 0) AS JobID, ISNULL(j.JobTitle, '') AS JobTitle,
       d.DocumentPath, d.OriginalFileName, d.VersionLabel,
       d.EffectiveDate, d.ExpiryDate, d.IsActive
FROM tblLmsDocument d
LEFT JOIN tblDepartment dept ON dept.DepartmentID = d.DepartmentID
LEFT JOIN tblJob j ON j.JobID = d.JobID
ORDER BY d.IsActive DESC, d.Category, d.Title;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        list.Add(ReadDocument(dr));
                }
            }
            return list;
        }

        public LmsDocumentItem GetById(int documentId, bool forSetup)
        {
            EnsureSchema();
            var ctx = GetViewerContext();
            if (!forSetup && !CanViewDocument(documentId))
                return null;

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT d.DocumentID, d.Title, d.Description, d.Category, d.AccessScope,
       ISNULL(d.DepartmentID, 0) AS DepartmentID, ISNULL(dept.DepartmentName, '') AS DepartmentName,
       ISNULL(d.JobID, 0) AS JobID, ISNULL(j.JobTitle, '') AS JobTitle,
       d.DocumentPath, d.OriginalFileName, d.VersionLabel,
       d.EffectiveDate, d.ExpiryDate, d.IsActive
FROM tblLmsDocument d
LEFT JOIN tblDepartment dept ON dept.DepartmentID = d.DepartmentID
LEFT JOIN tblJob j ON j.JobID = d.JobID
WHERE d.DocumentID = @DocumentID;", conn))
            {
                cmd.Parameters.AddWithValue("@DocumentID", documentId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    return dr.Read() ? ReadDocument(dr) : null;
            }
        }

        public List<LmsAccessGrant> GetAccessGrants(int documentId)
        {
            var list = new List<LmsAccessGrant>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT a.AccessID, a.GrantType,
       ISNULL(a.EmployeeID, 0) AS EmployeeID,
       ISNULL(a.DepartmentID, 0) AS DepartmentID,
       ISNULL(a.JobID, 0) AS JobID,
       CASE a.GrantType
         WHEN 'Employee' THEN ISNULL(e.EmployeeCode,'') + ' – ' + LTRIM(RTRIM(ISNULL(e.FirstName,'') + ' ' + ISNULL(e.LastName,'')))
         WHEN 'Department' THEN ISNULL(d.DepartmentName, '')
         WHEN 'Job' THEN ISNULL(j.JobTitle, '')
         ELSE a.GrantType
       END AS DisplayName
FROM tblLmsDocumentAccess a
LEFT JOIN tblEmployee e ON e.EmployeeID = a.EmployeeID
LEFT JOIN tblDepartment d ON d.DepartmentID = a.DepartmentID
LEFT JOIN tblJob j ON j.JobID = a.JobID
WHERE a.DocumentID = @DocumentID AND a.IsActive = 1
ORDER BY a.GrantType, DisplayName;", conn))
            {
                cmd.Parameters.AddWithValue("@DocumentID", documentId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new LmsAccessGrant
                        {
                            AccessID = Convert.ToInt32(dr["AccessID"]),
                            GrantType = dr["GrantType"].ToString() ?? "",
                            EmployeeID = Convert.ToInt32(dr["EmployeeID"]),
                            DepartmentID = Convert.ToInt32(dr["DepartmentID"]),
                            JobID = Convert.ToInt32(dr["JobID"]),
                            DisplayName = dr["DisplayName"]?.ToString()?.Trim() ?? ""
                        });
                    }
                }
            }
            return list;
        }

        public int SaveDocument(LmsDocumentItem item, int? userId)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                if (item.DocumentID > 0)
                {
                    using (var cmd = new SqlCommand(@"
UPDATE tblLmsDocument SET
  Title=@Title, Description=@Description, Category=@Category, AccessScope=@AccessScope,
  DepartmentID=@DepartmentID, JobID=@JobID,
  DocumentPath=@DocumentPath, OriginalFileName=@OriginalFileName, VersionLabel=@VersionLabel,
  EffectiveDate=@EffectiveDate, ExpiryDate=@ExpiryDate, IsActive=@IsActive,
  ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
WHERE DocumentID=@DocumentID;", conn))
                    {
                        AddDocParams(cmd, item);
                        cmd.Parameters.AddWithValue("@ModifiedByUserID", userId.HasValue ? (object)userId.Value : DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                    return item.DocumentID;
                }

                using (var cmd = new SqlCommand(@"
INSERT INTO tblLmsDocument
 (Title, Description, Category, AccessScope, DepartmentID, JobID,
  DocumentPath, OriginalFileName, VersionLabel, EffectiveDate, ExpiryDate, IsActive,
  CreatedOn, CreatedByUserID)
VALUES
 (@Title, @Description, @Category, @AccessScope, @DepartmentID, @JobID,
  @DocumentPath, @OriginalFileName, @VersionLabel, @EffectiveDate, @ExpiryDate, @IsActive,
  GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
                {
                    AddDocParams(cmd, item);
                    cmd.Parameters.AddWithValue("@CreatedByUserID", userId.HasValue ? (object)userId.Value : DBNull.Value);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void SoftDelete(int documentId, int? userId)
        {
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
UPDATE tblLmsDocument
SET IsActive=0, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
WHERE DocumentID=@DocumentID;", conn))
            {
                cmd.Parameters.AddWithValue("@DocumentID", documentId);
                cmd.Parameters.AddWithValue("@ModifiedByUserID", userId.HasValue ? (object)userId.Value : DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void ReplaceAccessGrants(int documentId, IEnumerable<LmsAccessGrant> grants, int? userId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var del = new SqlCommand("DELETE FROM tblLmsDocumentAccess WHERE DocumentID=@DocumentID;", conn))
                {
                    del.Parameters.AddWithValue("@DocumentID", documentId);
                    del.ExecuteNonQuery();
                }

                foreach (var g in grants ?? Enumerable.Empty<LmsAccessGrant>())
                {
                    if (string.IsNullOrWhiteSpace(g.GrantType)) continue;
                    using (var ins = new SqlCommand(@"
INSERT INTO tblLmsDocumentAccess
 (DocumentID, GrantType, EmployeeID, DepartmentID, JobID, IsActive, CreatedOn, CreatedByUserID)
VALUES
 (@DocumentID, @GrantType, @EmployeeID, @DepartmentID, @JobID, 1, GETDATE(), @CreatedByUserID);", conn))
                    {
                        ins.Parameters.AddWithValue("@DocumentID", documentId);
                        ins.Parameters.AddWithValue("@GrantType", g.GrantType.Trim());
                        ins.Parameters.AddWithValue("@EmployeeID", g.EmployeeID > 0 ? (object)g.EmployeeID : DBNull.Value);
                        ins.Parameters.AddWithValue("@DepartmentID", g.DepartmentID > 0 ? (object)g.DepartmentID : DBNull.Value);
                        ins.Parameters.AddWithValue("@JobID", g.JobID > 0 ? (object)g.JobID : DBNull.Value);
                        ins.Parameters.AddWithValue("@CreatedByUserID", userId.HasValue ? (object)userId.Value : DBNull.Value);
                        ins.ExecuteNonQuery();
                    }
                }
            }
        }

        private static SqlCommand BuildVisibilityCommand(
            SqlConnection conn,
            int? documentId,
            string categoryFilter,
            LmsViewerContext ctx,
            bool activeOnly)
        {
            var sql = @"
SELECT d.DocumentID, d.Title, d.Description, d.Category, d.AccessScope,
       ISNULL(d.DepartmentID, 0) AS DepartmentID, ISNULL(dept.DepartmentName, '') AS DepartmentName,
       ISNULL(d.JobID, 0) AS JobID, ISNULL(j.JobTitle, '') AS JobTitle,
       d.DocumentPath, d.OriginalFileName, d.VersionLabel,
       d.EffectiveDate, d.ExpiryDate, d.IsActive
FROM tblLmsDocument d
LEFT JOIN tblDepartment dept ON dept.DepartmentID = d.DepartmentID
LEFT JOIN tblJob j ON j.JobID = d.JobID
WHERE 1=1";

            if (activeOnly)
            {
                sql += @"
  AND d.IsActive = 1
  AND (d.EffectiveDate IS NULL OR d.EffectiveDate <= CAST(GETDATE() AS DATE))
  AND (d.ExpiryDate IS NULL OR d.ExpiryDate >= CAST(GETDATE() AS DATE))";
            }

            if (documentId.HasValue)
                sql += " AND d.DocumentID = @DocumentID";

            if (!string.IsNullOrWhiteSpace(categoryFilter))
                sql += " AND d.Category = @Category";

            if (!ctx.IsAdmin && !ctx.CanManage)
            {
                sql += @"
  AND (
        d.AccessScope = N'Organization'
     OR (d.AccessScope = N'Department' AND @DeptID > 0 AND d.DepartmentID = @DeptID)
     OR (d.AccessScope = N'Job' AND @JobID > 0 AND d.JobID = @JobID)
     OR EXISTS (
            SELECT 1 FROM tblLmsDocumentAccess a
            WHERE a.DocumentID = d.DocumentID AND a.IsActive = 1
              AND (
                    (a.GrantType = N'Employee' AND a.EmployeeID = @EmpID)
                 OR (a.GrantType = N'Department' AND @DeptID > 0 AND a.DepartmentID = @DeptID)
                 OR (a.GrantType = N'Job' AND @JobID > 0 AND a.JobID = @JobID)
              )
        )
  )";
            }

            sql += " ORDER BY d.Category, d.Title;";

            var cmd = new SqlCommand(sql, conn);
            if (documentId.HasValue)
                cmd.Parameters.AddWithValue("@DocumentID", documentId.Value);
            if (!string.IsNullOrWhiteSpace(categoryFilter))
                cmd.Parameters.AddWithValue("@Category", categoryFilter.Trim());
            cmd.Parameters.AddWithValue("@EmpID", ctx.EmployeeID ?? 0);
            cmd.Parameters.AddWithValue("@DeptID", ctx.DepartmentID);
            cmd.Parameters.AddWithValue("@JobID", ctx.JobID);
            return cmd;
        }

        private static void AddDocParams(SqlCommand cmd, LmsDocumentItem item)
        {
            cmd.Parameters.AddWithValue("@DocumentID", item.DocumentID);
            cmd.Parameters.AddWithValue("@Title", item.Title.Trim());
            cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(item.Description) ? (object)DBNull.Value : item.Description.Trim());
            cmd.Parameters.AddWithValue("@Category", string.IsNullOrWhiteSpace(item.Category) ? "General" : item.Category.Trim());
            cmd.Parameters.AddWithValue("@AccessScope", string.IsNullOrWhiteSpace(item.AccessScope) ? LmsAccessScopes.Organization : item.AccessScope.Trim());
            cmd.Parameters.AddWithValue("@DepartmentID", item.DepartmentID > 0 ? (object)item.DepartmentID : DBNull.Value);
            cmd.Parameters.AddWithValue("@JobID", item.JobID > 0 ? (object)item.JobID : DBNull.Value);
            cmd.Parameters.AddWithValue("@DocumentPath", string.IsNullOrWhiteSpace(item.DocumentPath) ? (object)DBNull.Value : item.DocumentPath);
            cmd.Parameters.AddWithValue("@OriginalFileName", string.IsNullOrWhiteSpace(item.OriginalFileName) ? (object)DBNull.Value : item.OriginalFileName);
            cmd.Parameters.AddWithValue("@VersionLabel", string.IsNullOrWhiteSpace(item.VersionLabel) ? (object)DBNull.Value : item.VersionLabel.Trim());
            cmd.Parameters.AddWithValue("@EffectiveDate", ParseDateOrDbNull(item.EffectiveDate));
            cmd.Parameters.AddWithValue("@ExpiryDate", ParseDateOrDbNull(item.ExpiryDate));
            cmd.Parameters.AddWithValue("@IsActive", item.IsActive);
        }

        private static object ParseDateOrDbNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
            return DateTime.TryParse(value, out var dt) ? (object)dt.Date : DBNull.Value;
        }

        private static LmsDocumentItem ReadDocument(SqlDataReader dr)
        {
            return new LmsDocumentItem
            {
                DocumentID = Convert.ToInt32(dr["DocumentID"]),
                Title = dr["Title"]?.ToString() ?? "",
                Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
                Category = dr["Category"]?.ToString() ?? "General",
                AccessScope = dr["AccessScope"]?.ToString() ?? LmsAccessScopes.Organization,
                DepartmentID = Convert.ToInt32(dr["DepartmentID"]),
                DepartmentName = dr["DepartmentName"]?.ToString() ?? "",
                JobID = Convert.ToInt32(dr["JobID"]),
                JobTitle = dr["JobTitle"]?.ToString() ?? "",
                DocumentPath = dr["DocumentPath"] == DBNull.Value ? "" : dr["DocumentPath"].ToString() ?? "",
                OriginalFileName = dr["OriginalFileName"] == DBNull.Value ? "" : dr["OriginalFileName"].ToString() ?? "",
                VersionLabel = dr["VersionLabel"] == DBNull.Value ? "" : dr["VersionLabel"].ToString() ?? "",
                EffectiveDate = dr["EffectiveDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["EffectiveDate"]).ToString("yyyy-MM-dd"),
                ExpiryDate = dr["ExpiryDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["ExpiryDate"]).ToString("yyyy-MM-dd"),
                IsActive = Convert.ToBoolean(dr["IsActive"])
            };
        }
    }
}
