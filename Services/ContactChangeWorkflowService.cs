using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Web.Script.Serialization;

namespace HRMS.Services
{
    public static class ContactWorkflowStatuses
    {
        public const string InProcess = "InProcess";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";

        public static string Display(string status)
        {
            if (string.Equals(status, InProcess, StringComparison.OrdinalIgnoreCase)) return "In Process";
            if (string.Equals(status, Approved, StringComparison.OrdinalIgnoreCase)) return "Approved";
            if (string.Equals(status, Rejected, StringComparison.OrdinalIgnoreCase)) return "Rejected";
            return string.IsNullOrEmpty(status) ? "Approved" : status;
        }

        public static string BadgeClass(string status)
        {
            if (string.Equals(status, InProcess, StringComparison.OrdinalIgnoreCase)) return "badge badge-warning";
            if (string.Equals(status, Approved, StringComparison.OrdinalIgnoreCase)) return "badge badge-success";
            if (string.Equals(status, Rejected, StringComparison.OrdinalIgnoreCase)) return "badge badge-danger";
            return "badge badge-info";
        }
    }

    public static class ContactVerification
    {
        public const string ToVerify = "ToVerify";
        public const string Verified = "Verified";

        public static string Display(string status)
        {
            if (string.Equals(status, Verified, StringComparison.OrdinalIgnoreCase)) return "Verified";
            return "Pending";
        }

        public static string BadgeClass(string status)
        {
            if (string.Equals(status, Verified, StringComparison.OrdinalIgnoreCase)) return "badge badge-success";
            return "badge badge-warning";
        }
    }

    public static class ContactWorkflowEntities
    {
        public const string Contact = "Contact";
        public const string Address = "Address";
    }

    public class ContactChangeLine
    {
        public string FieldLabel { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
    }

    public class ContactChangeHistoryItem
    {
        public int RequestID { get; set; }
        public string RequestCode { get; set; } = "";
        public string EntityType { get; set; } = "";
        public string ActionType { get; set; } = "";
        public int AddressID { get; set; }
        public string Status { get; set; } = "";
        public string ActorName { get; set; } = "";
        public string Comments { get; set; } = "";
        public DateTime ActionOn { get; set; }
        public string StatusDisplay => ContactWorkflowStatuses.Display(Status);
        public string EntityDisplay => string.Equals(EntityType, ContactWorkflowEntities.Address, StringComparison.OrdinalIgnoreCase)
            ? "Address" : "Contact";
    }

    public class ContactChangeRequest
    {
        public int RequestID { get; set; }
        public string RequestCode { get; set; } = "";
        public int ContactID { get; set; }
        public string ContactCode { get; set; } = "";
        public string ContactName { get; set; } = "";
        public string ActionType { get; set; } = "";
        public int SubmittedByUserID { get; set; }
        public string SubmittedByName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime SubmittedOn { get; set; }
        public DateTime? DecidedOn { get; set; }
        public int? DecidedByUserID { get; set; }
        public string DecidedByName { get; set; } = "";
        public string DecisionComments { get; set; } = "";
        public string OldJson { get; set; } = "";
        public string NewJson { get; set; } = "";
        public string EntityType { get; set; } = ContactWorkflowEntities.Contact;
        public int AddressID { get; set; }
        public List<ContactChangeLine> Lines { get; set; } = new List<ContactChangeLine>();
        public List<ContactChangeHistoryItem> History { get; set; } = new List<ContactChangeHistoryItem>();
        public string StatusDisplay => ContactWorkflowStatuses.Display(Status);
        public string BadgeClass => ContactWorkflowStatuses.BadgeClass(Status);
        public string EntityDisplay => string.Equals(EntityType, ContactWorkflowEntities.Address, StringComparison.OrdinalIgnoreCase)
            ? "Address" : "Contact";
        public string ActionDisplay => ActionType + " " + EntityDisplay;
    }

