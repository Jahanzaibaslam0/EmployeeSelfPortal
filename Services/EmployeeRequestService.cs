using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Script.Serialization;

namespace HRMS.Services
{
    public static class EmployeeRequestCategories
    {
        public const string MobilePhone = "MobilePhone";
        public const string HealthClaim = "HealthClaim";
        public const string Travelling = "Travelling";
        public const string EmployeeLoan = "EmployeeLoan";
        public const string Vehicle = "Vehicle";
        public const string Fuel = "Fuel";
        public const string Other = "Other";

        public static readonly string[] All =
        {
            MobilePhone, HealthClaim, Travelling, EmployeeLoan, Vehicle, Fuel, Other
        };

        public static string DisplayName(string category)
        {
            switch (category)
            {
                case MobilePhone: return "Mobile Phone";
                case HealthClaim: return "Health Claim";
                case Travelling: return "Travelling";
                case EmployeeLoan: return "Employee Loan";
                case Vehicle: return "Vehicle";
                case Fuel: return "Fuel";
                case Other: return "Other Requests";
                default: return category ?? "";
            }
        }
    }

    public static class EmployeeRequestStatuses
    {
        public const string Draft = "Draft";
        public const string Submitted = "Submitted";
        public const string PendingManagerApproval = "PendingManagerApproval";
        public const string PendingHRApproval = "PendingHRApproval";
        public const string PendingManagementApproval = "PendingManagementApproval";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string ReturnedForCorrection = "ReturnedForCorrection";

        public static string DisplayName(string status)
        {
            switch (status)
            {
                case Draft: return "Draft";
                case Submitted: return "Submitted";
                case PendingManagerApproval: return "Pending Manager Approval";
                case PendingHRApproval: return "Pending HR Approval";
                case PendingManagementApproval: return "Pending Management Approval";
                case Approved: return "Approved";
                case Rejected: return "Rejected";
                case ReturnedForCorrection: return "Returned for Correction";
                default: return status ?? "";
            }
        }

        public static string BadgeClass(string status)
        {
            switch (status)
            {
                case Approved: return "req-badge req-approved";
                case Rejected: return "req-badge req-rejected";
                case ReturnedForCorrection: return "req-badge req-returned";
                case Draft: return "req-badge req-draft";
                default: return "req-badge req-pending";
            }
        }
    }

