using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace HRMS.Services
{
    public static class DisciplineStatuses
    {
        public const string Open = "Open";
        public const string UnderInvestigation = "UnderInvestigation";
        public const string ActionRequired = "ActionRequired";
        public const string Resolved = "Resolved";
        public const string Closed = "Closed";

        public static readonly string[] All =
        {
            Open, UnderInvestigation, ActionRequired, Resolved, Closed
        };

        public static string DisplayName(string status)
        {
            switch (status)
            {
                case UnderInvestigation: return "Under Investigation";
                case ActionRequired: return "Action Required";
                default: return status ?? "";
            }
        }

        public static string BadgeClass(string status)
        {
            switch (status)
            {
                case Closed:
                case Resolved: return "disc-badge disc-closed";
                case ActionRequired: return "disc-badge disc-action";
                case UnderInvestigation: return "disc-badge disc-invest";
                default: return "disc-badge disc-open";
            }
        }
    }

    public static class DisciplineTypes
    {
        public static readonly string[] All =
        {
            "Misconduct", "Non-Conformance", "Attendance", "Conduct", "Policy Violation",
            "Performance Related", "Safety", "Other"
        };
    }

    public static class DisciplineCategories
    {
        public static readonly string[] All =
        {
            "Verbal Warning", "Written Warning", "Show Cause", "Suspension",
            "Penalty", "Termination Recommendation", "Counseling", "Process Deviation", "Other"
        };
    }

    public static class FindingSources
    {
        public static readonly string[] All =
        {
            "Disciplinary Case", "Non-Conformance", "Investigation", "Attendance/Conduct",
            "Performance Evaluation", "Other Incident"
        };
    }

    public class DisciplineCaseItem
    {
        public int DisciplineID { get; set; }
        public string CaseNumber { get; set; } = "";
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string Designation { get; set; } = "";
        public string ManagerName { get; set; } = "";
        public string IncidentDate { get; set; } = "";
        public string ReportedDate { get; set; } = "";
        public string DisciplineType { get; set; } = "";
        public string DisciplineCategory { get; set; } = "";
        public string Description { get; set; } = "";
        public string InvestigationDetails { get; set; } = "";
        public string InvestigationFindings { get; set; } = "";
        public string EmployeeExplanation { get; set; } = "";
        public string FindingsObservations { get; set; } = "";
        public string FindingRemarks { get; set; } = "";
        public string CorrectiveActions { get; set; } = "";
        public string DisciplinaryAction { get; set; } = "";
        public string ActionDate { get; set; } = "";
        public string ActionResponsiblePerson { get; set; } = "";
        public string CaseStatus { get; set; } = DisciplineStatuses.Open;
        public string StatusDisplay => DisciplineStatuses.DisplayName(CaseStatus);
        public string StatusBadgeClass => DisciplineStatuses.BadgeClass(CaseStatus);
        public string FinalDecision { get; set; } = "";
        public string ManagementRemarks { get; set; } = "";
        public bool IncludeInPerformance { get; set; } = true;
        public DateTime CreatedOn { get; set; }
        public DateTime? ClosedOn { get; set; }
    }

    public class DisciplineAttachment
    {
        public int AttachmentID { get; set; }
        public string DocumentPath { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public DateTime UploadedOn { get; set; }
    }

    public class DisciplineAuditItem
    {
        public int AuditID { get; set; }
        public string ActionType { get; set; } = "";
        public string ActionSummary { get; set; } = "";
        public string Remarks { get; set; } = "";
        public string ActionByName { get; set; } = "";
        public DateTime ActionOn { get; set; }
    }

    public class DisciplineFindingItem
    {
        public int FindingID { get; set; }
        public int EmployeeID { get; set; }
        public int? DisciplineID { get; set; }
        public string CaseNumber { get; set; } = "";
        public string FindingSource { get; set; } = "";
        public string FindingCategory { get; set; } = "";
        public string Title { get; set; } = "";
        public string Remarks { get; set; } = "";
        public string FindingDate { get; set; } = "";
        public string Severity { get; set; } = "";
        public bool IncludeInPerformance { get; set; } = true;
        public string CreatedByName { get; set; } = "";
        public DateTime CreatedOn { get; set; }
    }

    public class DisciplineService
    {
        public const string FormKey = "EmployeeDiscipline";

        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();
        private readonly PermissionService _perms = new PermissionService();
        private readonly DataAccessScopeService _scope = new DataAccessScopeService();
        private readonly EmployeeRequestService _requestSvc = new EmployeeRequestService();
        private readonly AppInboxService _inbox = new AppInboxService();

        public DisciplineService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public void EnsureSchema()
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'tblEmployeeDiscipline', N'U') IS NULL
BEGIN
    CREATE TABLE tblEmployeeDiscipline (
        DisciplineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CaseNumber NVARCHAR(30) NOT NULL,
        EmployeeID INT NOT NULL,
        IncidentDate DATE NULL,
        ReportedDate DATE NULL,
        DisciplineType NVARCHAR(100) NOT NULL,
        DisciplineCategory NVARCHAR(100) NULL,
        Description NVARCHAR(MAX) NULL,
        InvestigationDetails NVARCHAR(MAX) NULL,
        InvestigationFindings NVARCHAR(MAX) NULL,
        EmployeeExplanation NVARCHAR(MAX) NULL,
        FindingsObservations NVARCHAR(MAX) NULL,
        FindingRemarks NVARCHAR(MAX) NULL,
        CorrectiveActions NVARCHAR(MAX) NULL,
        DisciplinaryAction NVARCHAR(200) NULL,
        ActionDate DATE NULL,
        ActionResponsiblePerson NVARCHAR(200) NULL,
        CaseStatus NVARCHAR(50) NOT NULL DEFAULT N'Open',
        FinalDecision NVARCHAR(MAX) NULL,
        ManagementRemarks NVARCHAR(MAX) NULL,
        IncludeInPerformance BIT NOT NULL DEFAULT 1,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL,
        CreatedByUserID INT NULL,
        ModifiedByUserID INT NULL,
        ClosedOn DATETIME NULL,
        ClosedByUserID INT NULL
    );
END
IF OBJECT_ID(N'tblEmployeeDisciplineAttachment', N'U') IS NULL
CREATE TABLE tblEmployeeDisciplineAttachment (
    AttachmentID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DisciplineID INT NOT NULL REFERENCES tblEmployeeDiscipline(DisciplineID),
    DocumentPath NVARCHAR(500) NOT NULL,
    OriginalFileName NVARCHAR(255) NULL,
    UploadedOn DATETIME NOT NULL DEFAULT GETDATE(),
    UploadedByUserID INT NULL
);
IF OBJECT_ID(N'tblEmployeeDisciplineAudit', N'U') IS NULL
CREATE TABLE tblEmployeeDisciplineAudit (
    AuditID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DisciplineID INT NOT NULL REFERENCES tblEmployeeDiscipline(DisciplineID),
    ActionType NVARCHAR(50) NOT NULL,
    ActionSummary NVARCHAR(500) NULL,
    Remarks NVARCHAR(2000) NULL,
    ActionByUserID INT NULL,
    ActionByName NVARCHAR(200) NULL,
    ActionOn DATETIME NOT NULL DEFAULT GETDATE()
);
IF OBJECT_ID(N'tblEmployeeDisciplineFinding', N'U') IS NULL
CREATE TABLE tblEmployeeDisciplineFinding (
    FindingID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    EmployeeID INT NOT NULL,
    DisciplineID INT NULL,
    FindingSource NVARCHAR(100) NOT NULL,
    FindingCategory NVARCHAR(100) NULL,
    Title NVARCHAR(200) NOT NULL,
    Remarks NVARCHAR(MAX) NULL,
    FindingDate DATE NULL,
    Severity NVARCHAR(50) NULL,
    IncludeInPerformance BIT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedByUserID INT NULL,
    CreatedByName NVARCHAR(200) NULL,
    ModifiedOn DATETIME NULL,
    ModifiedByUserID INT NULL
);
IF OBJECT_ID(N'tblEmployeeDiscipline', N'U') IS NOT NULL AND COL_LENGTH(N'tblEmployeeDiscipline', N'IsActive') IS NULL
    ALTER TABLE tblEmployeeDiscipline ADD IsActive BIT NOT NULL CONSTRAINT DF_tblEmpDisc_IsActive_Runtime DEFAULT (1);
IF OBJECT_ID(N'tblEmployeeDisciplineFinding', N'U') IS NOT NULL AND COL_LENGTH(N'tblEmployeeDisciplineFinding', N'IsActive') IS NULL
    ALTER TABLE tblEmployeeDisciplineFinding ADD IsActive BIT NOT NULL CONSTRAINT DF_tblEmpDiscFind_IsActive_Runtime DEFAULT (1);
IF OBJECT_ID(N'tblEmployeeDiscipline', N'U') IS NOT NULL AND COL_LENGTH(N'tblEmployeeDiscipline', N'IncludeInPerformance') IS NULL
    ALTER TABLE tblEmployeeDiscipline ADD IncludeInPerformance BIT NOT NULL CONSTRAINT DF_tblEmpDisc_InclPerf_Runtime DEFAULT (1);", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool CanAccessModule()
            => _auth.IsAdmin || _perms.CanRead(FormKey);

        public bool CanWrite()
            => _auth.IsAdmin || _perms.CanWrite(FormKey);

        public bool CanViewPerformanceFindings()
            => CanAccessModule();

        public List<global::HRMS.LookupItem> GetEmployeeLookups()
        {
            var list = new List<global::HRMS.LookupItem>();
            var filter = _scope.GetEmployeeFilter("e");
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT e.EmployeeID,
       e.EmployeeCode + N' - ' + LTRIM(RTRIM(ISNULL(e.FirstName,'')+' '+ISNULL(e.LastName,''))) AS DisplayName
FROM tblEmployee e
WHERE e.Status = N'Active' " + filter.Sql + @"
ORDER BY e.EmployeeCode;", conn))
            {
                filter.ApplyTo(cmd);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        list.Add(new global::HRMS.LookupItem { Id = Convert.ToInt32(dr[0]), Name = dr[1]?.ToString() ?? "" });
                }
            }
            return list;
        }

        public DisciplineCaseItem GetEmployeeSnapshot(int employeeId)
        {
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT e.EmployeeID, e.EmployeeCode,
       LTRIM(RTRIM(ISNULL(e.FirstName,'')+' '+ISNULL(e.LastName,''))) AS EmployeeName,
       ISNULL(d.DepartmentName,''), ISNULL(e.Designation,''),
       ISNULL(e.JobID,0)
FROM tblEmployee e
LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
WHERE e.EmployeeID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", employeeId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    var item = new DisciplineCaseItem
                    {
                        EmployeeID = Convert.ToInt32(dr[0]),
                        EmployeeCode = dr[1]?.ToString() ?? "",
                        EmployeeName = dr[2]?.ToString() ?? "",
                        DepartmentName = dr[3]?.ToString() ?? "",
                        Designation = dr[4]?.ToString() ?? ""
                    };
                    var managerId = _requestSvc.ResolveManagerEmployeeId(employeeId);
                    if (managerId > 0)
                        item.ManagerName = GetEmployeeDisplayName(managerId);
                    return item;
                }
            }
        }

        public List<DisciplineCaseItem> ListCases(string statusFilter = null, int? employeeId = null)
        {
            EnsureSchema();
            if (!CanAccessModule()) return new List<DisciplineCaseItem>();

            var list = new List<DisciplineCaseItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT c.DisciplineID, c.CaseNumber, c.EmployeeID, e.EmployeeCode,
       LTRIM(RTRIM(ISNULL(e.FirstName,'')+' '+ISNULL(e.LastName,''))) AS EmployeeName,
       ISNULL(d.DepartmentName,'') AS DepartmentName, ISNULL(e.Designation,'') AS Designation,
       c.IncidentDate, c.ReportedDate, c.DisciplineType, c.DisciplineCategory,
       c.CaseStatus, c.IncludeInPerformance, c.CreatedOn, c.ClosedOn,
       c.DisciplinaryAction
FROM tblEmployeeDiscipline c
INNER JOIN tblEmployee e ON e.EmployeeID = c.EmployeeID
LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
WHERE c.IsActive=1
ORDER BY c.DisciplineID DESC;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var empId = Convert.ToInt32(dr["EmployeeID"]);
                        if (!_scope.CanAccessEmployee(empId)) continue;
                        var status = dr["CaseStatus"]?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(statusFilter)
                            && !status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (employeeId.HasValue && empId != employeeId.Value) continue;

                        list.Add(new DisciplineCaseItem
                        {
                            DisciplineID = Convert.ToInt32(dr["DisciplineID"]),
                            CaseNumber = dr["CaseNumber"]?.ToString() ?? "",
                            EmployeeID = empId,
                            EmployeeCode = dr["EmployeeCode"]?.ToString() ?? "",
                            EmployeeName = dr["EmployeeName"]?.ToString() ?? "",
                            DepartmentName = dr["DepartmentName"]?.ToString() ?? "",
                            Designation = dr["Designation"]?.ToString() ?? "",
                            IncidentDate = FormatDate(dr["IncidentDate"]),
                            ReportedDate = FormatDate(dr["ReportedDate"]),
                            DisciplineType = dr["DisciplineType"]?.ToString() ?? "",
                            DisciplineCategory = dr["DisciplineCategory"]?.ToString() ?? "",
                            CaseStatus = status,
                            IncludeInPerformance = Convert.ToBoolean(dr["IncludeInPerformance"]),
                            CreatedOn = Convert.ToDateTime(dr["CreatedOn"]),
                            ClosedOn = dr["ClosedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["ClosedOn"]),
                            DisciplinaryAction = dr["DisciplinaryAction"]?.ToString() ?? ""
                        });
                    }
                }
            }
            return list;
        }

        public DisciplineCaseItem GetById(int id)
        {
            EnsureSchema();
            if (!CanAccessModule()) return null;

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT c.*, e.EmployeeCode,
       LTRIM(RTRIM(ISNULL(e.FirstName,'')+' '+ISNULL(e.LastName,''))) AS EmployeeName,
       ISNULL(d.DepartmentName,'') AS DepartmentName, ISNULL(e.Designation,'') AS Designation
FROM tblEmployeeDiscipline c
INNER JOIN tblEmployee e ON e.EmployeeID = c.EmployeeID
LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
WHERE c.DisciplineID=@Id AND c.IsActive=1;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    var item = ReadFull(dr);
                    if (!_scope.CanAccessEmployee(item.EmployeeID)) return null;
                    item.ManagerName = GetEmployeeDisplayName(_requestSvc.ResolveManagerEmployeeId(item.EmployeeID));
                    return item;
                }
            }
        }

        public List<DisciplineAttachment> GetAttachments(int disciplineId)
        {
            var list = new List<DisciplineAttachment>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT AttachmentID, DocumentPath, OriginalFileName, UploadedOn
FROM tblEmployeeDisciplineAttachment WHERE DisciplineID=@Id ORDER BY AttachmentID;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", disciplineId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new DisciplineAttachment
                        {
                            AttachmentID = Convert.ToInt32(dr[0]),
                            DocumentPath = dr[1]?.ToString() ?? "",
                            OriginalFileName = dr[2]?.ToString() ?? "",
                            UploadedOn = Convert.ToDateTime(dr[3])
                        });
                    }
                }
            }
            return list;
        }

        public List<DisciplineAuditItem> GetAudit(int disciplineId)
        {
            var list = new List<DisciplineAuditItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT AuditID, ActionType, ActionSummary, Remarks, ActionByName, ActionOn
FROM tblEmployeeDisciplineAudit WHERE DisciplineID=@Id ORDER BY ActionOn DESC, AuditID DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", disciplineId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new DisciplineAuditItem
                        {
                            AuditID = Convert.ToInt32(dr[0]),
                            ActionType = dr[1]?.ToString() ?? "",
                            ActionSummary = dr[2]?.ToString() ?? "",
                            Remarks = dr[3]?.ToString() ?? "",
                            ActionByName = dr[4]?.ToString() ?? "",
                            ActionOn = Convert.ToDateTime(dr[5])
                        });
                    }
                }
            }
            return list;
        }

        public int Save(DisciplineCaseItem item)
        {
            EnsureSchema();
            if (!CanWrite()) throw new InvalidOperationException(PermissionService.AccessRestrictedMessage);
            if (item.EmployeeID <= 0) throw new InvalidOperationException("Employee is required.");
            if (!_scope.CanAccessEmployee(item.EmployeeID))
                throw new InvalidOperationException(DataAccessScopeService.AccessDeniedMessage);
            if (string.IsNullOrWhiteSpace(item.DisciplineType))
                throw new InvalidOperationException("Type of discipline / non-conformance is required.");
            if (!DisciplineStatuses.All.Contains(item.CaseStatus))
                item.CaseStatus = DisciplineStatuses.Open;

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                if (item.DisciplineID > 0)
                {
                    var existing = GetById(item.DisciplineID);
                    if (existing == null) throw new InvalidOperationException("Case not found.");
                    item.CaseNumber = existing.CaseNumber;

                    using (var cmd = new SqlCommand(@"
UPDATE tblEmployeeDiscipline SET
 EmployeeID=@EmployeeID, IncidentDate=@IncidentDate, ReportedDate=@ReportedDate,
 DisciplineType=@DisciplineType, DisciplineCategory=@DisciplineCategory, Description=@Description,
 InvestigationDetails=@InvestigationDetails, InvestigationFindings=@InvestigationFindings,
 EmployeeExplanation=@EmployeeExplanation, FindingsObservations=@FindingsObservations,
 FindingRemarks=@FindingRemarks, CorrectiveActions=@CorrectiveActions,
 DisciplinaryAction=@DisciplinaryAction, ActionDate=@ActionDate,
 ActionResponsiblePerson=@ActionResponsiblePerson, CaseStatus=@CaseStatus,
 FinalDecision=@FinalDecision, ManagementRemarks=@ManagementRemarks,
 IncludeInPerformance=@IncludeInPerformance, ModifiedOn=GETDATE(), ModifiedByUserID=@UserID,
 ClosedOn=CASE WHEN @CaseStatus IN (N'Resolved',N'Closed') AND ClosedOn IS NULL THEN GETDATE() ELSE ClosedOn END,
 ClosedByUserID=CASE WHEN @CaseStatus IN (N'Resolved',N'Closed') AND ClosedByUserID IS NULL THEN @UserID ELSE ClosedByUserID END
WHERE DisciplineID=@Id;", conn))
                    {
                        BindCase(cmd, item);
                        cmd.Parameters.AddWithValue("@Id", item.DisciplineID);
                        cmd.Parameters.AddWithValue("@UserID", (object)_auth.CurrentUserId ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                    LogAudit(conn, item.DisciplineID, "Update",
                        "Case updated. Status: " + DisciplineStatuses.DisplayName(item.CaseStatus), null);
                    SyncCaseFinding(conn, item);
                    if (!string.Equals(existing.CaseStatus, item.CaseStatus, StringComparison.OrdinalIgnoreCase))
                        NotifyDiscipline(item, "Status changed", existing.CaseStatus);
                    return item.DisciplineID;
                }

                var number = "DC-" + DateTime.Now.ToString("yyyyMMdd") + "-"
                             + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
                using (var cmd = new SqlCommand(@"
INSERT INTO tblEmployeeDiscipline
 (CaseNumber, EmployeeID, IncidentDate, ReportedDate, DisciplineType, DisciplineCategory, Description,
  InvestigationDetails, InvestigationFindings, EmployeeExplanation, FindingsObservations, FindingRemarks,
  CorrectiveActions, DisciplinaryAction, ActionDate, ActionResponsiblePerson, CaseStatus,
  FinalDecision, ManagementRemarks, IncludeInPerformance, CreatedOn, CreatedByUserID)
VALUES
 (@CaseNumber, @EmployeeID, @IncidentDate, @ReportedDate, @DisciplineType, @DisciplineCategory, @Description,
  @InvestigationDetails, @InvestigationFindings, @EmployeeExplanation, @FindingsObservations, @FindingRemarks,
  @CorrectiveActions, @DisciplinaryAction, @ActionDate, @ActionResponsiblePerson, @CaseStatus,
  @FinalDecision, @ManagementRemarks, @IncludeInPerformance, GETDATE(), @UserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
                {
                    cmd.Parameters.AddWithValue("@CaseNumber", number);
                    BindCase(cmd, item);
                    cmd.Parameters.AddWithValue("@UserID", (object)_auth.CurrentUserId ?? DBNull.Value);
                    var id = Convert.ToInt32(cmd.ExecuteScalar());
                    item.DisciplineID = id;
                    item.CaseNumber = number;
                    LogAudit(conn, id, "Create", "Case created.", null);
                    SyncCaseFinding(conn, item);
                    NotifyDiscipline(item, "Submitted", null);
                    return id;
                }
            }
        }

        public void AddAttachment(int disciplineId, string path, string originalName)
        {
            if (!CanWrite()) throw new InvalidOperationException(PermissionService.AccessRestrictedMessage);
            var existing = GetById(disciplineId);
            if (existing == null) throw new InvalidOperationException("Case not found.");

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
INSERT INTO tblEmployeeDisciplineAttachment (DisciplineID, DocumentPath, OriginalFileName, UploadedOn, UploadedByUserID)
VALUES (@Id, @Path, @Name, GETDATE(), @UserID);", conn))
            {
                cmd.Parameters.AddWithValue("@Id", disciplineId);
                cmd.Parameters.AddWithValue("@Path", path);
                cmd.Parameters.AddWithValue("@Name", (object)originalName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserID", (object)_auth.CurrentUserId ?? DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                LogAudit(conn, disciplineId, "Attachment", "Document uploaded: " + originalName, null);
            }
        }

        public int AddFinding(DisciplineFindingItem finding)
        {
            EnsureSchema();
            if (!CanWrite()) throw new InvalidOperationException(PermissionService.AccessRestrictedMessage);
            if (finding.EmployeeID <= 0) throw new InvalidOperationException("Employee is required.");
            if (!_scope.CanAccessEmployee(finding.EmployeeID))
                throw new InvalidOperationException(DataAccessScopeService.AccessDeniedMessage);
            if (string.IsNullOrWhiteSpace(finding.Title))
                throw new InvalidOperationException("Finding title is required.");
            if (string.IsNullOrWhiteSpace(finding.FindingSource))
                finding.FindingSource = "Other Incident";

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
INSERT INTO tblEmployeeDisciplineFinding
 (EmployeeID, DisciplineID, FindingSource, FindingCategory, Title, Remarks, FindingDate, Severity,
  IncludeInPerformance, IsActive, CreatedOn, CreatedByUserID, CreatedByName)
VALUES
 (@EmployeeID, @DisciplineID, @FindingSource, @FindingCategory, @Title, @Remarks, @FindingDate, @Severity,
  @IncludeInPerformance, 1, GETDATE(), @UserID, @ByName);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeID", finding.EmployeeID);
                cmd.Parameters.AddWithValue("@DisciplineID",
                    finding.DisciplineID.HasValue && finding.DisciplineID.Value > 0
                        ? (object)finding.DisciplineID.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@FindingSource", finding.FindingSource.Trim());
                cmd.Parameters.AddWithValue("@FindingCategory",
                    string.IsNullOrWhiteSpace(finding.FindingCategory) ? (object)DBNull.Value : finding.FindingCategory.Trim());
                cmd.Parameters.AddWithValue("@Title", finding.Title.Trim());
                cmd.Parameters.AddWithValue("@Remarks",
                    string.IsNullOrWhiteSpace(finding.Remarks) ? (object)DBNull.Value : finding.Remarks.Trim());
                cmd.Parameters.AddWithValue("@FindingDate", ParseDateOrDbNull(finding.FindingDate));
                cmd.Parameters.AddWithValue("@Severity",
                    string.IsNullOrWhiteSpace(finding.Severity) ? (object)DBNull.Value : finding.Severity.Trim());
                cmd.Parameters.AddWithValue("@IncludeInPerformance", finding.IncludeInPerformance);
                cmd.Parameters.AddWithValue("@UserID", (object)_auth.CurrentUserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ByName",
                    string.IsNullOrWhiteSpace(_auth.CurrentUsername) ? "User" : _auth.CurrentUsername);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public List<DisciplineFindingItem> GetFindingsForEmployee(int employeeId, bool performanceOnly = false)
        {
            EnsureSchema();
            if (!CanViewPerformanceFindings()) return new List<DisciplineFindingItem>();
            if (!_scope.CanAccessEmployee(employeeId)) return new List<DisciplineFindingItem>();

            var list = new List<DisciplineFindingItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT f.FindingID, f.EmployeeID, f.DisciplineID, ISNULL(c.CaseNumber,''),
       f.FindingSource, f.FindingCategory, f.Title, f.Remarks, f.FindingDate, f.Severity,
       f.IncludeInPerformance, ISNULL(f.CreatedByName,''), f.CreatedOn
FROM tblEmployeeDisciplineFinding f
LEFT JOIN tblEmployeeDiscipline c ON c.DisciplineID = f.DisciplineID
WHERE f.EmployeeID=@Emp AND f.IsActive=1
  AND (@PerfOnly=0 OR f.IncludeInPerformance=1)
ORDER BY ISNULL(f.FindingDate, CAST(f.CreatedOn AS DATE)) DESC, f.FindingID DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@Emp", employeeId);
                cmd.Parameters.AddWithValue("@PerfOnly", performanceOnly ? 1 : 0);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new DisciplineFindingItem
                        {
                            FindingID = Convert.ToInt32(dr[0]),
                            EmployeeID = Convert.ToInt32(dr[1]),
                            DisciplineID = dr[2] == DBNull.Value ? (int?)null : Convert.ToInt32(dr[2]),
                            CaseNumber = dr[3]?.ToString() ?? "",
                            FindingSource = dr[4]?.ToString() ?? "",
                            FindingCategory = dr[5]?.ToString() ?? "",
                            Title = dr[6]?.ToString() ?? "",
                            Remarks = dr[7]?.ToString() ?? "",
                            FindingDate = FormatDate(dr[8]),
                            Severity = dr[9]?.ToString() ?? "",
                            IncludeInPerformance = Convert.ToBoolean(dr[10]),
                            CreatedByName = dr[11]?.ToString() ?? "",
                            CreatedOn = Convert.ToDateTime(dr[12])
                        });
                    }
                }
            }
            return list;
        }

        public List<DisciplineCaseItem> GetCasesForEmployee(int employeeId)
        {
            return ListCases(null, employeeId);
        }

        private void SyncCaseFinding(SqlConnection conn, DisciplineCaseItem item)
        {
            if (!item.IncludeInPerformance) return;
            if (string.IsNullOrWhiteSpace(item.FindingRemarks)
                && string.IsNullOrWhiteSpace(item.FindingsObservations)
                && string.IsNullOrWhiteSpace(item.InvestigationFindings))
                return;

            int? existingId = null;
            using (var find = new SqlCommand(@"
SELECT TOP 1 FindingID FROM tblEmployeeDisciplineFinding
WHERE DisciplineID=@Id AND IsActive=1 ORDER BY FindingID;", conn))
            {
                find.Parameters.AddWithValue("@Id", item.DisciplineID);
                var r = find.ExecuteScalar();
                if (r != null && r != DBNull.Value) existingId = Convert.ToInt32(r);
            }

            var title = (item.DisciplineType ?? "Discipline") + " - " + (item.CaseNumber ?? ("#" + item.DisciplineID));
            var remarks = string.Join(Environment.NewLine + Environment.NewLine, new[]
            {
                string.IsNullOrWhiteSpace(item.InvestigationFindings) ? null : "Investigation findings: " + item.InvestigationFindings.Trim(),
                string.IsNullOrWhiteSpace(item.FindingsObservations) ? null : "Observations: " + item.FindingsObservations.Trim(),
                string.IsNullOrWhiteSpace(item.FindingRemarks) ? null : "Remarks: " + item.FindingRemarks.Trim(),
                string.IsNullOrWhiteSpace(item.DisciplinaryAction) ? null : "Action: " + item.DisciplinaryAction.Trim(),
                string.IsNullOrWhiteSpace(item.FinalDecision) ? null : "Final decision: " + item.FinalDecision.Trim()
            }.Where(s => s != null));

            if (existingId.HasValue)
            {
                using (var cmd = new SqlCommand(@"
UPDATE tblEmployeeDisciplineFinding SET
 Title=@Title, Remarks=@Remarks, FindingCategory=@Cat, FindingDate=@FindingDate,
 Severity=@Severity, IncludeInPerformance=1, ModifiedOn=GETDATE(), ModifiedByUserID=@UserID
WHERE FindingID=@Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(remarks) ? (object)DBNull.Value : remarks);
                    cmd.Parameters.AddWithValue("@Cat", string.IsNullOrWhiteSpace(item.DisciplineCategory) ? (object)DBNull.Value : item.DisciplineCategory);
                    cmd.Parameters.AddWithValue("@FindingDate", ParseDateOrDbNull(item.IncidentDate));
                    cmd.Parameters.AddWithValue("@Severity", string.IsNullOrWhiteSpace(item.DisciplinaryAction) ? (object)DBNull.Value : item.DisciplinaryAction);
                    cmd.Parameters.AddWithValue("@UserID", (object)_auth.CurrentUserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", existingId.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                using (var cmd = new SqlCommand(@"
INSERT INTO tblEmployeeDisciplineFinding
 (EmployeeID, DisciplineID, FindingSource, FindingCategory, Title, Remarks, FindingDate, Severity,
  IncludeInPerformance, IsActive, CreatedOn, CreatedByUserID, CreatedByName)
VALUES
 (@EmployeeID, @DisciplineID, @Source, @Cat, @Title, @Remarks, @FindingDate, @Severity,
  1, 1, GETDATE(), @UserID, @ByName);", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", item.EmployeeID);
                    cmd.Parameters.AddWithValue("@DisciplineID", item.DisciplineID);
                    cmd.Parameters.AddWithValue("@Source",
                        string.Equals(item.DisciplineType, "Non-Conformance", StringComparison.OrdinalIgnoreCase)
                            ? "Non-Conformance" : "Disciplinary Case");
                    cmd.Parameters.AddWithValue("@Cat", string.IsNullOrWhiteSpace(item.DisciplineCategory) ? (object)DBNull.Value : item.DisciplineCategory);
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(remarks) ? (object)DBNull.Value : remarks);
                    cmd.Parameters.AddWithValue("@FindingDate", ParseDateOrDbNull(item.IncidentDate));
                    cmd.Parameters.AddWithValue("@Severity", string.IsNullOrWhiteSpace(item.DisciplinaryAction) ? (object)DBNull.Value : item.DisciplinaryAction);
                    cmd.Parameters.AddWithValue("@UserID", (object)_auth.CurrentUserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ByName",
                        string.IsNullOrWhiteSpace(_auth.CurrentUsername) ? "User" : _auth.CurrentUsername);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void LogAudit(SqlConnection conn, int disciplineId, string actionType, string summary, string remarks)
        {
            using (var cmd = new SqlCommand(@"
INSERT INTO tblEmployeeDisciplineAudit
 (DisciplineID, ActionType, ActionSummary, Remarks, ActionByUserID, ActionByName, ActionOn)
VALUES
 (@Id, @Type, @Summary, @Remarks, @UserID, @ByName, GETDATE());", conn))
            {
                cmd.Parameters.AddWithValue("@Id", disciplineId);
                cmd.Parameters.AddWithValue("@Type", actionType);
                cmd.Parameters.AddWithValue("@Summary", (object)summary ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(remarks) ? (object)DBNull.Value : remarks);
                cmd.Parameters.AddWithValue("@UserID", (object)_auth.CurrentUserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ByName",
                    string.IsNullOrWhiteSpace(_auth.CurrentUsername) ? "User" : _auth.CurrentUsername);
                cmd.ExecuteNonQuery();
            }
        }

        private static void BindCase(SqlCommand cmd, DisciplineCaseItem item)
        {
            cmd.Parameters.AddWithValue("@EmployeeID", item.EmployeeID);
            cmd.Parameters.AddWithValue("@IncidentDate", ParseDateOrDbNull(item.IncidentDate));
            cmd.Parameters.AddWithValue("@ReportedDate", ParseDateOrDbNull(item.ReportedDate));
            cmd.Parameters.AddWithValue("@DisciplineType", item.DisciplineType.Trim());
            cmd.Parameters.AddWithValue("@DisciplineCategory",
                string.IsNullOrWhiteSpace(item.DisciplineCategory) ? (object)DBNull.Value : item.DisciplineCategory.Trim());
            cmd.Parameters.AddWithValue("@Description", NullIfEmpty(item.Description));
            cmd.Parameters.AddWithValue("@InvestigationDetails", NullIfEmpty(item.InvestigationDetails));
            cmd.Parameters.AddWithValue("@InvestigationFindings", NullIfEmpty(item.InvestigationFindings));
            cmd.Parameters.AddWithValue("@EmployeeExplanation", NullIfEmpty(item.EmployeeExplanation));
            cmd.Parameters.AddWithValue("@FindingsObservations", NullIfEmpty(item.FindingsObservations));
            cmd.Parameters.AddWithValue("@FindingRemarks", NullIfEmpty(item.FindingRemarks));
            cmd.Parameters.AddWithValue("@CorrectiveActions", NullIfEmpty(item.CorrectiveActions));
            cmd.Parameters.AddWithValue("@DisciplinaryAction",
                string.IsNullOrWhiteSpace(item.DisciplinaryAction) ? (object)DBNull.Value : item.DisciplinaryAction.Trim());
            cmd.Parameters.AddWithValue("@ActionDate", ParseDateOrDbNull(item.ActionDate));
            cmd.Parameters.AddWithValue("@ActionResponsiblePerson",
                string.IsNullOrWhiteSpace(item.ActionResponsiblePerson) ? (object)DBNull.Value : item.ActionResponsiblePerson.Trim());
            cmd.Parameters.AddWithValue("@CaseStatus", item.CaseStatus);
            cmd.Parameters.AddWithValue("@FinalDecision", NullIfEmpty(item.FinalDecision));
            cmd.Parameters.AddWithValue("@ManagementRemarks", NullIfEmpty(item.ManagementRemarks));
            cmd.Parameters.AddWithValue("@IncludeInPerformance", item.IncludeInPerformance);
        }

        private DisciplineCaseItem ReadFull(SqlDataReader dr)
        {
            return new DisciplineCaseItem
            {
                DisciplineID = Convert.ToInt32(dr["DisciplineID"]),
                CaseNumber = dr["CaseNumber"]?.ToString() ?? "",
                EmployeeID = Convert.ToInt32(dr["EmployeeID"]),
                EmployeeCode = dr["EmployeeCode"]?.ToString() ?? "",
                EmployeeName = dr["EmployeeName"]?.ToString() ?? "",
                DepartmentName = SafeString(dr, "DepartmentName"),
                Designation = SafeString(dr, "Designation"),
                IncidentDate = FormatDate(dr["IncidentDate"]),
                ReportedDate = FormatDate(dr["ReportedDate"]),
                DisciplineType = SafeString(dr, "DisciplineType"),
                DisciplineCategory = SafeString(dr, "DisciplineCategory"),
                Description = SafeString(dr, "Description"),
                InvestigationDetails = SafeString(dr, "InvestigationDetails"),
                InvestigationFindings = SafeString(dr, "InvestigationFindings"),
                EmployeeExplanation = SafeString(dr, "EmployeeExplanation"),
                FindingsObservations = SafeString(dr, "FindingsObservations"),
                FindingRemarks = SafeString(dr, "FindingRemarks"),
                CorrectiveActions = SafeString(dr, "CorrectiveActions"),
                DisciplinaryAction = SafeString(dr, "DisciplinaryAction"),
                ActionDate = FormatDate(dr["ActionDate"]),
                ActionResponsiblePerson = SafeString(dr, "ActionResponsiblePerson"),
                CaseStatus = SafeString(dr, "CaseStatus"),
                FinalDecision = SafeString(dr, "FinalDecision"),
                ManagementRemarks = SafeString(dr, "ManagementRemarks"),
                IncludeInPerformance = Convert.ToBoolean(dr["IncludeInPerformance"]),
                CreatedOn = Convert.ToDateTime(dr["CreatedOn"]),
                ClosedOn = dr["ClosedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["ClosedOn"])
            };
        }

        private string GetEmployeeDisplayName(int employeeId)
        {
            if (employeeId <= 0) return "";
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT LTRIM(RTRIM(ISNULL(FirstName,'')+' '+ISNULL(LastName,''))) FROM tblEmployee WHERE EmployeeID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", employeeId);
                conn.Open();
                var r = cmd.ExecuteScalar();
                return r?.ToString()?.Trim() ?? "";
            }
        }

        private static string FormatDate(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            return Convert.ToDateTime(value).ToString("yyyy-MM-dd");
        }

        private static object ParseDateOrDbNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
            if (DateTime.TryParse(value, out var dt)) return dt.Date;
            return DBNull.Value;
        }

        private static object NullIfEmpty(string value)
            => string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();

        private static string SafeString(SqlDataReader dr, string col)
        {
            try
            {
                var ord = dr.GetOrdinal(col);
                return dr.IsDBNull(ord) ? "" : dr.GetValue(ord)?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private void NotifyDiscipline(DisciplineCaseItem item, string action, string previousStatus)
        {
            if (item == null || item.DisciplineID <= 0) return;
            var emp = string.IsNullOrWhiteSpace(item.EmployeeName)
                ? _inbox.EmployeeLabel(item.EmployeeID)
                : ((item.EmployeeCode ?? "") + " " + item.EmployeeName).Trim();
            var status = DisciplineStatuses.DisplayName(item.CaseStatus);
            if (!string.IsNullOrWhiteSpace(previousStatus)
                && !string.Equals(previousStatus, item.CaseStatus, StringComparison.OrdinalIgnoreCase))
                action = DisciplineStatuses.DisplayName(previousStatus) + " → " + status;
            var body = _inbox.FormatBody("Employee Discipline", item.CaseNumber, emp, action, status, _inbox.ActorName());
            var title = "Discipline " + item.CaseNumber + " — " + status;
            var link = "/EmployeeDiscipline.aspx?editId=" + item.DisciplineID;
            var targets = new HashSet<int>(_inbox.UserIdsForEmployee(item.EmployeeID));
            foreach (var uid in _inbox.UserIdsWithForm(FormKey))
                targets.Add(uid);
            if (_auth.CurrentUserId.HasValue)
                targets.Add(_auth.CurrentUserId.Value);
            _inbox.NotifyUsers(targets, title, body, link, AppInboxService.TypeDiscipline, item.DisciplineID);
        }
    }
}