    public class ContactChangeWorkflowService
    {
        public const string FormKey = "ContactMaster";
        public const string RelatedType = "ContactChange";

        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();
        private readonly PermissionService _perms = new PermissionService();
        private readonly AppInboxService _inbox = new AppInboxService();
        private readonly AuditService _audit = new AuditService();
        private readonly ContactAddressService _addresses = new ContactAddressService();
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public ContactChangeWorkflowService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public void EnsureSchema()
        {
            if (string.IsNullOrWhiteSpace(_conn)) return;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                Exec(conn, @"
IF COL_LENGTH(N'dbo.tblContactMaster', N'WorkflowStatus') IS NULL
    ALTER TABLE dbo.tblContactMaster ADD WorkflowStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_tblContact_Workflow DEFAULT N'Approved';");
                Exec(conn, @"
IF OBJECT_ID(N'dbo.tblContactChangeRequest','U') IS NULL
CREATE TABLE dbo.tblContactChangeRequest (
    RequestID INT IDENTITY(1,1) PRIMARY KEY,
    RequestCode NVARCHAR(20) NOT NULL,
    ContactID INT NOT NULL,
    ActionType NVARCHAR(20) NOT NULL,
    SubmittedByUserID INT NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    SubmittedOn DATETIME NOT NULL DEFAULT GETDATE(),
    DecidedOn DATETIME NULL,
    DecidedByUserID INT NULL,
    DecisionComments NVARCHAR(1000) NULL,
    OldJson NVARCHAR(MAX) NULL,
    NewJson NVARCHAR(MAX) NULL,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE()
);");
                Exec(conn, @"
IF OBJECT_ID(N'dbo.tblContactChangeLine','U') IS NULL
CREATE TABLE dbo.tblContactChangeLine (
    LineID INT IDENTITY(1,1) PRIMARY KEY,
    RequestID INT NOT NULL,
    FieldLabel NVARCHAR(80) NOT NULL,
    OldValue NVARCHAR(500) NULL,
    NewValue NVARCHAR(500) NULL
);");
                Exec(conn, @"
IF OBJECT_ID(N'dbo.tblContactChangeHistory','U') IS NULL
CREATE TABLE dbo.tblContactChangeHistory (
    HistoryID INT IDENTITY(1,1) PRIMARY KEY,
    RequestID INT NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    ActorUserID INT NOT NULL,
    Comments NVARCHAR(1000) NULL,
    ActionOn DATETIME NOT NULL DEFAULT GETDATE()
);");
                Exec(conn, @"
IF OBJECT_ID(N'dbo.tblContactChangeRequest','U') IS NOT NULL
AND COL_LENGTH(N'dbo.tblContactChangeRequest', N'EntityType') IS NULL
    ALTER TABLE dbo.tblContactChangeRequest ADD EntityType NVARCHAR(20) NOT NULL CONSTRAINT DF_tblContactChange_Entity DEFAULT N'Contact';");
                Exec(conn, @"
IF OBJECT_ID(N'dbo.tblContactChangeRequest','U') IS NOT NULL
AND COL_LENGTH(N'dbo.tblContactChangeRequest', N'AddressID') IS NULL
    ALTER TABLE dbo.tblContactChangeRequest ADD AddressID INT NULL;");
                Exec(conn, @"
IF OBJECT_ID(N'dbo.tblContactAddress','U') IS NOT NULL
AND COL_LENGTH(N'dbo.tblContactAddress', N'WorkflowStatus') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD WorkflowStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_tblContactAddr_Workflow DEFAULT N'Approved';");
                Exec(conn, @"
IF OBJECT_ID(N'dbo.tblContactAddress','U') IS NOT NULL
AND COL_LENGTH(N'dbo.tblContactAddress', N'VerificationStatus') IS NULL
    ALTER TABLE dbo.tblContactAddress ADD VerificationStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_tblContactAddr_Verify DEFAULT N'Verified';");
            }
        }

        public bool CanReview()
            => _auth.IsAdmin || _perms.CanApprove(FormKey) || _perms.CanWrite(FormKey);

        public bool CanApproveRequest(ContactChangeRequest request)
        {
            if (request == null || request.Status != ContactWorkflowStatuses.InProcess) return false;
            if (!CanReview() || !_auth.CurrentUserId.HasValue) return false;
            if (request.SubmittedByUserID == _auth.CurrentUserId.Value && !_auth.IsAdmin) return false;
            return true;
        }

        public ContactChangeRequest GetPending(int contactId)
        {
            return LoadPending(contactId, 0, ContactWorkflowEntities.Contact);
        }

        public ContactChangeRequest GetPendingAddress(int addressId)
        {
            return LoadPending(0, addressId, ContactWorkflowEntities.Address);
        }

        public bool HasPendingForContact(int contactId)
        {
            if (contactId <= 0) return false;
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT TOP 1 RequestID FROM tblContactChangeRequest
WHERE ContactID=@Id AND Status=@Status;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", contactId);
                cmd.Parameters.AddWithValue("@Status", ContactWorkflowStatuses.InProcess);
                conn.Open();
                var id = cmd.ExecuteScalar();
                return id != null && id != DBNull.Value;
            }
        }

        private ContactChangeRequest LoadPending(int contactId, int addressId, string entityType)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT TOP 1 RequestID FROM tblContactChangeRequest
WHERE Status=@Status AND ISNULL(EntityType,N'Contact')=@Entity
  AND ((@AddressID>0 AND AddressID=@AddressID) OR (@AddressID=0 AND ContactID=@ContactID))
ORDER BY SubmittedOn DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@Status", ContactWorkflowStatuses.InProcess);
                cmd.Parameters.AddWithValue("@Entity", entityType ?? ContactWorkflowEntities.Contact);
                cmd.Parameters.AddWithValue("@AddressID", addressId);
                cmd.Parameters.AddWithValue("@ContactID", contactId);
                conn.Open();
                var id = cmd.ExecuteScalar();
                if (id == null || id == DBNull.Value) return null;
                return GetRequest(Convert.ToInt32(id));
            }
        }

        public string Submit(ContactInput proposed, bool editMode,
            Action<SqlConnection, ContactInput, bool> persist, out int requestId)
        {
            requestId = 0;
            if (proposed == null) return "Contact data is missing.";
            if (!_auth.CurrentUserId.HasValue) return "Sign in again to submit this request.";
            EnsureSchema();

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                if (!editMode)
                {
                    ForcePendingVerification(proposed);
                    persist(conn, proposed, false);
                    var lines = BuildLines(null, proposed);
                    requestId = InsertRequest(conn, proposed.ContactID, "Create", "", Serialize(proposed), lines,
                        "New contact submitted for approval.", ContactWorkflowEntities.Contact, 0);
                    SetContactWorkflow(conn, proposed.ContactID, ContactWorkflowStatuses.InProcess, ContactVerification.ToVerify);
                }
                else
                {
                    var pending = GetPending(proposed.ContactID);
                    if (pending != null)
                    {
                        if (pending.SubmittedByUserID != _auth.CurrentUserId.Value && !CanReview())
                            return "Contact " + pending.RequestCode + " is already in process. Wait for approval before changing it again.";
                        if (string.Equals(pending.ActionType, "Create", StringComparison.OrdinalIgnoreCase))
                        {
                            ForcePendingVerification(proposed);
                            persist(conn, proposed, true);
                            ReplaceProposal(conn, pending.RequestID, Serialize(proposed), BuildLines(Deserialize(pending.OldJson), proposed),
                                "Contact details updated while the create request was in process.");
                            requestId = pending.RequestID;
                        }
                        else
                        {
                            ForcePendingVerification(proposed);
                            var live = LoadLive(conn, proposed.ContactID);
                            ReplaceProposal(conn, pending.RequestID, Serialize(proposed), BuildLines(live, proposed),
                                "Proposed changes updated while the request was in process.");
                            requestId = pending.RequestID;
                        }
                    }
                    else
                    {
                        var live = LoadLive(conn, proposed.ContactID);
                        ForcePendingVerification(proposed);
                        var lines = BuildLines(live, proposed);
                        if (lines.Count == 0)
                            return "No contact information changes were detected.";
                        requestId = InsertRequest(conn, proposed.ContactID, "Update", Serialize(live), Serialize(proposed), lines,
                            "Contact change submitted for approval. Live values stay unchanged until approved.",
                            ContactWorkflowEntities.Contact, 0);
                        SetContactWorkflow(conn, proposed.ContactID, ContactWorkflowStatuses.InProcess, ContactVerification.ToVerify);
                    }
                }
            }

            var req = GetRequest(requestId);
            NotifySubmit(req);
            WriteAudit("ContactChangeSubmitted", req ?? new ContactChangeRequest
            {
                ContactID = proposed.ContactID,
                ContactCode = proposed.ContactCode,
                ActionType = editMode ? "Update" : "Create",
                EntityType = ContactWorkflowEntities.Contact,
                Status = ContactWorkflowStatuses.InProcess
            }, ContactWorkflowStatuses.InProcess, "Submitted", "Submit");
            return null;
        }

        public string SubmitAddress(ContactAddressItem proposed, out int requestId)
        {
            requestId = 0;
            if (proposed == null) return "Address data is missing.";
            if (!_auth.CurrentUserId.HasValue) return "Sign in again to submit this request.";
            var validate = _addresses.ValidateAddress(proposed);
            if (!string.IsNullOrEmpty(validate)) return validate;
            EnsureSchema();

            var contactPending = GetPending(proposed.ContactID);
            if (contactPending != null && string.Equals(contactPending.ActionType, "Create", StringComparison.OrdinalIgnoreCase))
                return "Finish the contact create approval before submitting addresses.";

            proposed.WorkflowStatus = ContactWorkflowStatuses.InProcess;
            proposed.VerificationStatus = ContactVerification.ToVerify;
            _addresses.FillLabels(proposed);
            var isCreate = proposed.AddressID <= 0;

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                try
                {
                    if (isCreate)
                    {
                        _addresses.InsertAddress(conn, proposed, _auth.CurrentUserId);
                        var lines = BuildAddressLines(null, proposed);
                        requestId = InsertRequest(conn, proposed.ContactID, "Create", "", SerializeAddress(proposed), lines,
                            "New contact address submitted for approval.", ContactWorkflowEntities.Address, proposed.AddressID);
                    }
                    else
                    {
                        var pending = GetPendingAddress(proposed.AddressID);
                        if (pending != null)
                        {
                            if (pending.SubmittedByUserID != _auth.CurrentUserId.Value && !CanReview())
                                return "Address " + pending.RequestCode + " is already in process. Wait for approval before changing it again.";
                            if (string.Equals(pending.ActionType, "Delete", StringComparison.OrdinalIgnoreCase))
                                return "This address has a delete request in process.";
                            if (string.Equals(pending.ActionType, "Create", StringComparison.OrdinalIgnoreCase))
                            {
                                _addresses.UpdateAddressLive(conn, null, proposed, _auth.CurrentUserId);
                                ReplaceProposal(conn, pending.RequestID, SerializeAddress(proposed),
                                    BuildAddressLines(DeserializeAddress(pending.OldJson), proposed),
                                    "Address details updated while the create request was in process.");
                                requestId = pending.RequestID;
                            }
                            else
                            {
                                var live = _addresses.GetAddress(proposed.AddressID);
                                ReplaceProposal(conn, pending.RequestID, SerializeAddress(proposed),
                                    BuildAddressLines(live, proposed),
                                    "Proposed address changes updated while the request was in process.");
                                requestId = pending.RequestID;
                            }
                        }
                        else
                        {
                            var live = _addresses.GetAddress(proposed.AddressID);
                            var lines = BuildAddressLines(live, proposed);
                            if (lines.Count == 0)
                                return "No address information changes were detected.";
                            requestId = InsertRequest(conn, proposed.ContactID, "Update", SerializeAddress(live), SerializeAddress(proposed),
                                lines, "Address change submitted for approval. Live values stay unchanged until approved.",
                                ContactWorkflowEntities.Address, proposed.AddressID);
                            _addresses.SetWorkflowState(conn, null, proposed.AddressID,
                                ContactWorkflowStatuses.InProcess, ContactVerification.ToVerify);
                        }
                    }
                }
                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
                {
                    return "This contact already has a primary, billing, or shipping address of that type. Please recorrect the flags.";
                }
            }

            var req = GetRequest(requestId);
            NotifySubmit(req);
            WriteAudit("ContactAddressSubmitted", req, ContactWorkflowStatuses.InProcess, "Submitted", "SubmitAddress");
            return null;
        }

        public string SubmitAddressDelete(int addressId, int contactId, out int requestId)
        {
            requestId = 0;
            if (addressId <= 0 || contactId <= 0) return "Address was not found.";
            if (!_auth.CurrentUserId.HasValue) return "Sign in again to submit this request.";
            EnsureSchema();
            var contactPending = GetPending(contactId);
            if (contactPending != null && string.Equals(contactPending.ActionType, "Create", StringComparison.OrdinalIgnoreCase))
                return "Finish the contact create approval before changing addresses.";
            if (GetPendingAddress(addressId) != null)
                return "This address already has an in-process approval request.";

            var live = _addresses.GetAddress(addressId);
            if (live == null || live.ContactID != contactId) return "Address was not found.";
            var lines = BuildAddressLines(live, null);
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                requestId = InsertRequest(conn, contactId, "Delete", SerializeAddress(live), "", lines,
                    "Address delete submitted for approval.", ContactWorkflowEntities.Address, addressId);
                _addresses.SetWorkflowState(conn, null, addressId, ContactWorkflowStatuses.InProcess, ContactVerification.ToVerify);
            }
            var req = GetRequest(requestId);
            NotifySubmit(req);
            WriteAudit("ContactAddressSubmitted", req, ContactWorkflowStatuses.InProcess, "Submitted", "DeleteAddress");
            return null;
        }

        public string Decide(int requestId, bool approve, string comments)
        {
            EnsureSchema();
            var request = GetRequest(requestId);
            if (request == null) return "Request not found.";
            if (!CanApproveRequest(request))
                return request.SubmittedByUserID == _auth.CurrentUserId
                    ? "You cannot approve or reject your own contact request unless you are an administrator."
                    : "You are not authorized to review this request.";

            comments = (comments ?? "").Trim();
            if (!approve && string.IsNullOrWhiteSpace(comments))
                return "Enter remarks when rejecting a contact request.";

            var newStatus = approve ? ContactWorkflowStatuses.Approved : ContactWorkflowStatuses.Rejected;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        if (approve)
                            ApplyApproved(conn, tx, request);
                        else
                            ApplyRejected(conn, tx, request);

                        using (var upd = new SqlCommand(@"
UPDATE tblContactChangeRequest
SET Status=@Status, DecidedOn=GETDATE(), DecidedByUserID=@UserID, DecisionComments=@Comments
WHERE RequestID=@ID AND Status=@Pending;", conn, tx))
                        {
                            upd.Parameters.AddWithValue("@Status", newStatus);
                            upd.Parameters.AddWithValue("@UserID", _auth.CurrentUserId.Value);
                            upd.Parameters.AddWithValue("@Comments", string.IsNullOrEmpty(comments) ? (object)DBNull.Value : comments);
                            upd.Parameters.AddWithValue("@ID", requestId);
                            upd.Parameters.AddWithValue("@Pending", ContactWorkflowStatuses.InProcess);
                            if (upd.ExecuteNonQuery() != 1)
                            {
                                tx.Rollback();
                                return "This request is no longer in process.";
                            }
                        }
                        InsertHistory(conn, tx, requestId, newStatus, comments);
                        tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        return "Could not complete the workflow action. " + ex.Message;
                    }
                }
            }

            var msg = approve
                ? "Your " + request.EntityDisplay.ToLowerInvariant() + " request " + request.RequestCode + " was approved."
                : "Your " + request.EntityDisplay.ToLowerInvariant() + " request " + request.RequestCode + " was rejected. Remarks: " + comments;
            _inbox.Notify(request.SubmittedByUserID,
                approve ? request.EntityDisplay + " request approved" : request.EntityDisplay + " request rejected",
                _inbox.FormatBody("Contact Hub", request.RequestCode, request.ContactName,
                    approve ? "Approved" : "Rejected", ContactWorkflowStatuses.Display(newStatus), _inbox.ActorName())
                    + (string.IsNullOrEmpty(comments) ? "" : "\nRemarks: " + comments),
                "/ContactChangeReview.aspx?id=" + requestId, AppInboxService.TypeContactChange, requestId);
            if (_auth.CurrentUserId.HasValue && _auth.CurrentUserId.Value != request.SubmittedByUserID)
            {
                _inbox.Notify(_auth.CurrentUserId.Value,
                    request.EntityDisplay + " request " + (approve ? "approved" : "rejected"),
                    msg, "/ContactChangeReview.aspx?id=" + requestId, AppInboxService.TypeContactChange, requestId);
            }
            _inbox.MarkRelatedRead(AppInboxService.TypeContactChange, requestId, _auth.CurrentUserId ?? 0);
            var decided = GetRequest(requestId) ?? request;
            WriteAudit(approve ? "ContactChangeApproved" : "ContactChangeRejected", decided, newStatus, comments, "Decide");
            return null;
        }