    public class EmployeeRequestItem
    {
        public int RequestID { get; set; }
        public string RequestNumber { get; set; } = "";
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string RequestCategory { get; set; } = EmployeeRequestCategories.Other;
        public string CategoryDisplay => EmployeeRequestCategories.DisplayName(RequestCategory);
        public string Status { get; set; } = EmployeeRequestStatuses.Draft;
        public string StatusDisplay => EmployeeRequestStatuses.DisplayName(Status);
        public string StatusBadgeClass => EmployeeRequestStatuses.BadgeClass(Status);
        public string Subject { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal? Amount { get; set; }
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
        public string DetailsJson { get; set; } = "";
        public int ManagerEmployeeID { get; set; }
        public string ManagerName { get; set; } = "";
        public DateTime? SubmittedOn { get; set; }
        public DateTime CreatedOn { get; set; }
        public Dictionary<string, string> Details { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public class EmployeeRequestAttachment
    {
        public int AttachmentID { get; set; }
        public string DocumentPath { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public DateTime UploadedOn { get; set; }
    }

    public class EmployeeRequestAction
    {
        public int ActionID { get; set; }
        public string ActionStage { get; set; } = "";
        public string ActionType { get; set; } = "";
        public string Comments { get; set; } = "";
        public string ActionByName { get; set; } = "";
        public DateTime ActionOn { get; set; }
    }

    public class EmployeeRequestAlertItem
    {
        public int AlertID { get; set; }
        public int RequestID { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string LinkUrl { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class EmployeeRequestService
    {
        public const string FormKey = "EmployeeRequest";
        public const string HrFormKey = "EmployeeRequestHR";
        public const string ManagementFormKey = "EmployeeRequestManagement";

        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();
        private readonly PermissionService _perms = new PermissionService();
        private readonly EmployeeProfileAccessService _profile = new EmployeeProfileAccessService();
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();
        private readonly AppInboxService _inbox = new AppInboxService();

        public EmployeeRequestService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public void EnsureSchema()
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'tblEmployeeRequest', N'U') IS NULL
BEGIN
    CREATE TABLE tblEmployeeRequest (
        RequestID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RequestNumber NVARCHAR(30) NOT NULL,
        EmployeeID INT NOT NULL,
        RequestCategory NVARCHAR(50) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT N'Draft',
        Subject NVARCHAR(200) NOT NULL,
        Description NVARCHAR(2000) NULL,
        Amount DECIMAL(18,2) NULL,
        StartDate DATE NULL,
        EndDate DATE NULL,
        DetailsJson NVARCHAR(MAX) NULL,
        ManagerEmployeeID INT NULL,
        SubmittedOn DATETIME NULL,
        CompletedOn DATETIME NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedOn DATETIME NULL,
        CreatedByUserID INT NULL,
        ModifiedByUserID INT NULL
    );
END
IF OBJECT_ID(N'tblEmployeeRequestAttachment', N'U') IS NULL
CREATE TABLE tblEmployeeRequestAttachment (
    AttachmentID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RequestID INT NOT NULL REFERENCES tblEmployeeRequest(RequestID),
    DocumentPath NVARCHAR(500) NOT NULL,
    OriginalFileName NVARCHAR(255) NULL,
    UploadedOn DATETIME NOT NULL DEFAULT GETDATE(),
    UploadedByUserID INT NULL
);
IF OBJECT_ID(N'tblEmployeeRequestAction', N'U') IS NULL
CREATE TABLE tblEmployeeRequestAction (
    ActionID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RequestID INT NOT NULL REFERENCES tblEmployeeRequest(RequestID),
    ActionStage NVARCHAR(50) NOT NULL,
    ActionType NVARCHAR(50) NOT NULL,
    Comments NVARCHAR(2000) NULL,
    ActionByUserID INT NULL,
    ActionByEmployeeID INT NULL,
    ActionByName NVARCHAR(200) NULL,
    ActionOn DATETIME NOT NULL DEFAULT GETDATE()
);
IF OBJECT_ID(N'tblEmployeeRequestAlert', N'U') IS NULL
CREATE TABLE tblEmployeeRequestAlert (
    AlertID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserID INT NOT NULL,
    RequestID INT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(1000) NULL,
    LinkUrl NVARCHAR(300) NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE()
);", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool CanAccessModule()
            => _auth.IsAdmin
               || _perms.CanRead(FormKey)
               || _perms.CanRead(HrFormKey)
               || _perms.CanRead(ManagementFormKey)
               || _profile.IsEmployeeProfileSynchronized();

        public bool CanActAsHr()
            => _auth.IsAdmin || _perms.CanWrite(HrFormKey) || _perms.CanRead(HrFormKey);

        public bool CanActAsManagement()
            => _auth.IsAdmin || _perms.CanWrite(ManagementFormKey) || _perms.CanRead(ManagementFormKey);

        public int? GetLinkedEmployeeId() => _profile.GetLinkedEmployeeId();

        public List<EmployeeRequestItem> ListVisible(string inbox = null)
        {
            EnsureSchema();
            var list = new List<EmployeeRequestItem>();
            var linked = GetLinkedEmployeeId() ?? 0;
            var isAdmin = _auth.IsAdmin;
            var isHr = CanActAsHr();
            var isMgmt = CanActAsManagement();

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT r.RequestID, r.RequestNumber, r.EmployeeID, e.EmployeeCode,
       LTRIM(RTRIM(ISNULL(e.FirstName,'')+' '+ISNULL(e.LastName,''))) AS EmployeeName,
       ISNULL(d.DepartmentName,'') AS DepartmentName,
       r.RequestCategory, r.Status, r.Subject, r.Description, r.Amount,
       r.StartDate, r.EndDate, r.DetailsJson, ISNULL(r.ManagerEmployeeID,0) AS ManagerEmployeeID,
       LTRIM(RTRIM(ISNULL(m.FirstName,'')+' '+ISNULL(m.LastName,''))) AS ManagerName,
       r.SubmittedOn, r.CreatedOn
FROM tblEmployeeRequest r
INNER JOIN tblEmployee e ON e.EmployeeID = r.EmployeeID
LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
LEFT JOIN tblEmployee m ON m.EmployeeID = r.ManagerEmployeeID
WHERE r.IsActive = 1
ORDER BY r.RequestID DESC;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var item = ReadRequest(dr);
                        if (!CanViewRequest(item, linked, isAdmin, isHr, isMgmt))
                            continue;
                        if (!string.IsNullOrWhiteSpace(inbox))
                        {
                            if (inbox.Equals("mine", StringComparison.OrdinalIgnoreCase) && item.EmployeeID != linked)
                                continue;
                            if (inbox.Equals("manager", StringComparison.OrdinalIgnoreCase)
                                && !(item.ManagerEmployeeID == linked
                                     && item.Status == EmployeeRequestStatuses.PendingManagerApproval))
                                continue;
                            if (inbox.Equals("hr", StringComparison.OrdinalIgnoreCase)
                                && !(isHr && item.Status == EmployeeRequestStatuses.PendingHRApproval))
                                continue;
                            if (inbox.Equals("management", StringComparison.OrdinalIgnoreCase)
                                && !(isMgmt && item.Status == EmployeeRequestStatuses.PendingManagementApproval))
                                continue;
                        }
                        list.Add(item);
                    }
                }
            }
            return list;
        }

        public EmployeeRequestItem GetById(int requestId)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT r.RequestID, r.RequestNumber, r.EmployeeID, e.EmployeeCode,
       LTRIM(RTRIM(ISNULL(e.FirstName,'')+' '+ISNULL(e.LastName,''))) AS EmployeeName,
       ISNULL(d.DepartmentName,'') AS DepartmentName,
       r.RequestCategory, r.Status, r.Subject, r.Description, r.Amount,
       r.StartDate, r.EndDate, r.DetailsJson, ISNULL(r.ManagerEmployeeID,0) AS ManagerEmployeeID,
       LTRIM(RTRIM(ISNULL(m.FirstName,'')+' '+ISNULL(m.LastName,''))) AS ManagerName,
       r.SubmittedOn, r.CreatedOn
FROM tblEmployeeRequest r
INNER JOIN tblEmployee e ON e.EmployeeID = r.EmployeeID
LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
LEFT JOIN tblEmployee m ON m.EmployeeID = r.ManagerEmployeeID
WHERE r.RequestID = @Id AND r.IsActive = 1;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", requestId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    var item = ReadRequest(dr);
                    var linked = GetLinkedEmployeeId() ?? 0;
                    if (!CanViewRequest(item, linked, _auth.IsAdmin, CanActAsHr(), CanActAsManagement()))
                        return null;
                    item.Details = DeserializeDetails(item.DetailsJson);
                    return item;
                }
            }
        }