        public ContactChangeRequest GetRequest(int requestId)
        {
            EnsureSchema();
            ContactChangeRequest item = null;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT r.RequestID, r.RequestCode, r.ContactID, r.ActionType, r.SubmittedByUserID, r.Status, r.SubmittedOn,
       r.DecidedOn, r.DecidedByUserID, r.DecisionComments, ISNULL(r.OldJson,''), ISNULL(r.NewJson,''),
       ISNULL(c.ContactCode,''), ISNULL(c.Name,''),
       ISNULL(su.FullName, su.Username), ISNULL(du.FullName, du.Username),
       ISNULL(r.EntityType,N'Contact'), ISNULL(r.AddressID,0)
FROM tblContactChangeRequest r
LEFT JOIN tblContactMaster c ON c.ContactID=r.ContactID
LEFT JOIN tblUser su ON su.UserID=r.SubmittedByUserID
LEFT JOIN tblUser du ON du.UserID=r.DecidedByUserID
WHERE r.RequestID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", requestId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    item = new ContactChangeRequest
                    {
                        RequestID = dr.GetInt32(0),
                        RequestCode = dr.GetString(1),
                        ContactID = dr.GetInt32(2),
                        ActionType = dr.GetString(3),
                        SubmittedByUserID = dr.GetInt32(4),
                        Status = dr.GetString(5),
                        SubmittedOn = dr.GetDateTime(6),
                        DecidedOn = dr.IsDBNull(7) ? (DateTime?)null : dr.GetDateTime(7),
                        DecidedByUserID = dr.IsDBNull(8) ? (int?)null : dr.GetInt32(8),
                        DecisionComments = dr.IsDBNull(9) ? "" : dr.GetString(9),
                        OldJson = dr.GetString(10),
                        NewJson = dr.GetString(11),
                        ContactCode = dr.GetString(12),
                        ContactName = dr.GetString(13),
                        SubmittedByName = dr.IsDBNull(14) ? "" : dr.GetString(14),
                        DecidedByName = dr.IsDBNull(15) ? "" : dr.GetString(15),
                        EntityType = dr.FieldCount > 16 && !dr.IsDBNull(16) ? dr.GetString(16) : ContactWorkflowEntities.Contact,
                        AddressID = dr.FieldCount > 17 && !dr.IsDBNull(17) ? Convert.ToInt32(dr.GetValue(17)) : 0
                    };
                }
            }
            item.Lines = LoadLines(requestId);
            item.History = LoadHistory(requestId);
            return item;
        }

        public bool CanViewRequest(ContactChangeRequest request)
        {
            if (request == null || !_auth.CurrentUserId.HasValue) return false;
            if (CanReview()) return true;
            return request.SubmittedByUserID == _auth.CurrentUserId.Value;
        }

        public List<ContactChangeRequest> ListPending()
        {
            EnsureSchema();
            var list = new List<ContactChangeRequest>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT r.RequestID, r.RequestCode, r.ContactID, r.ActionType, r.SubmittedByUserID, r.Status, r.SubmittedOn,
       ISNULL(c.ContactCode,''), ISNULL(c.Name,''), ISNULL(su.FullName, su.Username),
       ISNULL(r.EntityType,N'Contact'), ISNULL(r.AddressID,0)
FROM tblContactChangeRequest r
LEFT JOIN tblContactMaster c ON c.ContactID=r.ContactID
LEFT JOIN tblUser su ON su.UserID=r.SubmittedByUserID
WHERE r.Status=@Status
ORDER BY r.SubmittedOn ASC;", conn))
            {
                cmd.Parameters.AddWithValue("@Status", ContactWorkflowStatuses.InProcess);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new ContactChangeRequest
                        {
                            RequestID = dr.GetInt32(0),
                            RequestCode = dr.GetString(1),
                            ContactID = dr.GetInt32(2),
                            ActionType = dr.GetString(3),
                            SubmittedByUserID = dr.GetInt32(4),
                            Status = dr.GetString(5),
                            SubmittedOn = dr.GetDateTime(6),
                            ContactCode = dr.GetString(7),
                            ContactName = dr.GetString(8),
                            SubmittedByName = dr.GetString(9),
                            EntityType = dr.FieldCount > 10 && !dr.IsDBNull(10) ? dr.GetString(10) : ContactWorkflowEntities.Contact,
                            AddressID = dr.FieldCount > 11 && !dr.IsDBNull(11) ? Convert.ToInt32(dr.GetValue(11)) : 0
                        });
                    }
                }
            }
            return list;
        }

        public List<ContactChangeRequest> ListRequestsForContact(int contactId)
        {
            var list = new List<ContactChangeRequest>();
            if (contactId <= 0) return list;
            EnsureSchema();
            var ids = new List<int>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT RequestID FROM tblContactChangeRequest
WHERE ContactID=@Id
ORDER BY SubmittedOn DESC, RequestID DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", contactId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read()) ids.Add(dr.GetInt32(0));
            }
            foreach (var id in ids)
            {
                var item = GetRequest(id);
                if (item != null) list.Add(item);
            }
            return list;
        }

        public List<ContactChangeRequest> ListRecent(int take)
        {
            var list = new List<ContactChangeRequest>();
            EnsureSchema();
            if (take <= 0) take = 40;
            if (take > 100) take = 100;
            var ids = new List<int>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT TOP " + take + @" RequestID FROM tblContactChangeRequest
ORDER BY SubmittedOn DESC, RequestID DESC;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read()) ids.Add(dr.GetInt32(0));
            }
            foreach (var id in ids)
            {
                var item = GetRequest(id);
                if (item != null) list.Add(item);
            }
            return list;
        }

        public List<AuditEntry> AuditTrailForContact(int contactId, IList<int> addressIds)
        {
            var ids = new List<int>();
            if (addressIds != null)
                foreach (var id in addressIds)
                    if (id > 0) ids.Add(id);
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT ISNULL(AddressID,0) FROM tblContactChangeRequest WHERE ContactID=@Id AND ISNULL(AddressID,0)>0;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", contactId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                    {
                        var aid = dr.GetInt32(0);
                        if (aid > 0 && !ids.Contains(aid)) ids.Add(aid);
                    }
            }
            return _audit.ListContactHub(contactId, ids);
        }

        public List<ContactChangeHistoryItem> HistoryForContact(int contactId)
        {
            var list = new List<ContactChangeHistoryItem>();
            if (contactId <= 0) return list;
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT h.Status, ISNULL(u.FullName, u.Username), ISNULL(h.Comments,''), h.ActionOn,
       r.RequestID, ISNULL(r.RequestCode,''), ISNULL(r.EntityType,N'Contact'), ISNULL(r.ActionType,''), ISNULL(r.AddressID,0)
FROM tblContactChangeHistory h
INNER JOIN tblContactChangeRequest r ON r.RequestID=h.RequestID
LEFT JOIN tblUser u ON u.UserID=h.ActorUserID
WHERE r.ContactID=@Id
ORDER BY h.ActionOn DESC, h.HistoryID DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", contactId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new ContactChangeHistoryItem
                        {
                            Status = dr.GetString(0),
                            ActorName = dr.IsDBNull(1) ? "" : dr.GetString(1),
                            Comments = dr.GetString(2),
                            ActionOn = dr.GetDateTime(3),
                            RequestID = dr.GetInt32(4),
                            RequestCode = dr.GetString(5),
                            EntityType = dr.FieldCount > 6 && !dr.IsDBNull(6) ? dr.GetString(6) : ContactWorkflowEntities.Contact,
                            ActionType = dr.FieldCount > 7 && !dr.IsDBNull(7) ? dr.GetString(7) : "",
                            AddressID = dr.FieldCount > 8 && !dr.IsDBNull(8) ? Convert.ToInt32(dr.GetValue(8)) : 0
                        });
                    }
                }
            }
            return list;
        }

        public ContactInput OverlayPendingUpdate(ContactInput live)
        {
            if (live == null || live.ContactID <= 0) return live;
            var pending = GetPending(live.ContactID);
            if (pending == null || !string.Equals(pending.ActionType, "Update", StringComparison.OrdinalIgnoreCase))
                return live;
            var proposed = Deserialize(pending.NewJson);
            if (proposed == null) return live;
            proposed.ContactID = live.ContactID;
            proposed.ContactCode = live.ContactCode;
            proposed.WorkflowStatus = ContactWorkflowStatuses.InProcess;
            proposed.VerificationStatus = ContactVerification.ToVerify;
            return proposed;
        }

        public ContactAddressItem OverlayPendingAddress(ContactAddressItem live)
        {
            if (live == null || live.AddressID <= 0) return live;
            var pending = GetPendingAddress(live.AddressID);
            if (pending == null || !string.Equals(pending.ActionType, "Update", StringComparison.OrdinalIgnoreCase))
                return live;
            var proposed = DeserializeAddress(pending.NewJson);
            if (proposed == null) return live;
            proposed.AddressID = live.AddressID;
            proposed.ContactID = live.ContactID;
            proposed.WorkflowStatus = ContactWorkflowStatuses.InProcess;
            proposed.VerificationStatus = ContactVerification.ToVerify;
            _addresses.FillLabels(proposed);
            return proposed;
        }

        private void ApplyApproved(SqlConnection conn, SqlTransaction tx, ContactChangeRequest request)
        {
            if (string.Equals(request.EntityType, ContactWorkflowEntities.Address, StringComparison.OrdinalIgnoreCase))
            {
                ApplyApprovedAddress(conn, tx, request);
                return;
            }
            if (string.Equals(request.ActionType, "Update", StringComparison.OrdinalIgnoreCase))
            {
                var proposed = Deserialize(request.NewJson);
                if (proposed == null) throw new InvalidOperationException("Proposed contact values are missing.");
                proposed.ContactID = request.ContactID;
                proposed.WorkflowStatus = ContactWorkflowStatuses.Approved;
                proposed.VerificationStatus = ContactVerification.Verified;
                UpdateLive(conn, tx, proposed);
            }
            else
            {
                SetContactWorkflow(conn, tx, request.ContactID, ContactWorkflowStatuses.Approved, ContactVerification.Verified);
            }
        }

        private void ApplyRejected(SqlConnection conn, SqlTransaction tx, ContactChangeRequest request)
        {
            if (string.Equals(request.EntityType, ContactWorkflowEntities.Address, StringComparison.OrdinalIgnoreCase))
            {
                ApplyRejectedAddress(conn, tx, request);
                return;
            }
            var inactive = string.Equals(request.ActionType, "Create", StringComparison.OrdinalIgnoreCase);
            using (var cmd = new SqlCommand(@"
UPDATE tblContactMaster SET WorkflowStatus=@S, VerificationStatus=@V,
 ContactStatus=CASE WHEN @Inactive=1 THEN N'Inactive' ELSE ContactStatus END,
 ModifiedOn=GETDATE()
WHERE ContactID=@Id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@S", ContactWorkflowStatuses.Rejected);
                cmd.Parameters.AddWithValue("@V", ContactVerification.ToVerify);
                cmd.Parameters.AddWithValue("@Inactive", inactive ? 1 : 0);
                cmd.Parameters.AddWithValue("@Id", request.ContactID);
                cmd.ExecuteNonQuery();
            }
        }

        private void ApplyApprovedAddress(SqlConnection conn, SqlTransaction tx, ContactChangeRequest request)
        {
            if (string.Equals(request.ActionType, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                _addresses.DeleteAddress(conn, tx, request.AddressID, request.ContactID);
                return;
            }
            if (string.Equals(request.ActionType, "Update", StringComparison.OrdinalIgnoreCase))
            {
                var proposed = DeserializeAddress(request.NewJson);
                if (proposed == null) throw new InvalidOperationException("Proposed address values are missing.");
                proposed.AddressID = request.AddressID;
                proposed.ContactID = request.ContactID;
                proposed.WorkflowStatus = ContactWorkflowStatuses.Approved;
                proposed.VerificationStatus = ContactVerification.Verified;
                _addresses.UpdateAddressLive(conn, tx, proposed, _auth.CurrentUserId);
                return;
            }
            _addresses.SetWorkflowState(conn, tx, request.AddressID, ContactWorkflowStatuses.Approved, ContactVerification.Verified);
        }

        private void ApplyRejectedAddress(SqlConnection conn, SqlTransaction tx, ContactChangeRequest request)
        {
            _addresses.SetWorkflowState(conn, tx, request.AddressID, ContactWorkflowStatuses.Rejected, ContactVerification.ToVerify);
        }

        private void UpdateLive(SqlConnection conn, SqlTransaction tx, ContactInput input)
        {
            using (var cmd = new SqlCommand(@"
UPDATE tblContactMaster SET CustomerID=@CustomerID, VendorID=@VendorID, EmployeeID=@EmployeeID, GuestName=@GuestName,
  ContactFor=@ContactFor, ContactType=@ContactType, ContactStatus=@ContactStatus, Name=@Name, SearchName=@SearchName,
  GenderID=@GenderID, MaritalStatus=@MaritalStatus, ProfessionalTitle=@ProfessionalTitle, Department=@Department,
  OfficeLocation=@OfficeLocation, AvailableFrom=@AvailableFrom, AvailableTo=@AvailableTo,
  ReportToManagerName=@ReportToManagerName, Phone=@Phone, Mobile=@Mobile, Email=@Email, Whatsapp=@Whatsapp, URL=@URL, Fax=@Fax,
  RecordType=@RecordType, VerificationStatus=@VerificationStatus, AccessStatus=@AccessStatus,
  WorkflowStatus=@WorkflowStatus, ModifiedOn=GETDATE()
WHERE ContactID=@ContactID;", conn, tx))
            {
                Bind(cmd, input);
                cmd.Parameters.AddWithValue("@ContactID", input.ContactID);
                cmd.ExecuteNonQuery();
            }
        }

        private ContactInput LoadLive(SqlConnection conn, int contactId)
        {
            using (var cmd = new SqlCommand(@"
SELECT ContactID, ContactCode, ISNULL(CustomerID,0), ISNULL(VendorID,0), ISNULL(EmployeeID,0), ISNULL(GuestName,''),
 ISNULL(ContactFor,''), ContactType, ContactStatus, Name, ISNULL(SearchName,''), ISNULL(GenderID,0), ISNULL(MaritalStatus,''),
 ISNULL(ProfessionalTitle,''), ISNULL(Department,''), ISNULL(OfficeLocation,''), AvailableFrom, AvailableTo,
 ISNULL(ReportToManagerName,''), ISNULL(Phone,''), ISNULL(Mobile,''), ISNULL(Email,''), ISNULL(Whatsapp,''), ISNULL(URL,''), ISNULL(Fax,''),
 ISNULL(RecordType,N'People'), ISNULL(VerificationStatus,N'Verified'), ISNULL(AccessStatus,N'Open'),
 ISNULL(WorkflowStatus,N'Approved')
FROM tblContactMaster WHERE ContactID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", contactId);
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    return new ContactInput
                    {
                        ContactID = dr.GetInt32(0),
                        ContactCode = dr.GetString(1),
                        CustomerID = Convert.ToInt32(dr.GetValue(2)),
                        VendorID = Convert.ToInt32(dr.GetValue(3)),
                        EmployeeID = Convert.ToInt32(dr.GetValue(4)),
                        GuestName = dr.GetString(5),
                        ContactFor = dr.GetString(6),
                        ContactType = dr.GetString(7),
                        ContactStatus = dr.GetString(8),
                        Name = dr.GetString(9),
                        SearchName = dr.GetString(10),
                        GenderID = Convert.ToInt32(dr.GetValue(11)),
                        MaritalStatus = dr.GetString(12),
                        ProfessionalTitle = dr.GetString(13),
                        Department = dr.GetString(14),
                        OfficeLocation = dr.GetString(15),
                        AvailableFrom = FormatTime(dr.IsDBNull(16) ? null : dr.GetValue(16)),
                        AvailableTo = FormatTime(dr.IsDBNull(17) ? null : dr.GetValue(17)),
                        ReportToManagerName = dr.GetString(18),
                        Phone = dr.GetString(19),
                        Mobile = dr.GetString(20),
                        Email = dr.GetString(21),
                        Whatsapp = dr.GetString(22),
                        URL = dr.GetString(23),
                        Fax = dr.GetString(24),
                        RecordType = dr.GetString(25),
                        VerificationStatus = dr.GetString(26),
                        AccessStatus = dr.GetString(27),
                        WorkflowStatus = dr.GetString(28)
                    };
                }
            }
        }

        private static void Bind(SqlCommand cmd, ContactInput input)
        {
            cmd.Parameters.AddWithValue("@CustomerID", input.CustomerID > 0 ? (object)input.CustomerID : DBNull.Value);
            cmd.Parameters.AddWithValue("@VendorID", input.VendorID > 0 ? (object)input.VendorID : DBNull.Value);
            cmd.Parameters.AddWithValue("@EmployeeID", input.EmployeeID > 0 ? (object)input.EmployeeID : DBNull.Value);
            cmd.Parameters.AddWithValue("@GuestName", string.IsNullOrWhiteSpace(input.GuestName) ? (object)DBNull.Value : input.GuestName);
            cmd.Parameters.AddWithValue("@ContactFor", string.IsNullOrWhiteSpace(input.ContactFor) ? (object)DBNull.Value : input.ContactFor);
            cmd.Parameters.AddWithValue("@ContactType", input.ContactType ?? "");
            cmd.Parameters.AddWithValue("@ContactStatus", input.ContactStatus ?? "Active");
            cmd.Parameters.AddWithValue("@Name", input.Name ?? "");
            cmd.Parameters.AddWithValue("@SearchName", string.IsNullOrWhiteSpace(input.SearchName) ? (object)DBNull.Value : input.SearchName);
            cmd.Parameters.AddWithValue("@GenderID", input.GenderID > 0 ? (object)input.GenderID : DBNull.Value);
            cmd.Parameters.AddWithValue("@MaritalStatus", string.IsNullOrWhiteSpace(input.MaritalStatus) ? (object)DBNull.Value : input.MaritalStatus);
            cmd.Parameters.AddWithValue("@ProfessionalTitle", string.IsNullOrWhiteSpace(input.ProfessionalTitle) ? (object)DBNull.Value : input.ProfessionalTitle);
            cmd.Parameters.AddWithValue("@Department", string.IsNullOrWhiteSpace(input.Department) ? (object)DBNull.Value : input.Department);
            cmd.Parameters.AddWithValue("@OfficeLocation", string.IsNullOrWhiteSpace(input.OfficeLocation) ? (object)DBNull.Value : input.OfficeLocation);
            cmd.Parameters.AddWithValue("@AvailableFrom", TimeParam(input.AvailableFrom));
            cmd.Parameters.AddWithValue("@AvailableTo", TimeParam(input.AvailableTo));
            cmd.Parameters.AddWithValue("@ReportToManagerName", string.IsNullOrWhiteSpace(input.ReportToManagerName) ? (object)DBNull.Value : input.ReportToManagerName);
            cmd.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(input.Phone) ? (object)DBNull.Value : input.Phone);
            cmd.Parameters.AddWithValue("@Mobile", string.IsNullOrWhiteSpace(input.Mobile) ? (object)DBNull.Value : input.Mobile);
            cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(input.Email) ? (object)DBNull.Value : input.Email);
            cmd.Parameters.AddWithValue("@Whatsapp", string.IsNullOrWhiteSpace(input.Whatsapp) ? (object)DBNull.Value : input.Whatsapp);
            cmd.Parameters.AddWithValue("@URL", string.IsNullOrWhiteSpace(input.URL) ? (object)DBNull.Value : input.URL);
            cmd.Parameters.AddWithValue("@Fax", string.IsNullOrWhiteSpace(input.Fax) ? (object)DBNull.Value : input.Fax);
            cmd.Parameters.AddWithValue("@RecordType", string.IsNullOrWhiteSpace(input.RecordType) ? "People" : input.RecordType);
            cmd.Parameters.AddWithValue("@VerificationStatus", string.IsNullOrWhiteSpace(input.VerificationStatus) ? ContactVerification.ToVerify : input.VerificationStatus);
            cmd.Parameters.AddWithValue("@AccessStatus", string.IsNullOrWhiteSpace(input.AccessStatus) ? "Open" : input.AccessStatus);
            cmd.Parameters.AddWithValue("@WorkflowStatus", string.IsNullOrWhiteSpace(input.WorkflowStatus) ? ContactWorkflowStatuses.InProcess : input.WorkflowStatus);
        }

        private static object TimeParam(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
            return TimeSpan.TryParse(value, out var t) ? (object)t : DBNull.Value;
        }

        private static string FormatTime(object value)
        {
            if (value == null || value == DBNull.Value) return "";
            if (value is TimeSpan ts) return ts.ToString(@"hh\:mm");
            return value.ToString() ?? "";
        }

        private int InsertRequest(SqlConnection conn, int contactId, string action, string oldJson, string newJson,
            List<ContactChangeLine> lines, string historyComment, string entityType, int addressId)
        {
            int requestId;
            var code = NextCode(conn);
            using (var ins = new SqlCommand(@"
INSERT INTO tblContactChangeRequest
 (RequestCode, ContactID, ActionType, SubmittedByUserID, Status, SubmittedOn, OldJson, NewJson, CreatedOn, EntityType, AddressID)
VALUES (@Code,@ContactID,@Action,@UserID,@Status,GETDATE(),@OldJson,@NewJson,GETDATE(),@EntityType,@AddressID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
            {
                ins.Parameters.AddWithValue("@Code", code);
                ins.Parameters.AddWithValue("@ContactID", contactId);
                ins.Parameters.AddWithValue("@Action", action);
                ins.Parameters.AddWithValue("@UserID", _auth.CurrentUserId.Value);
                ins.Parameters.AddWithValue("@Status", ContactWorkflowStatuses.InProcess);
                ins.Parameters.AddWithValue("@OldJson", (object)oldJson ?? "");
                ins.Parameters.AddWithValue("@NewJson", (object)newJson ?? "");
                ins.Parameters.AddWithValue("@EntityType", string.IsNullOrWhiteSpace(entityType) ? ContactWorkflowEntities.Contact : entityType);
                ins.Parameters.AddWithValue("@AddressID", addressId > 0 ? (object)addressId : DBNull.Value);
                requestId = Convert.ToInt32(ins.ExecuteScalar());
            }
            ReplaceLines(conn, null, requestId, lines);
            InsertHistory(conn, null, requestId, ContactWorkflowStatuses.InProcess, historyComment);
            return requestId;
        }

        private void ReplaceProposal(SqlConnection conn, int requestId, string newJson, List<ContactChangeLine> lines, string comment)
        {
            using (var cmd = new SqlCommand("UPDATE tblContactChangeRequest SET NewJson=@Json WHERE RequestID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Json", newJson ?? "");
                cmd.Parameters.AddWithValue("@Id", requestId);
                cmd.ExecuteNonQuery();
            }
            ReplaceLines(conn, null, requestId, lines);
            InsertHistory(conn, null, requestId, ContactWorkflowStatuses.InProcess, comment);
        }

        private static void ReplaceLines(SqlConnection conn, SqlTransaction tx, int requestId, List<ContactChangeLine> lines)
        {
            using (var del = new SqlCommand("DELETE FROM tblContactChangeLine WHERE RequestID=@Id;", conn, tx))
            {
                del.Parameters.AddWithValue("@Id", requestId);
                del.ExecuteNonQuery();
            }
            if (lines == null) return;
            foreach (var line in lines)
            {
                using (var ins = new SqlCommand(@"
INSERT INTO tblContactChangeLine (RequestID, FieldLabel, OldValue, NewValue)
VALUES (@RequestID,@Label,@Old,@New);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@RequestID", requestId);
                    ins.Parameters.AddWithValue("@Label", line.FieldLabel);
                    ins.Parameters.AddWithValue("@Old", (object)line.OldValue ?? "");
                    ins.Parameters.AddWithValue("@New", (object)line.NewValue ?? "");
                    ins.ExecuteNonQuery();
                }
            }
        }

        private void InsertHistory(SqlConnection conn, SqlTransaction tx, int requestId, string status, string comments)
        {
            using (var cmd = new SqlCommand(@"
INSERT INTO tblContactChangeHistory (RequestID, Status, ActorUserID, Comments, ActionOn)
VALUES (@RequestID,@Status,@UserID,@Comments,GETDATE());", conn, tx))
            {
                cmd.Parameters.AddWithValue("@RequestID", requestId);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@UserID", _auth.CurrentUserId ?? 0);
                cmd.Parameters.AddWithValue("@Comments", string.IsNullOrWhiteSpace(comments) ? (object)DBNull.Value : comments);
                cmd.ExecuteNonQuery();
            }
        }

        private List<ContactChangeLine> LoadLines(int requestId)
        {
            var list = new List<ContactChangeLine>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT FieldLabel, ISNULL(OldValue,''), ISNULL(NewValue,'') FROM tblContactChangeLine WHERE RequestID=@Id;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", requestId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        list.Add(new ContactChangeLine { FieldLabel = dr.GetString(0), OldValue = dr.GetString(1), NewValue = dr.GetString(2) });
            }
            return list;
        }

        private List<ContactChangeHistoryItem> LoadHistory(int requestId)
        {
            var list = new List<ContactChangeHistoryItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT h.Status, ISNULL(u.FullName,u.Username), ISNULL(h.Comments,''), h.ActionOn
FROM tblContactChangeHistory h
LEFT JOIN tblUser u ON u.UserID=h.ActorUserID
WHERE h.RequestID=@Id ORDER BY h.ActionOn;", conn))
            {
                cmd.Parameters.AddWithValue("@Id", requestId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        list.Add(new ContactChangeHistoryItem
                        {
                            Status = dr.GetString(0),
                            ActorName = dr.IsDBNull(1) ? "" : dr.GetString(1),
                            Comments = dr.GetString(2),
                            ActionOn = dr.GetDateTime(3)
                        });
            }
            return list;
        }

        private string NextCode(SqlConnection conn)
        {
            using (var cmd = new SqlCommand(@"
SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(RequestCode,5,10) AS INT)),0)
FROM tblContactChangeRequest WHERE RequestCode LIKE 'CWF[0-9]%';", conn))
            {
                return "CWF" + (Convert.ToInt32(cmd.ExecuteScalar()) + 1).ToString("D5");
            }
        }

        private void SetContactWorkflow(SqlConnection conn, int contactId, string status, string verification)
        {
            SetContactWorkflow(conn, null, contactId, status, verification);
        }

        private static void SetContactWorkflow(SqlConnection conn, SqlTransaction tx, int contactId, string status, string verification)
        {
            using (var cmd = new SqlCommand(@"
UPDATE tblContactMaster SET WorkflowStatus=@S, VerificationStatus=@V, ModifiedOn=GETDATE()
WHERE ContactID=@Id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@S", status);
                cmd.Parameters.AddWithValue("@V", string.IsNullOrWhiteSpace(verification) ? ContactVerification.ToVerify : verification);
                cmd.Parameters.AddWithValue("@Id", contactId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void ForcePendingVerification(ContactInput proposed)
        {
            if (proposed == null) return;
            proposed.WorkflowStatus = ContactWorkflowStatuses.InProcess;
            proposed.VerificationStatus = ContactVerification.ToVerify;
        }

        private void NotifySubmit(ContactChangeRequest req)
        {
            if (req == null) return;
            var body = _inbox.FormatBody("Contact Hub", req.RequestCode + " — " + req.ContactCode, req.ContactName,
                req.ActionDisplay + " submitted",
                ContactWorkflowStatuses.Display(ContactWorkflowStatuses.InProcess), _inbox.ActorName());
            var title = req.EntityDisplay + " approval required — " + req.RequestCode;
            var link = "/ContactChangeReview.aspx?id=" + req.RequestID;
            var targets = new HashSet<int>(_inbox.UserIdsWithForm(FormKey));
            if (_auth.CurrentUserId.HasValue) targets.Remove(_auth.CurrentUserId.Value);
            _inbox.NotifyUsers(targets, title, body, link, AppInboxService.TypeContactChange, req.RequestID);
            if (_auth.CurrentUserId.HasValue)
            {
                _inbox.Notify(_auth.CurrentUserId.Value, req.EntityDisplay + " request submitted — " + req.RequestCode, body, link,
                    AppInboxService.TypeContactChange, req.RequestID);
            }
        }

        private static List<ContactChangeLine> BuildLines(ContactInput oldItem, ContactInput next)
        {
            var lines = new List<ContactChangeLine>();
            Add(lines, "Contact type", oldItem?.ContactType, next?.ContactType);
            Add(lines, "Name", oldItem?.Name, next?.Name);
            Add(lines, "Search name", oldItem?.SearchName, next?.SearchName);
            Add(lines, "Status", oldItem?.ContactStatus, next?.ContactStatus);
            Add(lines, "Customer ID", oldItem?.CustomerID.ToString(), next?.CustomerID.ToString());
            Add(lines, "Vendor ID", oldItem?.VendorID.ToString(), next?.VendorID.ToString());
            Add(lines, "Employee ID", oldItem?.EmployeeID.ToString(), next?.EmployeeID.ToString());
            Add(lines, "Guest name", oldItem?.GuestName, next?.GuestName);
            Add(lines, "Contact for", oldItem?.ContactFor, next?.ContactFor);
            Add(lines, "Gender ID", oldItem?.GenderID.ToString(), next?.GenderID.ToString());
            Add(lines, "Marital status", oldItem?.MaritalStatus, next?.MaritalStatus);
            Add(lines, "Title", oldItem?.ProfessionalTitle, next?.ProfessionalTitle);
            Add(lines, "Department", oldItem?.Department, next?.Department);
            Add(lines, "Branch / location", oldItem?.OfficeLocation, next?.OfficeLocation);
            Add(lines, "Available from", oldItem?.AvailableFrom, next?.AvailableFrom);
            Add(lines, "Available to", oldItem?.AvailableTo, next?.AvailableTo);
            Add(lines, "Reports to", oldItem?.ReportToManagerName, next?.ReportToManagerName);
            Add(lines, "Phone", oldItem?.Phone, next?.Phone);
            Add(lines, "Mobile", oldItem?.Mobile, next?.Mobile);
            Add(lines, "Email", oldItem?.Email, next?.Email);
            Add(lines, "WhatsApp", oldItem?.Whatsapp, next?.Whatsapp);
            Add(lines, "URL", oldItem?.URL, next?.URL);
            Add(lines, "Fax", oldItem?.Fax, next?.Fax);
            Add(lines, "Record type", oldItem?.RecordType, next?.RecordType);
            Add(lines, "Verification", oldItem?.VerificationStatus, next?.VerificationStatus);
            Add(lines, "Access", oldItem?.AccessStatus, next?.AccessStatus);
            return lines;
        }

        private static void Add(List<ContactChangeLine> lines, string label, string oldVal, string newVal)
        {
            oldVal = (oldVal ?? "").Trim();
            newVal = (newVal ?? "").Trim();
            if (oldVal == "0") oldVal = "";
            if (newVal == "0") newVal = "";
            if (string.Equals(oldVal, newVal, StringComparison.Ordinal)) return;
            lines.Add(new ContactChangeLine { FieldLabel = label, OldValue = oldVal, NewValue = newVal });
        }

        private static string Serialize(ContactInput input) => input == null ? "" : Json.Serialize(input);
        private static ContactInput Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return Json.Deserialize<ContactInput>(json); }
            catch { return null; }
        }

        private void WriteAudit(string actionType, ContactChangeRequest req, string status, string comments, string handler)
        {
            if (req == null) return;
            var isAddress = string.Equals(req.EntityType, ContactWorkflowEntities.Address, StringComparison.OrdinalIgnoreCase);
            var details = FormatAuditDetails(req, status, comments);
            _audit.Log(actionType, isAddress ? "ContactAddress" : "Contact",
                isAddress && req.AddressID > 0 ? req.AddressID : req.ContactID,
                string.IsNullOrEmpty(req.RequestCode) ? req.ContactCode : req.RequestCode + " " + req.ContactCode,
                FormKey, handler == "Decide" ? "/ContactChangeReview" : "/ContactMaster", handler, details);
        }

        private static string FormatAuditDetails(ContactChangeRequest req, string status, string comments)
        {
            var sb = new StringBuilder();
            sb.Append("ContactID=").Append(req.ContactID).Append("; ");
            if (req.AddressID > 0) sb.Append("AddressID=").Append(req.AddressID).Append("; ");
            sb.Append("Request=").Append(req.RequestCode ?? "").Append("; ");
            sb.Append("Entity=").Append(req.EntityDisplay).Append("; ");
            sb.Append("Action=").Append(req.ActionType ?? "").Append("; ");
            sb.Append("Status=").Append(ContactWorkflowStatuses.Display(status)).Append("; ");
            sb.Append("Initiator=").Append(req.SubmittedByName ?? "").Append("; ");
            if (!string.IsNullOrWhiteSpace(req.DecidedByName))
                sb.Append("Approver=").Append(req.DecidedByName).Append("; ");
            if (!string.IsNullOrWhiteSpace(comments))
                sb.Append("Comments=").Append(comments.Trim()).Append("; ");
            if (req.Lines != null)
            {
                foreach (var line in req.Lines)
                {
                    sb.Append(line.FieldLabel).Append(":[").Append(line.OldValue).Append(" => ").Append(line.NewValue).Append("]; ");
                }
            }
            return sb.ToString();
        }

        private static string SerializeAddress(ContactAddressItem item) => item == null ? "" : Json.Serialize(item);
        private static ContactAddressItem DeserializeAddress(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return Json.Deserialize<ContactAddressItem>(json); }
            catch { return null; }
        }

        private static List<ContactChangeLine> BuildAddressLines(ContactAddressItem oldItem, ContactAddressItem next)
        {
            var lines = new List<ContactChangeLine>();
            Add(lines, "Country", oldItem?.CountryName, next?.CountryName);
            Add(lines, "State", oldItem?.StateName, next?.StateName);
            Add(lines, "City", oldItem?.CityName, next?.CityName);
            Add(lines, "Postal code", oldItem?.PostalCode, next?.PostalCode);
            Add(lines, "Street", oldItem?.StreetName, next?.StreetName);
            Add(lines, "Address detail", oldItem?.AddressLine, next?.AddressLine);
            Add(lines, "Latitude", oldItem?.LatitudeText, next?.LatitudeText);
            Add(lines, "Longitude", oldItem?.LongitudeText, next?.LongitudeText);
            Add(lines, "Primary", Flag(oldItem?.IsPrimary), Flag(next?.IsPrimary));
            Add(lines, "Billing", Flag(oldItem?.IsBilling), Flag(next?.IsBilling));
            Add(lines, "Shipping", Flag(oldItem?.IsShipping), Flag(next?.IsShipping));
            if (next == null && oldItem != null)
                Add(lines, "Record", "Active address", "Delete");
            return lines;
        }

        private static string Flag(bool? value) => value == true ? "Yes" : "";

        private static void Exec(SqlConnection conn, string sql)
        {
            try
            {
                using (var cmd = new SqlCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
            catch { }
        }
    }
}