        public List<EmployeeRequestAttachment> GetAttachments(int requestId)
        {
            var list = new List<EmployeeRequestAttachment>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT AttachmentID, DocumentPath, OriginalFileName, UploadedOn
FROM tblEmployeeRequestAttachment WHERE RequestID=@Id ORDER BY AttachmentID;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", requestId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new EmployeeRequestAttachment
                        {
                            AttachmentID = Convert.ToInt32(dr["AttachmentID"]),
                            DocumentPath = dr["DocumentPath"]?.ToString() ?? "",
                            OriginalFileName = dr["OriginalFileName"] == DBNull.Value ? "" : dr["OriginalFileName"].ToString() ?? "",
                            UploadedOn = Convert.ToDateTime(dr["UploadedOn"])
                        });
                    }
                }
            }
            return list;
        }

        public List<EmployeeRequestAction> GetActions(int requestId)
        {
            var list = new List<EmployeeRequestAction>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT ActionID, ActionStage, ActionType, Comments, ActionByName, ActionOn
FROM tblEmployeeRequestAction WHERE RequestID=@Id ORDER BY ActionOn, ActionID;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", requestId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new EmployeeRequestAction
                        {
                            ActionID = Convert.ToInt32(dr["ActionID"]),
                            ActionStage = dr["ActionStage"]?.ToString() ?? "",
                            ActionType = dr["ActionType"]?.ToString() ?? "",
                            Comments = dr["Comments"] == DBNull.Value ? "" : dr["Comments"].ToString() ?? "",
                            ActionByName = dr["ActionByName"] == DBNull.Value ? "" : dr["ActionByName"].ToString() ?? "",
                            ActionOn = Convert.ToDateTime(dr["ActionOn"])
                        });
                    }
                }
            }
            return list;
        }

        public int SaveDraft(EmployeeRequestItem item)
        {
            EnsureSchema();
            var linked = RequireLinkedEmployee();
            if (item.EmployeeID <= 0) item.EmployeeID = linked;
            if (item.EmployeeID != linked && !_auth.IsAdmin)
                throw new InvalidOperationException("You can only create requests for your own employee profile.");

            item.DetailsJson = SerializeDetails(item.Details);
            if (string.IsNullOrWhiteSpace(item.Subject))
                throw new InvalidOperationException("Subject is required.");
            if (!EmployeeRequestCategories.All.Contains(item.RequestCategory))
                item.RequestCategory = EmployeeRequestCategories.Other;

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                if (item.RequestID > 0)
                {
                    var existing = GetById(item.RequestID);
                    if (existing == null) throw new InvalidOperationException("Request not found.");
                    if (existing.Status != EmployeeRequestStatuses.Draft
                        && existing.Status != EmployeeRequestStatuses.ReturnedForCorrection)
                        throw new InvalidOperationException("Only draft or returned requests can be edited.");
                    if (existing.EmployeeID != linked && !_auth.IsAdmin)
                        throw new InvalidOperationException("You cannot edit this request.");

                    using (var cmd = new SqlCommand(@"
UPDATE tblEmployeeRequest SET
 RequestCategory=@RequestCategory, Subject=@Subject, Description=@Description,
 Amount=@Amount, StartDate=@StartDate, EndDate=@EndDate, DetailsJson=@DetailsJson,
 ModifiedOn=GETDATE(), ModifiedByUserID=@UserID
WHERE RequestID=@RequestID;", conn))
                    {
                        BindCommon(cmd, item);
                        cmd.Parameters.AddWithValue("@RequestID", item.RequestID);
                        cmd.Parameters.AddWithValue("@UserID", (object)_auth.CurrentUserId ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                    LogAction(conn, item.RequestID, "Employee", "SaveDraft", "Draft saved.", linked);
                    return item.RequestID;
                }

                var number = "ER-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
                using (var cmd = new SqlCommand(@"
INSERT INTO tblEmployeeRequest
 (RequestNumber, EmployeeID, RequestCategory, Status, Subject, Description, Amount, StartDate, EndDate, DetailsJson,
  CreatedOn, CreatedByUserID)
VALUES
 (@RequestNumber, @EmployeeID, @RequestCategory, @Status, @Subject, @Description, @Amount, @StartDate, @EndDate, @DetailsJson,
  GETDATE(), @UserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
                {
                    cmd.Parameters.AddWithValue("@RequestNumber", number);
                    cmd.Parameters.AddWithValue("@EmployeeID", item.EmployeeID);
                    cmd.Parameters.AddWithValue("@Status", EmployeeRequestStatuses.Draft);
                    BindCommon(cmd, item);
                    cmd.Parameters.AddWithValue("@UserID", (object)_auth.CurrentUserId ?? DBNull.Value);
                    var id = Convert.ToInt32(cmd.ExecuteScalar());
                    LogAction(conn, id, "Employee", "SaveDraft", "Request created as draft.", linked);
                    return id;
                }
            }
        }

        public void Submit(int requestId, string comments)
        {
            EnsureSchema();
            var linked = RequireLinkedEmployee();
            var item = GetById(requestId) ?? throw new InvalidOperationException("Request not found.");
            if (item.EmployeeID != linked && !_auth.IsAdmin)
                throw new InvalidOperationException("You can only submit your own requests.");
            if (item.Status != EmployeeRequestStatuses.Draft && item.Status != EmployeeRequestStatuses.ReturnedForCorrection)
                throw new InvalidOperationException("Request is not in a submittable status.");

            var managerId = ResolveManagerEmployeeId(item.EmployeeID);
            if (managerId <= 0)
                throw new InvalidOperationException("No reporting manager is configured for your profile. Contact HR.");

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
UPDATE tblEmployeeRequest SET
 Status=@Status, ManagerEmployeeID=@ManagerEmployeeID, SubmittedOn=GETDATE(),
 ModifiedOn=GETDATE(), ModifiedByUserID=@UserID
WHERE RequestID=@Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Status", EmployeeRequestStatuses.PendingManagerApproval);
                    cmd.Parameters.AddWithValue("@ManagerEmployeeID", managerId);
                    cmd.Parameters.AddWithValue("@UserID", (object)_auth.CurrentUserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", requestId);
                    cmd.ExecuteNonQuery();
                }
                LogAction(conn, requestId, "Employee", "Submit",
                    string.IsNullOrWhiteSpace(comments) ? "Submitted for manager approval." : comments.Trim(), linked);
                var body = RequestInboxBody(item, "Submitted", EmployeeRequestStatuses.DisplayName(EmployeeRequestStatuses.PendingManagerApproval));
                NotifyEmployeeUsers(conn, managerId, requestId, "Manager approval required", body);
                NotifyEmployeeUsers(conn, item.EmployeeID, requestId, "Request submitted", body);
            }
        }

        public void Approve(int requestId, string comments)
        {
            EnsureSchema();
            var item = GetById(requestId) ?? throw new InvalidOperationException("Request not found.");
            var linked = GetLinkedEmployeeId() ?? 0;

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                if (item.Status == EmployeeRequestStatuses.PendingManagerApproval)
                {
                    if (linked != item.ManagerEmployeeID && !_auth.IsAdmin)
                        throw new InvalidOperationException("Only the reporting manager can approve at this stage.");
                    UpdateStatus(conn, requestId, EmployeeRequestStatuses.PendingHRApproval, null);
                    LogAction(conn, requestId, "Manager", "Approve",
                        string.IsNullOrWhiteSpace(comments) ? "Approved by manager." : comments.Trim(), linked);
                    var bodyHr = RequestInboxBody(item, "Approved (Manager)", EmployeeRequestStatuses.DisplayName(EmployeeRequestStatuses.PendingHRApproval));
                    NotifyHrUsers(conn, requestId, item.RequestNumber, bodyHr);
                    NotifyEmployeeUsers(conn, item.EmployeeID, requestId, "Request forwarded to HR", bodyHr);
                    NotifyCurrentActor("Request forwarded to HR", bodyHr, requestId);
                }
                else if (item.Status == EmployeeRequestStatuses.PendingHRApproval)
                {
                    if (!CanActAsHr())
                        throw new InvalidOperationException("You are not authorized for HR approval.");
                    UpdateStatus(conn, requestId, EmployeeRequestStatuses.PendingManagementApproval, null);
                    LogAction(conn, requestId, "HR", "Approve",
                        string.IsNullOrWhiteSpace(comments) ? "Approved by HR." : comments.Trim(), linked);
                    var bodyMgmt = RequestInboxBody(item, "Approved (HR)", EmployeeRequestStatuses.DisplayName(EmployeeRequestStatuses.PendingManagementApproval));
                    NotifyManagementUsers(conn, requestId, item.RequestNumber, bodyMgmt);
                    NotifyEmployeeUsers(conn, item.EmployeeID, requestId, "Request forwarded to Management", bodyMgmt);
                    NotifyCurrentActor("Request forwarded to Management", bodyMgmt, requestId);
                }
                else if (item.Status == EmployeeRequestStatuses.PendingManagementApproval)
                {
                    if (!CanActAsManagement())
                        throw new InvalidOperationException("You are not authorized for management approval.");
                    UpdateStatus(conn, requestId, EmployeeRequestStatuses.Approved, DateTime.Now);
                    LogAction(conn, requestId, "Management", "Approve",
                        string.IsNullOrWhiteSpace(comments) ? "Final approval granted." : comments.Trim(), linked);
                    var bodyOk = RequestInboxBody(item, "Approved", EmployeeRequestStatuses.DisplayName(EmployeeRequestStatuses.Approved));
                    NotifyEmployeeUsers(conn, item.EmployeeID, requestId, "Request approved", bodyOk);
                    NotifyCurrentActor("Request approved", bodyOk, requestId);
                }
                else
                    throw new InvalidOperationException("Request is not awaiting approval.");
            }
        }

        public void Reject(int requestId, string comments)
        {
            EnsureSchema();
            if (string.IsNullOrWhiteSpace(comments))
                throw new InvalidOperationException("Comments are required when rejecting a request.");
            var item = GetById(requestId) ?? throw new InvalidOperationException("Request not found.");
            var linked = GetLinkedEmployeeId() ?? 0;
            string stage;
            if (item.Status == EmployeeRequestStatuses.PendingManagerApproval)
            {
                if (linked != item.ManagerEmployeeID && !_auth.IsAdmin)
                    throw new InvalidOperationException("Only the reporting manager can reject at this stage.");
                stage = "Manager";
            }
            else if (item.Status == EmployeeRequestStatuses.PendingHRApproval)
            {
                if (!CanActAsHr()) throw new InvalidOperationException("Not authorized for HR rejection.");
                stage = "HR";
            }
            else if (item.Status == EmployeeRequestStatuses.PendingManagementApproval)
            {
                if (!CanActAsManagement()) throw new InvalidOperationException("Not authorized for management rejection.");
                stage = "Management";
            }
            else
                throw new InvalidOperationException("Request is not awaiting approval.");

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                UpdateStatus(conn, requestId, EmployeeRequestStatuses.Rejected, DateTime.Now);
                LogAction(conn, requestId, stage, "Reject", comments.Trim(), linked);
                var body = RequestInboxBody(item, "Rejected", EmployeeRequestStatuses.DisplayName(EmployeeRequestStatuses.Rejected))
                    + "\nComments: " + comments.Trim();
                NotifyEmployeeUsers(conn, item.EmployeeID, requestId, "Request rejected", body);
                NotifyCurrentActor("Request rejected", body, requestId);
            }
        }

        public void ReturnForCorrection(int requestId, string comments)
        {
            EnsureSchema();
            if (string.IsNullOrWhiteSpace(comments))
                throw new InvalidOperationException("Comments are required when returning a request.");
            var item = GetById(requestId) ?? throw new InvalidOperationException("Request not found.");
            var linked = GetLinkedEmployeeId() ?? 0;
            string stage;
            if (item.Status == EmployeeRequestStatuses.PendingManagerApproval)
            {
                if (linked != item.ManagerEmployeeID && !_auth.IsAdmin)
                    throw new InvalidOperationException("Only the reporting manager can return this request.");
                stage = "Manager";
            }
            else if (item.Status == EmployeeRequestStatuses.PendingHRApproval)
            {
                if (!CanActAsHr()) throw new InvalidOperationException("Not authorized.");
                stage = "HR";
            }
            else if (item.Status == EmployeeRequestStatuses.PendingManagementApproval)
            {
                if (!CanActAsManagement()) throw new InvalidOperationException("Not authorized.");
                stage = "Management";
            }
            else
                throw new InvalidOperationException("Request is not awaiting approval.");

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                UpdateStatus(conn, requestId, EmployeeRequestStatuses.ReturnedForCorrection, null);
                LogAction(conn, requestId, stage, "Return", comments.Trim(), linked);
                var body = RequestInboxBody(item, "Returned for correction", EmployeeRequestStatuses.DisplayName(EmployeeRequestStatuses.ReturnedForCorrection))
                    + "\nComments: " + comments.Trim();
                NotifyEmployeeUsers(conn, item.EmployeeID, requestId, "Request returned for correction", body);
                NotifyCurrentActor("Request returned for correction", body, requestId);
            }
        }

        public void AddAttachment(int requestId, string path, string originalName)
        {
            var item = GetById(requestId) ?? throw new InvalidOperationException("Request not found.");
            var linked = RequireLinkedEmployee();
            if (item.EmployeeID != linked && !_auth.IsAdmin)
                throw new InvalidOperationException("You cannot attach files to this request.");
            if (item.Status != EmployeeRequestStatuses.Draft && item.Status != EmployeeRequestStatuses.ReturnedForCorrection)
                throw new InvalidOperationException("Attachments can only be added to draft or returned requests.");

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
INSERT INTO tblEmployeeRequestAttachment (RequestID, DocumentPath, OriginalFileName, UploadedOn, UploadedByUserID)
VALUES (@RequestID, @DocumentPath, @OriginalFileName, GETDATE(), @UserID);", conn))
            {
                cmd.Parameters.AddWithValue("@RequestID", requestId);
                cmd.Parameters.AddWithValue("@DocumentPath", path);
                cmd.Parameters.AddWithValue("@OriginalFileName", (object)originalName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserID", (object)_auth.CurrentUserId ?? DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<EmployeeRequestAlertItem> GetUnreadAlerts(int take = 20)
        {
            EnsureSchema();
            var list = new List<EmployeeRequestAlertItem>();
            if (!_auth.CurrentUserId.HasValue) return list;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT TOP (@Take) AlertID, ISNULL(RequestID,0), Title, Message, LinkUrl, IsRead, CreatedOn
FROM tblEmployeeRequestAlert
WHERE UserID=@UserID AND IsRead=0
ORDER BY CreatedOn DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@Take", take);
                cmd.Parameters.AddWithValue("@UserID", _auth.CurrentUserId.Value);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new EmployeeRequestAlertItem
                        {
                            AlertID = Convert.ToInt32(dr[0]),
                            RequestID = Convert.ToInt32(dr[1]),
                            Title = dr.GetString(2),
                            Message = dr.IsDBNull(3) ? "" : dr.GetString(3),
                            LinkUrl = dr.IsDBNull(4) ? "" : dr.GetString(4),
                            IsRead = Convert.ToBoolean(dr[5]),
                            CreatedOn = Convert.ToDateTime(dr[6])
                        });
                    }
                }
            }
            return list;
        }

        public void MarkAlertsReadForRequest(int requestId)
        {
            if (!_auth.CurrentUserId.HasValue || requestId <= 0) return;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
UPDATE tblEmployeeRequestAlert SET IsRead=1
WHERE UserID=@UserID AND RequestID=@RequestID AND IsRead=0;", conn))
            {
                cmd.Parameters.AddWithValue("@UserID", _auth.CurrentUserId.Value);
                cmd.Parameters.AddWithValue("@RequestID", requestId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public int ResolveManagerEmployeeId(int employeeId)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                // Prefer Job.ReportsToEmployeeID
                using (var cmd = new SqlCommand(@"
SELECT TOP 1 j.ReportsToEmployeeID
FROM tblEmployee e
INNER JOIN tblJob j ON j.JobID = e.JobID
WHERE e.EmployeeID = @Id AND ISNULL(j.ReportsToEmployeeID,0) > 0;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", employeeId);
                    var r = cmd.ExecuteScalar();
                    if (r != null && r != DBNull.Value && Convert.ToInt32(r) > 0)
                        return Convert.ToInt32(r);
                }
                // Fallback: permanent responsible on employee
                using (var cmd = new SqlCommand(@"
SELECT TOP 1 ISNULL(PermanentResponsibleEmployeeID,0)
FROM tblEmployee WHERE EmployeeID=@Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", employeeId);
                    var r = cmd.ExecuteScalar();
                    if (r != null && r != DBNull.Value && Convert.ToInt32(r) > 0)
                        return Convert.ToInt32(r);
                }
            }
            return 0;
        }

        private bool CanViewRequest(EmployeeRequestItem item, int linkedEmployeeId, bool isAdmin, bool isHr, bool isMgmt)
        {
            if (isAdmin) return true;
            if (item.EmployeeID == linkedEmployeeId) return true;
            if (item.ManagerEmployeeID == linkedEmployeeId) return true;
            if (isHr && (item.Status == EmployeeRequestStatuses.PendingHRApproval
                         || item.Status == EmployeeRequestStatuses.PendingManagementApproval
                         || item.Status == EmployeeRequestStatuses.Approved
                         || item.Status == EmployeeRequestStatuses.Rejected))
                return true;
            if (isMgmt && (item.Status == EmployeeRequestStatuses.PendingManagementApproval
                           || item.Status == EmployeeRequestStatuses.Approved
                           || item.Status == EmployeeRequestStatuses.Rejected))
                return true;
            if (isHr || isMgmt) return true; // allow HR/Management to browse history
            return false;
        }

        private int RequireLinkedEmployee()
        {
            var id = GetLinkedEmployeeId();
            if (!id.HasValue || id.Value <= 0)
                throw new InvalidOperationException(EmployeeProfileAccessService.NotSynchronizedMessage);
            return id.Value;
        }

        private void UpdateStatus(SqlConnection conn, int requestId, string status, DateTime? completedOn)
        {
            using (var cmd = new SqlCommand(@"
UPDATE tblEmployeeRequest SET Status=@Status,
 CompletedOn=CASE WHEN @CompletedOn IS NULL THEN CompletedOn ELSE @CompletedOn END,
 ModifiedOn=GETDATE(), ModifiedByUserID=@UserID
WHERE RequestID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@CompletedOn", completedOn.HasValue ? (object)completedOn.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@UserID", (object)_auth.CurrentUserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Id", requestId);
                cmd.ExecuteNonQuery();
            }
        }

        private void LogAction(SqlConnection conn, int requestId, string stage, string type, string comments, int? employeeId)
        {
            using (var cmd = new SqlCommand(@"
INSERT INTO tblEmployeeRequestAction
 (RequestID, ActionStage, ActionType, Comments, ActionByUserID, ActionByEmployeeID, ActionByName, ActionOn)
VALUES
 (@RequestID, @ActionStage, @ActionType, @Comments, @UserID, @EmployeeID, @ActionByName, GETDATE());", conn))
            {
                cmd.Parameters.AddWithValue("@RequestID", requestId);
                cmd.Parameters.AddWithValue("@ActionStage", stage);
                cmd.Parameters.AddWithValue("@ActionType", type);
                cmd.Parameters.AddWithValue("@Comments", string.IsNullOrWhiteSpace(comments) ? (object)DBNull.Value : comments);
                cmd.Parameters.AddWithValue("@UserID", (object)_auth.CurrentUserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EmployeeID", employeeId.HasValue && employeeId.Value > 0 ? (object)employeeId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@ActionByName", string.IsNullOrWhiteSpace(_auth.CurrentUsername) ? "User" : _auth.CurrentUsername);
                cmd.ExecuteNonQuery();
            }
        }

        private void NotifyEmployeeUsers(SqlConnection conn, int employeeId, int requestId, string title, string message)
        {
            var userIds = GetUserIdsForEmployee(conn, employeeId);
            foreach (var uid in userIds)
                InsertAlert(conn, uid, requestId, title, message);
        }

        private string RequestInboxBody(EmployeeRequestItem item, string action, string statusDisplay)
        {
            var emp = ((item.EmployeeCode ?? "") + " " + (item.EmployeeName ?? "")).Trim();
            var refer = item.RequestNumber + (string.IsNullOrWhiteSpace(item.Subject) ? "" : " — " + item.Subject.Trim());
            return _inbox.FormatBody("Employee Requests", refer, emp, action, statusDisplay, _inbox.ActorName());
        }

        private void NotifyHrUsers(SqlConnection conn, int requestId, string requestNumber, string body)
        {
            foreach (var uid in GetUsersWithFormRight(conn, HrFormKey))
                InsertAlert(conn, uid, requestId, "HR approval required", body);
        }

        private void NotifyManagementUsers(SqlConnection conn, int requestId, string requestNumber, string body)
        {
            foreach (var uid in GetUsersWithFormRight(conn, ManagementFormKey))
                InsertAlert(conn, uid, requestId, "Management approval required", body);
        }

        private void NotifyCurrentActor(string title, string body, int requestId)
        {
            if (!_auth.CurrentUserId.HasValue) return;
            _inbox.Notify(_auth.CurrentUserId.Value, title, body, "/EmployeeRequest.aspx?editId=" + requestId,
                AppInboxService.TypeEmployeeRequest, requestId);
        }

        private void InsertAlert(SqlConnection conn, int userId, int requestId, string title, string message)
        {
            using (var cmd = new SqlCommand(@"
INSERT INTO tblEmployeeRequestAlert (UserID, RequestID, Title, Message, LinkUrl, IsRead, CreatedOn)
VALUES (@UserID, @RequestID, @Title, @Message, @LinkUrl, 0, GETDATE());", conn))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@RequestID", requestId);
                cmd.Parameters.AddWithValue("@Title", title);
                cmd.Parameters.AddWithValue("@Message", message ?? "");
                cmd.Parameters.AddWithValue("@LinkUrl", "/EmployeeRequest.aspx?editId=" + requestId);
                cmd.ExecuteNonQuery();
            }
            _inbox.Notify(userId, title, message, "/EmployeeRequest.aspx?editId=" + requestId,
                AppInboxService.TypeEmployeeRequest, requestId);
        }

        private List<int> GetUserIdsForEmployee(SqlConnection conn, int employeeId)
        {
            var ids = new List<int>();
            using (var cmd = new SqlCommand("SELECT UserID FROM tblEmployee WHERE EmployeeID=@Id AND ISNULL(UserID,0)>0;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", employeeId);
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        ids.Add(Convert.ToInt32(dr[0]));
                }
            }
            return ids;
        }

        private List<int> GetUsersWithFormRight(SqlConnection conn, string formKey)
        {
            var ids = new List<int>();
            using (var cmd = new SqlCommand(@"
SELECT DISTINCT u.UserID
FROM tblUser u
LEFT JOIN tblUserPermission p ON p.UserID = u.UserID AND p.FormKey = @FormKey
WHERE u.IsActive = 1 AND (u.IsAdmin = 1 OR ISNULL(p.CanRead,0)=1 OR ISNULL(p.CanWrite,0)=1);", conn))
            {
                cmd.Parameters.AddWithValue("@FormKey", formKey);
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        ids.Add(Convert.ToInt32(dr[0]));
                }
            }
            return ids;
        }

        private static void BindCommon(SqlCommand cmd, EmployeeRequestItem item)
        {
            cmd.Parameters.AddWithValue("@RequestCategory", item.RequestCategory);
            cmd.Parameters.AddWithValue("@Subject", item.Subject.Trim());
            cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(item.Description) ? (object)DBNull.Value : item.Description.Trim());
            cmd.Parameters.AddWithValue("@Amount", item.Amount.HasValue ? (object)item.Amount.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@StartDate", ParseDateOrDbNull(item.StartDate));
            cmd.Parameters.AddWithValue("@EndDate", ParseDateOrDbNull(item.EndDate));
            cmd.Parameters.AddWithValue("@DetailsJson", string.IsNullOrWhiteSpace(item.DetailsJson) ? (object)DBNull.Value : item.DetailsJson);
        }

        private static object ParseDateOrDbNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
            return DateTime.TryParse(value, out var d) ? (object)d.Date : DBNull.Value;
        }

        private string SerializeDetails(Dictionary<string, string> details)
        {
            if (details == null || details.Count == 0) return "";
            return _json.Serialize(details);
        }

        private Dictionary<string, string> DeserializeDetails(string json)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(json)) return dict;
            try
            {
                var raw = _json.Deserialize<Dictionary<string, object>>(json);
                if (raw == null) return dict;
                foreach (var kv in raw)
                    dict[kv.Key] = kv.Value?.ToString() ?? "";
            }
            catch { }
            return dict;
        }

        private EmployeeRequestItem ReadRequest(SqlDataReader dr)
        {
            return new EmployeeRequestItem
            {
                RequestID = Convert.ToInt32(dr["RequestID"]),
                RequestNumber = dr["RequestNumber"]?.ToString() ?? "",
                EmployeeID = Convert.ToInt32(dr["EmployeeID"]),
                EmployeeCode = dr["EmployeeCode"] == DBNull.Value ? "" : dr["EmployeeCode"].ToString() ?? "",
                EmployeeName = (dr["EmployeeName"]?.ToString() ?? "").Trim(),
                DepartmentName = dr["DepartmentName"] == DBNull.Value ? "" : dr["DepartmentName"].ToString() ?? "",
                RequestCategory = dr["RequestCategory"]?.ToString() ?? EmployeeRequestCategories.Other,
                Status = dr["Status"]?.ToString() ?? EmployeeRequestStatuses.Draft,
                Subject = dr["Subject"]?.ToString() ?? "",
                Description = dr["Description"] == DBNull.Value ? "" : dr["Description"].ToString() ?? "",
                Amount = dr["Amount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(dr["Amount"]),
                StartDate = dr["StartDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["StartDate"]).ToString("yyyy-MM-dd"),
                EndDate = dr["EndDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["EndDate"]).ToString("yyyy-MM-dd"),
                DetailsJson = dr["DetailsJson"] == DBNull.Value ? "" : dr["DetailsJson"].ToString() ?? "",
                ManagerEmployeeID = Convert.ToInt32(dr["ManagerEmployeeID"]),
                ManagerName = (dr["ManagerName"]?.ToString() ?? "").Trim(),
                SubmittedOn = dr["SubmittedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["SubmittedOn"]),
                CreatedOn = Convert.ToDateTime(dr["CreatedOn"])
            };
        }
    }
}
