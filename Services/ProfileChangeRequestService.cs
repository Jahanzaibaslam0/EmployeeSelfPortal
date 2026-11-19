using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace HRMS.Services
{
    public static class ProfileChangeStatuses
    {
        public const string Draft = "Draft";
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";

        public static string Display(string status)
        {
            switch (status)
            {
                case Pending: return "Pending HR Approval";
                case Approved: return "Approved";
                case Rejected: return "Rejected";
                case Draft: return "Draft";
                default: return status ?? "";
            }
        }

        public static string BadgeClass(string status)
        {
            switch (status)
            {
                case Pending: return "badge badge-warning";
                case Approved: return "badge badge-success";
                case Rejected: return "badge badge-danger";
                default: return "badge badge-info";
            }
        }
    }

    public class ProfileChangeLine
    {
        public string FieldKey { get; set; } = "";
        public string FieldLabel { get; set; } = "";
        public string Section { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public string OldJson { get; set; } = "";
        public string NewJson { get; set; } = "";
        public bool IsSnapshot { get; set; }
        public bool IsPhoto => string.Equals(FieldKey, "Personal.PhotoPath", StringComparison.OrdinalIgnoreCase);
    }

    public class ProfileChangeHistoryItem
    {
        public string Status { get; set; } = "";
        public string ActorName { get; set; } = "";
        public string Comments { get; set; } = "";
        public DateTime ActionOn { get; set; }
    }

    public class ProfileChangeRequest
    {
        public int RequestID { get; set; }
        public string RequestCode { get; set; } = "";
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public int SubmittedByUserID { get; set; }
        public string SubmittedByName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime SubmittedOn { get; set; }
        public DateTime? DecidedOn { get; set; }
        public int? DecidedByUserID { get; set; }
        public string DecidedByName { get; set; } = "";
        public string DecisionComments { get; set; } = "";
        public List<ProfileChangeLine> Lines { get; set; } = new List<ProfileChangeLine>();
        public List<ProfileChangeHistoryItem> History { get; set; } = new List<ProfileChangeHistoryItem>();
        public string StatusDisplay => ProfileChangeStatuses.Display(Status);
        public string BadgeClass => ProfileChangeStatuses.BadgeClass(Status);
        public IEnumerable<ProfileChangeLine> VisibleLines => Lines.Where(l => !l.IsSnapshot);
    }

    public class ProfileChangeDraft
    {
        public int EmployeeID { get; set; }
        public List<ProfileChangeLine> Lines { get; set; } = new List<ProfileChangeLine>();
    }

    public class ProfileChangeRequestService
    {
        public const string FormKey = "ProfileChange";
        public const string SessionDraftKey = "ProfileChangeDraft";
        public const string RelatedType = "ProfileChange";

        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();
        private readonly PermissionService _perms = new PermissionService();
        private readonly EmployeeProfileAccessService _profile = new EmployeeProfileAccessService();
        private readonly AuditService _audit = new AuditService();
        private readonly AppInboxService _inbox = new AppInboxService();
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public ProfileChangeRequestService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public void EnsureSchema()
        {
            if (string.IsNullOrWhiteSpace(_conn)) return;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                foreach (var sql in SchemaStatements)
                {
                    try
                    {
                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.CommandTimeout = 30;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { }
                }
            }
        }

        // Uses global::HRMS.EmployeeInput / global::HRMS.EmployeeContactInput / global::HRMS.EmployeeAddressInput / global::HRMS.EmployeeIdentificationInput from namespace HRMS.

        public bool RequiresWorkflow(int employeeId)
        {
            if (employeeId <= 0) return false;
            // Any user changing their own linked employee profile must go through HR approval.
            // Official Employee Master edits of *other* people still save directly.
            return _profile.OwnsEmployee(employeeId);
        }

        public bool CanReview()
        {
            if (_auth.IsAdmin) return true;
            if (_perms.CanApprove(FormKey) || _perms.CanRead(FormKey)) return true;
            if (_perms.CanApprove("EmployeeMaster")) return true;
            if (_perms.CanRead("LeaveHR")) return true;
            return _profile.HasFullEmployeeMasterAccess() && _perms.CanWrite("EmployeeMaster");
        }

        public bool CanApproveRequest(ProfileChangeRequest request)
        {
            if (request == null || request.Status != ProfileChangeStatuses.Pending) return false;
            if (!CanReview()) return false;
            if (!_auth.CurrentUserId.HasValue) return false;
            if (request.SubmittedByUserID == _auth.CurrentUserId.Value) return false;
            return true;
        }

        public ProfileChangeRequest GetPendingForEmployee(int employeeId)
        {
            EnsureSchema();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT TOP 1 RequestID FROM tblProfileChangeRequest
WHERE EmployeeID=@EmployeeID AND Status=@Status
ORDER BY SubmittedOn DESC;", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                cmd.Parameters.AddWithValue("@Status", ProfileChangeStatuses.Pending);
                conn.Open();
                var id = cmd.ExecuteScalar();
                if (id == null || id == DBNull.Value) return null;
                return GetRequest(Convert.ToInt32(id));
            }
        }

        public string BuildDraft(int employeeId, global::HRMS.EmployeeInput proposed,
            List<global::HRMS.EmployeeContactInput> contacts, List<global::HRMS.EmployeeAddressInput> addresses,
            List<global::HRMS.EmployeeIdentificationInput> identifications,
            List<global::HRMS.EmployeeFamilyMemberInput> family,
            List<global::HRMS.EmployeeBankInput> banks,
            List<global::HRMS.EmployeeEducationInput> education,
            List<global::HRMS.EmployeeCertificateInput> certificates,
            List<global::HRMS.EmployeeDocumentInput> documents,
            out ProfileChangeDraft draft)
        {
            draft = null;
            EnsureSchema();
            if (!_profile.OwnsEmployee(employeeId))
                return "You can only submit changes to your own profile.";
            var pending = GetPendingForEmployee(employeeId);
            if (pending != null)
                return "You already have pending request " + pending.RequestCode + ". Wait for HR to review it before submitting again.";

            var lines = new List<ProfileChangeLine>();
            if (proposed != null) AddPersonalDiffs(employeeId, proposed, lines);
            if (contacts != null) AddContactDiffs(employeeId, contacts, lines);
            if (addresses != null) AddAddressDiffs(employeeId, addresses, lines);
            if (identifications != null) AddIdentificationDiffs(employeeId, identifications, lines);
            if (family != null) AddFamilyDiffs(employeeId, family, lines);
            if (banks != null) AddBankDiffs(employeeId, banks, lines);
            if (education != null) AddEducationDiffs(employeeId, education, lines);
            if (certificates != null) AddCertificateDiffs(employeeId, certificates, lines);
            if (documents != null) AddDocumentDiffs(employeeId, documents, lines);

            var visible = lines.Where(l => !l.IsSnapshot).ToList();
            if (visible.Count == 0)
                return "No changes were detected compared with your approved profile.";

            draft = new ProfileChangeDraft { EmployeeID = employeeId, Lines = lines };
            return null;
        }

        public string SubmitDraft(ProfileChangeDraft draft, out int requestId)
        {
            requestId = 0;
            if (draft == null || draft.EmployeeID <= 0 || draft.Lines == null || !draft.Lines.Any(l => !l.IsSnapshot))
                return "There is nothing to submit.";
            if (!_auth.CurrentUserId.HasValue)
                return "Sign in again to submit this request.";
            if (!_profile.OwnsEmployee(draft.EmployeeID))
                return "You can only submit changes to your own profile.";

            EnsureSchema();
            var pending = GetPendingForEmployee(draft.EmployeeID);
            if (pending != null)
                return "You already have pending request " + pending.RequestCode + ".";

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var code = NextCode(conn, tx);
                        using (var ins = new SqlCommand(@"
INSERT INTO tblProfileChangeRequest
    (RequestCode, EmployeeID, SubmittedByUserID, Status, SubmittedOn, CreatedOn)
VALUES (@Code, @EmployeeID, @UserID, @Status, GETDATE(), GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx))
                        {
                            ins.Parameters.AddWithValue("@Code", code);
                            ins.Parameters.AddWithValue("@EmployeeID", draft.EmployeeID);
                            ins.Parameters.AddWithValue("@UserID", _auth.CurrentUserId.Value);
                            ins.Parameters.AddWithValue("@Status", ProfileChangeStatuses.Pending);
                            requestId = Convert.ToInt32(ins.ExecuteScalar());
                        }
                        foreach (var line in draft.Lines)
                            InsertLine(conn, tx, requestId, line);
                        InsertHistory(conn, tx, requestId, ProfileChangeStatuses.Pending, _auth.CurrentUserId.Value, "Submitted for HR approval.");
                        tx.Commit();
                    }
                    catch
                    {
                        try { tx.Rollback(); } catch { }
                        requestId = 0;
                        return "Could not save every proposed field. Nothing was submitted.";
                    }
                }
            }

            var emp = EmployeeLabel(draft.EmployeeID);
            NotifyUser(_auth.CurrentUserId.Value, "Profile update submitted",
                "Request was sent to HR. Your approved profile is unchanged until HR decides.",
                "/ProfileChangeHistory.aspx?id=" + requestId, requestId);
            foreach (var hrId in GetReviewerUserIds())
            {
                if (hrId == _auth.CurrentUserId.Value) continue;
                NotifyUser(hrId, "Profile update pending",
                    emp + " submitted a profile update for review.",
                    "/ProfileChangeReview.aspx?id=" + requestId, requestId);
            }
            _audit.Log("ProfileChangeSubmitted", "ProfileChange", requestId, emp, FormKey, "/EmployeeMaster", "Submit", "Pending HR approval");
            return null;
        }

        public string Decide(int requestId, bool approve, string comments)
        {
            EnsureSchema();
            var request = GetRequest(requestId);
            if (request == null) return "Request not found.";
            if (!CanApproveRequest(request))
                return request.SubmittedByUserID == _auth.CurrentUserId
                    ? "You cannot approve or reject your own profile request."
                    : "You are not authorized to review this request.";

            comments = (comments ?? "").Trim();
            if (!approve && string.IsNullOrWhiteSpace(comments))
                return "Enter a reason when rejecting a request.";

            var newStatus = approve ? ProfileChangeStatuses.Approved : ProfileChangeStatuses.Rejected;
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        if (approve)
                            ApplyApproved(conn, tx, request);

                        using (var upd = new SqlCommand(@"
UPDATE tblProfileChangeRequest
SET Status=@Status, DecidedOn=GETDATE(), DecidedByUserID=@UserID, DecisionComments=@Comments, ModifiedOn=GETDATE()
WHERE RequestID=@ID AND Status=@Pending;", conn, tx))
                        {
                            upd.Parameters.AddWithValue("@Status", newStatus);
                            upd.Parameters.AddWithValue("@UserID", _auth.CurrentUserId.Value);
                            upd.Parameters.AddWithValue("@Comments", string.IsNullOrEmpty(comments) ? (object)DBNull.Value : comments);
                            upd.Parameters.AddWithValue("@ID", requestId);
                            upd.Parameters.AddWithValue("@Pending", ProfileChangeStatuses.Pending);
                            if (upd.ExecuteNonQuery() != 1)
                            {
                                tx.Rollback();
                                return "This request is no longer pending.";
                            }
                        }
                        InsertHistory(conn, tx, requestId, newStatus, _auth.CurrentUserId.Value, comments);
                        tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        return "Could not apply every field in this request. No official profile values were changed. " + ex.Message;
                    }
                }
            }

            var msg = approve
                ? "HR approved your profile update. Official values are now updated."
                : "HR rejected your profile update. Approved values were not changed. Reason: " + comments;
            NotifyUser(request.SubmittedByUserID, approve ? "Profile update approved" : "Profile update rejected",
                msg, "/ProfileChangeHistory.aspx?id=" + requestId, requestId);
            MarkRelatedInboxRead(requestId);
            _audit.Log(approve ? "ProfileChangeApproved" : "ProfileChangeRejected", "ProfileChange", requestId,
                request.RequestCode, FormKey, "/ProfileChangeReview", "Decide", comments, success: true);
            return null;
        }

        public ProfileChangeRequest GetRequest(int requestId)
        {
            EnsureSchema();
            ProfileChangeRequest item = null;
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT r.RequestID, r.RequestCode, r.EmployeeID, r.SubmittedByUserID, r.Status, r.SubmittedOn,
       r.DecidedOn, r.DecidedByUserID, r.DecisionComments,
       ISNULL(e.FirstName,'') + ' ' + ISNULL(e.LastName,'') AS EmployeeName,
       ISNULL(e.EmployeeCode,'') AS EmployeeCode,
       ISNULL(su.FullName, su.Username) AS SubmittedByName,
       ISNULL(du.FullName, du.Username) AS DecidedByName
FROM tblProfileChangeRequest r
LEFT JOIN tblEmployee e ON e.EmployeeID = r.EmployeeID
LEFT JOIN tblUser su ON su.UserID = r.SubmittedByUserID
LEFT JOIN tblUser du ON du.UserID = r.DecidedByUserID
WHERE r.RequestID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", requestId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    item = MapHeader(dr);
                }
            }
            item.Lines = LoadLines(requestId);
            item.History = LoadHistory(requestId);
            return item;
        }

        public bool CanViewRequest(ProfileChangeRequest request)
        {
            if (request == null || !_auth.CurrentUserId.HasValue) return false;
            if (CanReview()) return true;
            return request.SubmittedByUserID == _auth.CurrentUserId.Value || _profile.OwnsEmployee(request.EmployeeID);
        }

        public List<ProfileChangeRequest> ListPendingForHr()
        {
            return ListBySql(@"
WHERE r.Status = @Status
ORDER BY r.SubmittedOn ASC;", cmd => cmd.Parameters.AddWithValue("@Status", ProfileChangeStatuses.Pending));
        }

        public List<ProfileChangeRequest> ListForEmployee(int employeeId)
        {
            return ListBySql(@"
WHERE r.EmployeeID = @EmployeeID
ORDER BY r.SubmittedOn DESC;", cmd => cmd.Parameters.AddWithValue("@EmployeeID", employeeId));
        }

        public int UnreadInboxCount(int userId) => _inbox.UnreadCount(userId);

        public List<InboxMessage> ListInbox(int userId, int take = 30) => _inbox.List(userId, take);

        public void MarkInboxRead(int inboxId, int userId) => _inbox.MarkRead(inboxId, userId);

        public void StoreDraft(ProfileChangeDraft draft)
        {
            HttpContext.Current.Session[SessionDraftKey] = draft;
        }

        public ProfileChangeDraft LoadDraft()
        {
            return HttpContext.Current.Session[SessionDraftKey] as ProfileChangeDraft;
        }

        public void ClearDraft() => HttpContext.Current.Session.Remove(SessionDraftKey);

        private void AddPersonalDiffs(int employeeId, global::HRMS.EmployeeInput proposed, List<ProfileChangeLine> lines)
        {
            if (proposed == null) return;
            var current = LoadPersonal(employeeId);
            if (current == null) return;

            AddScalar(lines, "Personal.EmployeeCode", "Employee ID", "Employment", current.EmployeeCode, proposed.EmployeeCode);
            AddScalar(lines, "Personal.FirstName", "First name", "Personal", current.FirstName, proposed.FirstName);
            AddScalar(lines, "Personal.LastName", "Last name", "Personal", current.LastName, proposed.LastName);
            AddScalar(lines, "Personal.FathersHusbandsName", "Father / husband name", "Personal", current.FathersHusbandsName, proposed.FathersHusbandsName);
            AddScalar(lines, "Personal.DisplayName", "Display name", "Personal", current.DisplayName, proposed.DisplayName);
            AddScalar(lines, "Personal.NationalIDNumber", "National ID / CNIC", "Personal", current.NationalIDNumber, proposed.NationalIDNumber);
            AddPhotoDiff(lines, current.PhotoPath, proposed.PhotoPath);
            AddDateDiff(lines, "Personal.DateOfBirth", "Date of birth", "Personal", current.DateOfBirth, proposed.DateOfBirth);
            AddFkDiff(lines, "Personal.GenderID", "Gender", "Personal", current.GenderID, proposed.GenderID, "tblGender", "GenderID", "GenderName");
            AddFkDiff(lines, "Personal.NationalityID", "Nationality", "Personal", current.NationalityID, proposed.NationalityID, "tblNationality", "NationalityID", "NationalityName");
            AddScalar(lines, "Personal.Domicile", "Domicile", "Personal", current.Domicile, proposed.Domicile);
            AddFkDiff(lines, "Personal.ReligionID", "Religion", "Personal", current.ReligionID, proposed.ReligionID, "tblReligion", "ReligionID", "ReligionName");
            AddScalar(lines, "Personal.MaritalStatus", "Marital status", "Personal", current.MaritalStatus, proposed.MaritalStatus);
            AddFkDiff(lines, "Personal.BloodGroupID", "Blood group", "Personal", current.BloodGroupID, proposed.BloodGroupID, "tblBloodGroup", "BloodGroupID", "BloodGroupName");
            AddScalar(lines, "Personal.Status", "Employee status", "Employment", current.Status, proposed.Status);
            AddScalar(lines, "Personal.BiometricID", "Biometric ID", "Employment", current.BiometricID, proposed.BiometricID);

            AddFkDiff(lines, "Personal.LegalEntityID", "Legal entity", "Employment", current.LegalEntityID, proposed.LegalEntityID, "tblLegalEntity", "LegalEntityID", "LegalEntityName");
            AddFkDiff(lines, "Personal.BusinessUnitID", "Business unit", "Employment", current.BusinessUnitID, proposed.BusinessUnitID, "tblBusinessUnit", "BusinessUnitID", "BusinessUnitName");
            AddFkDiff(lines, "Personal.DivisionID", "Division", "Employment", current.DivisionID, proposed.DivisionID, "tblDivision", "DivisionID", "DivisionName");
            AddFkDiff(lines, "Personal.DepartmentID", "Department", "Employment", current.DepartmentID, proposed.DepartmentID, "tblDepartment", "DepartmentID", "DepartmentName");
            if (current.DesignationID != proposed.DesignationID)
                AddScalar(lines, "Personal.DesignationID", "Designation", "Employment",
                    LookupDesignation(current.DesignationID), LookupDesignation(proposed.DesignationID),
                    current.DesignationID.ToString(), proposed.DesignationID.ToString());
            AddFkDiff(lines, "Personal.SalesTeamID", "Section / team", "Employment", current.SalesTeamID, proposed.SalesTeamID, "tblSalesTeam", "SalesTeamID", "SalesTeamName");
            AddFkDiff(lines, "Personal.CostCenterID", "Cost center", "Employment", current.CostCenterID, proposed.CostCenterID, "tblCostCenter", "CostCenterID", "CostCenterName");
            AddFkDiff(lines, "Personal.RegionID", "Region", "Employment", current.RegionID, proposed.RegionID, "tblRegion", "RegionID", "RegionName");
            AddFkDiff(lines, "Personal.LocationID", "Location", "Employment", current.LocationID, proposed.LocationID, "tblLocation", "LocationID", "LocationName");
            AddFkDiff(lines, "Personal.DealForBranchID", "Deal for branch", "Employment", current.DealForBranchID, proposed.DealForBranchID, "tblLocation", "LocationID", "LocationName");
            AddFkDiff(lines, "Personal.SecondaryLocationID", "Secondary location", "Employment", current.SecondaryLocationID, proposed.SecondaryLocationID, "tblLocation", "LocationID", "LocationName");
            AddFkDiff(lines, "Personal.ExtensionID", "Extension", "Employment", current.ExtensionID, proposed.ExtensionID, "tblExtension", "ExtensionID", "ExtensionName");
            AddFkDiff(lines, "Personal.EmploymentStatusID", "Employment status", "Employment", current.EmploymentStatusID, proposed.EmploymentStatusID, "tblEmploymentStatus", "EmploymentStatusID", "EmploymentStatusName");
            AddFkDiff(lines, "Personal.EmploymentTypeID", "Employment type", "Employment", current.EmploymentTypeID, proposed.EmploymentTypeID, "tblEmploymentType", "EmploymentTypeID", "EmploymentTypeName");
            AddFkDiff(lines, "Personal.JobID", "Job", "Employment", current.JobID, proposed.JobID, "tblJob", "JobID", "JobTitle");
            AddFkDiff(lines, "Personal.CityID", "City", "Employment", current.CityID, proposed.CityID, "tblCity", "CityID", "CityName");
            AddFkDiff(lines, "Personal.ProvinceID", "Province", "Employment", current.ProvinceID, proposed.ProvinceID, "tblProvince", "ProvinceID", "ProvinceName");
            AddFkDiff(lines, "Personal.GradeID", "Grade", "Employment", current.GradeID, proposed.GradeID, "tblGrade", "GradeID", "GradeName");
            AddScalar(lines, "Personal.PrivilegeTier", "Privilege tier", "Employment", current.PrivilegeTier, proposed.PrivilegeTier);
            AddFkDiff(lines, "Personal.WorkerCategoryID", "Worker category", "Employment", current.WorkerCategoryID, proposed.WorkerCategoryID, "tblWorkerCategory", "WorkerCategoryID", "WorkerCategoryName");
            AddFkDiff(lines, "Personal.WorkforceSegmentID", "Workforce segment", "Employment", current.WorkforceSegmentID, proposed.WorkforceSegmentID, "tblWorkforceSegment", "WorkforceSegmentID", "WorkforceSegmentName");
            AddFkDiff(lines, "Personal.WorkArrangementID", "Work agreement", "Employment", current.WorkArrangementID, proposed.WorkArrangementID, "tblWorkArrangement", "WorkArrangementID", "WorkArrangementName");
            AddFkDiff(lines, "Personal.AttendanceMethodID", "Attendance method", "Employment", current.AttendanceMethodID, proposed.AttendanceMethodID, "tblAttendanceMethod", "AttendanceMethodID", "AttendanceMethodName");
            if (current.UserID != proposed.UserID)
                AddScalar(lines, "Personal.UserID", "User", "Employment",
                    LookupUser(current.UserID), LookupUser(proposed.UserID),
                    current.UserID.ToString(), proposed.UserID.ToString());
            if (current.TemporaryResponsibleEmployeeID != proposed.TemporaryResponsibleEmployeeID)
                AddScalar(lines, "Personal.TemporaryResponsibleEmployeeID", "Temporary responsible", "Employment",
                    LookupEmployeeName(current.TemporaryResponsibleEmployeeID), LookupEmployeeName(proposed.TemporaryResponsibleEmployeeID),
                    current.TemporaryResponsibleEmployeeID.ToString(), proposed.TemporaryResponsibleEmployeeID.ToString());
            if (current.PermanentResponsibleEmployeeID != proposed.PermanentResponsibleEmployeeID)
                AddScalar(lines, "Personal.PermanentResponsibleEmployeeID", "Permanent responsible", "Employment",
                    LookupEmployeeName(current.PermanentResponsibleEmployeeID), LookupEmployeeName(proposed.PermanentResponsibleEmployeeID),
                    current.PermanentResponsibleEmployeeID.ToString(), proposed.PermanentResponsibleEmployeeID.ToString());
            AddDateDiff(lines, "Personal.DateOfJoining", "Joining date", "Employment", current.DateOfJoining, proposed.DateOfJoining);
            AddScalar(lines, "Personal.ProbationPeriodDays", "Probation period (days)", "Employment", current.ProbationPeriodDays, proposed.ProbationPeriodDays);
            AddDateDiff(lines, "Personal.ProbationEndDate", "Probation end date", "Employment", current.ProbationEndDate, proposed.ProbationEndDate);
            AddDateDiff(lines, "Personal.ConfirmationDate", "Confirmation date", "Employment", current.ConfirmationDate, proposed.ConfirmationDate);
            AddMoneyDiff(lines, "Personal.BasicSalary", "Gross salary", "Compensation", current.BasicSalary, proposed.BasicSalary);
            AddMoneyDiff(lines, "Personal.ConvenienceAllowance", "Convenience allowance", "Compensation", current.ConvenienceAllowance, proposed.ConvenienceAllowance);
            AddFkDiff(lines, "Personal.BenefitEntitlementID", "Benefit entitlement", "Compensation", current.BenefitEntitlementID, proposed.BenefitEntitlementID, "tblBenefitEntitlement", "BenefitEntitlementID", "BenefitEntitlementName");
        }

        private void AddContactDiffs(int employeeId, List<global::HRMS.EmployeeContactInput> proposed, List<ProfileChangeLine> lines)
        {
            var current = LoadContacts(employeeId);
            var curText = FormatContacts(current);
            var newText = FormatContacts(proposed);
            if (Norm(curText) == Norm(newText)) return;
            var oldJson = Json.Serialize(current);
            var newJson = Json.Serialize(SanitizeContacts(proposed));
            lines.Add(new ProfileChangeLine
            {
                FieldKey = "Snapshot.Contacts",
                FieldLabel = "Contacts snapshot",
                Section = "Contact",
                OldJson = oldJson,
                NewJson = newJson,
                IsSnapshot = true
            });
            var types = current.Select(c => c.ContactType).Concat(proposed.Select(c => c.ContactType))
                .Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var type in types)
            {
                var oldV = string.Join(" | ", current.Where(c => string.Equals(c.ContactType, type, StringComparison.OrdinalIgnoreCase))
                    .Select(FormatContact));
                var newV = string.Join(" | ", proposed.Where(c => string.Equals(c.ContactType, type, StringComparison.OrdinalIgnoreCase))
                    .Select(FormatContact));
                AddScalar(lines, "Contact." + type, type, "Contact", oldV, newV, oldJson, newJson);
            }
        }

        private void AddAddressDiffs(int employeeId, List<global::HRMS.EmployeeAddressInput> proposed, List<ProfileChangeLine> lines)
        {
            var current = LoadAddresses(employeeId);
            if (Norm(FormatAddresses(current)) == Norm(FormatAddresses(proposed))) return;
            var oldJson = Json.Serialize(current);
            var newJson = Json.Serialize(SanitizeAddresses(proposed));
            lines.Add(new ProfileChangeLine
            {
                FieldKey = "Snapshot.Addresses",
                FieldLabel = "Addresses snapshot",
                Section = "Address",
                OldJson = oldJson,
                NewJson = newJson,
                IsSnapshot = true
            });
            var types = current.Select(a => a.AddressType).Concat(proposed.Select(a => a.AddressType))
                .Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var type in types)
            {
                var oldV = string.Join(" | ", current.Where(a => string.Equals(a.AddressType, type, StringComparison.OrdinalIgnoreCase)).Select(FormatAddress));
                var newV = string.Join(" | ", proposed.Where(a => string.Equals(a.AddressType, type, StringComparison.OrdinalIgnoreCase)).Select(FormatAddress));
                AddScalar(lines, "Address." + type, type + " address", "Address", oldV, newV, oldJson, newJson);
            }
        }

        private void AddIdentificationDiffs(int employeeId, List<global::HRMS.EmployeeIdentificationInput> proposed, List<ProfileChangeLine> lines)
        {
            var current = LoadIdentifications(employeeId);
            if (Norm(FormatIdents(current)) == Norm(FormatIdents(proposed))) return;
            var oldJson = Json.Serialize(current);
            var newJson = Json.Serialize(SanitizeIdents(proposed));
            lines.Add(new ProfileChangeLine
            {
                FieldKey = "Snapshot.Identifications",
                FieldLabel = "Identifications snapshot",
                Section = "Identification",
                OldJson = oldJson,
                NewJson = newJson,
                IsSnapshot = true
            });
            var types = current.Select(i => i.DocumentType).Concat(proposed.Select(i => i.DocumentType))
                .Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var type in types)
            {
                var oldV = string.Join(" | ", current.Where(i => string.Equals(i.DocumentType, type, StringComparison.OrdinalIgnoreCase)).Select(FormatIdent));
                var newV = string.Join(" | ", proposed.Where(i => string.Equals(i.DocumentType, type, StringComparison.OrdinalIgnoreCase)).Select(FormatIdent));
                AddScalar(lines, "Identification." + type, type, "Identification", oldV, newV, oldJson, newJson);
            }
        }

        private void AddFamilyDiffs(int employeeId, List<global::HRMS.EmployeeFamilyMemberInput> proposed, List<ProfileChangeLine> lines)
            => AddNamedListDiff(LoadFamily(employeeId), proposed, lines, "Snapshot.Family", "Family.Members", "Family members", "Family",
                rows => string.Join("; ", rows.Where(m => !string.IsNullOrWhiteSpace(m.MemberName))
                    .Select(m => (m.MemberName ?? "") + (string.IsNullOrWhiteSpace(m.Relationship) ? "" : " (" + m.Relationship + ")"))));

        private void AddBankDiffs(int employeeId, List<global::HRMS.EmployeeBankInput> proposed, List<ProfileChangeLine> lines)
            => AddNamedListDiff(LoadBanks(employeeId), proposed, lines, "Snapshot.Banks", "Bank.Accounts", "Bank accounts", "Bank",
                rows => string.Join("; ", rows.Where(b => b.BankID > 0)
                    .Select(b => (string.IsNullOrWhiteSpace(b.IBAN) ? b.BankCode : b.IBAN) + (b.IsPrimary ? " (primary)" : ""))));

        private void AddEducationDiffs(int employeeId, List<global::HRMS.EmployeeEducationInput> proposed, List<ProfileChangeLine> lines)
            => AddNamedListDiff(LoadEducation(employeeId), proposed, lines, "Snapshot.Education", "Education.Records", "Education", "Education",
                rows => string.Join("; ", rows.Where(e => !string.IsNullOrWhiteSpace(e.HighestQualification) || !string.IsNullOrWhiteSpace(e.Institution))
                    .Select(e => (e.HighestQualification ?? e.DegreeCertificate ?? "") + (string.IsNullOrWhiteSpace(e.Institution) ? "" : " / " + e.Institution))));

        private void AddCertificateDiffs(int employeeId, List<global::HRMS.EmployeeCertificateInput> proposed, List<ProfileChangeLine> lines)
            => AddNamedListDiff(LoadCertificates(employeeId), proposed, lines, "Snapshot.Certificates", "Certificate.Records", "Certificates", "Certificate",
                rows => string.Join("; ", rows.Where(c => !string.IsNullOrWhiteSpace(c.CertificationName) || !string.IsNullOrWhiteSpace(c.CertificateNumber))
                    .Select(c => (c.CertificationName ?? "") + (string.IsNullOrWhiteSpace(c.CertificateNumber) ? "" : " #" + c.CertificateNumber))));

        private void AddDocumentDiffs(int employeeId, List<global::HRMS.EmployeeDocumentInput> proposed, List<ProfileChangeLine> lines)
        {
            var current = NormalizeDocuments(LoadDocuments(employeeId));
            var next = NormalizeDocuments(proposed);
            if (DocumentFingerprint(current) == DocumentFingerprint(next))
                return;
            AddNamedListDiff(current, next, lines, "Snapshot.Documents", "Document.Records", "Documents", "Document", FormatDocuments);
        }

        private static void AddPhotoDiff(List<ProfileChangeLine> lines, string currentPath, string proposedPath)
        {
            var oldPath = (currentPath ?? "").Trim();
            var newPath = (proposedPath ?? "").Trim();
            if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                return;
            lines.Add(new ProfileChangeLine
            {
                FieldKey = "Personal.PhotoPath",
                FieldLabel = "Profile picture",
                Section = "Personal",
                OldValue = PhotoLabel(oldPath),
                NewValue = PhotoLabel(newPath),
                OldJson = oldPath,
                NewJson = newPath
            });
        }

        private static string PhotoLabel(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "(none)";
            var name = Path.GetFileName(path.Replace('\\', '/'));
            return string.IsNullOrWhiteSpace(name) ? "Photo on file" : name;
        }

        private List<global::HRMS.EmployeeDocumentInput> NormalizeDocuments(List<global::HRMS.EmployeeDocumentInput> rows)
        {
            var list = new List<global::HRMS.EmployeeDocumentInput>();
            foreach (var d in rows ?? new List<global::HRMS.EmployeeDocumentInput>())
            {
                if (d == null) continue;
                if (d.DocumentTypeID <= 0
                    && string.IsNullOrWhiteSpace(d.DocumentNumber)
                    && string.IsNullOrWhiteSpace(d.DocumentPath)
                    && string.IsNullOrWhiteSpace(d.OriginalFileName))
                    continue;
                if (string.IsNullOrWhiteSpace(d.DocumentTypeName) && d.DocumentTypeID > 0)
                    d.DocumentTypeName = LookupName("tblDocumentType", "DocumentTypeID", "DocumentTypeName", d.DocumentTypeID);
                list.Add(d);
            }
            return list;
        }

        private static string FormatDocuments(IEnumerable<global::HRMS.EmployeeDocumentInput> rows)
        {
            return string.Join("; ", (rows ?? Enumerable.Empty<global::HRMS.EmployeeDocumentInput>()).Select(d =>
            {
                var name = string.IsNullOrWhiteSpace(d.DocumentTypeName) ? "Document" : d.DocumentTypeName.Trim();
                var num = string.IsNullOrWhiteSpace(d.DocumentNumber) ? "" : " #" + d.DocumentNumber.Trim();
                var file = string.IsNullOrWhiteSpace(d.OriginalFileName)
                    ? (string.IsNullOrWhiteSpace(d.DocumentPath) ? "" : " [file attached]")
                    : " [" + d.OriginalFileName.Trim() + "]";
                return name + num + file;
            }));
        }

        private static string DocumentFingerprint(IEnumerable<global::HRMS.EmployeeDocumentInput> rows)
        {
            return string.Join("\n", (rows ?? Enumerable.Empty<global::HRMS.EmployeeDocumentInput>())
                .Select(d => string.Join("|",
                    d.DocumentTypeID,
                    (d.DocumentNumber ?? "").Trim().ToLowerInvariant(),
                    NormDate(d.IssueDate),
                    NormDate(d.ExpiryDate),
                    (d.Remarks ?? "").Trim().ToLowerInvariant(),
                    (d.DocumentPath ?? "").Trim().ToLowerInvariant(),
                    (d.OriginalFileName ?? "").Trim().ToLowerInvariant(),
                    (d.VerificationStatus ?? "Pending").Trim().ToLowerInvariant()))
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
        }

        private void AddNamedListDiff<T>(List<T> current, List<T> proposed, List<ProfileChangeLine> lines,
            string snapshotKey, string fieldKey, string label, string section, Func<IEnumerable<T>, string> format)
        {
            var oldText = format(current ?? new List<T>());
            var newText = format(proposed ?? new List<T>());
            if (Norm(oldText) == Norm(newText) && Json.Serialize(current ?? new List<T>()) == Json.Serialize(proposed ?? new List<T>()))
                return;
            var oldJson = Json.Serialize(current ?? new List<T>());
            var newJson = Json.Serialize(proposed ?? new List<T>());
            if (oldJson == newJson) return;
            lines.Add(new ProfileChangeLine
            {
                FieldKey = snapshotKey,
                FieldLabel = label + " snapshot",
                Section = section,
                OldJson = oldJson,
                NewJson = newJson,
                IsSnapshot = true
            });
            AddScalar(lines, fieldKey, label, section, oldText, newText, oldJson, newJson);
        }

        private static void AddScalar(List<ProfileChangeLine> lines, string key, string label, string section, string oldV, string newV, string oldJson = null, string newJson = null)
        {
            if (Norm(oldV) == Norm(newV)) return;
            lines.Add(new ProfileChangeLine
            {
                FieldKey = key,
                FieldLabel = label,
                Section = section,
                OldValue = oldV ?? "",
                NewValue = newV ?? "",
                OldJson = oldJson ?? "",
                NewJson = newJson ?? ""
            });
        }

        private void AddFkDiff(List<ProfileChangeLine> lines, string key, string label, string section,
            int oldId, int newId, string table, string idColumn, string nameColumn)
        {
            if (oldId == newId) return;
            AddScalar(lines, key, label, section,
                LookupName(table, idColumn, nameColumn, oldId),
                LookupName(table, idColumn, nameColumn, newId),
                oldId.ToString(), newId.ToString());
        }

        private static void AddDateDiff(List<ProfileChangeLine> lines, string key, string label, string section, string oldV, string newV)
        {
            if (NormDate(oldV) == NormDate(newV)) return;
            AddScalar(lines, key, label, section, DisplayDate(oldV), DisplayDate(newV));
        }

        private static void AddMoneyDiff(List<ProfileChangeLine> lines, string key, string label, string section, string oldV, string newV)
        {
            if (NormMoney(oldV) == NormMoney(newV)) return;
            AddScalar(lines, key, label, section, DisplayMoney(oldV), DisplayMoney(newV));
        }

        private void ApplyApproved(SqlConnection conn, SqlTransaction tx, ProfileChangeRequest request)
        {
            var lines = request.Lines ?? new List<ProfileChangeLine>();
            ApplyPersonalColumns(conn, tx, request.EmployeeID, lines);

            var contactJson = FindSectionJson(lines, "Snapshot.Contacts", "Contact.");
            if (!string.IsNullOrWhiteSpace(contactJson))
                ReplaceContacts(conn, tx, request.EmployeeID, DeserializeList<global::HRMS.EmployeeContactInput>(contactJson));
            else
                ApplyContactsFromScalars(conn, tx, request.EmployeeID, lines);

            var addressJson = FindSectionJson(lines, "Snapshot.Addresses", "Address.");
            if (!string.IsNullOrWhiteSpace(addressJson))
                ReplaceAddresses(conn, tx, request.EmployeeID, DeserializeList<global::HRMS.EmployeeAddressInput>(addressJson));
            else
                ApplyAddressesFromScalars(conn, tx, request.EmployeeID, lines);

            var identJson = FindSectionJson(lines, "Snapshot.Identifications", "Identification.");
            if (!string.IsNullOrWhiteSpace(identJson))
                ReplaceIdentifications(conn, tx, request.EmployeeID, DeserializeList<global::HRMS.EmployeeIdentificationInput>(identJson));
            else
                ApplyIdentificationsFromScalars(conn, tx, request.EmployeeID, lines);

            ApplyJsonSection(conn, tx, request.EmployeeID, lines, "Snapshot.Family", "Family.",
                json => ReplaceFamily(conn, tx, request.EmployeeID, DeserializeList<global::HRMS.EmployeeFamilyMemberInput>(json)));
            ApplyJsonSection(conn, tx, request.EmployeeID, lines, "Snapshot.Banks", "Bank.",
                json => ReplaceBanks(conn, tx, request.EmployeeID, DeserializeList<global::HRMS.EmployeeBankInput>(json)));
            ApplyJsonSection(conn, tx, request.EmployeeID, lines, "Snapshot.Education", "Education.",
                json => ReplaceEducation(conn, tx, request.EmployeeID, DeserializeList<global::HRMS.EmployeeEducationInput>(json)));
            ApplyJsonSection(conn, tx, request.EmployeeID, lines, "Snapshot.Certificates", "Certificate.",
                json => ReplaceCertificates(conn, tx, request.EmployeeID, DeserializeList<global::HRMS.EmployeeCertificateInput>(json)));
            ApplyJsonSection(conn, tx, request.EmployeeID, lines, "Snapshot.Documents", "Document.",
                json => ReplaceDocuments(conn, tx, request.EmployeeID, DeserializeList<global::HRMS.EmployeeDocumentInput>(json)));
        }

        private static void ApplyJsonSection(SqlConnection conn, SqlTransaction tx, int employeeId, List<ProfileChangeLine> lines,
            string snapshotKey, string prefix, Action<string> apply)
        {
            var json = FindSectionJson(lines, snapshotKey, prefix);
            if (!string.IsNullOrWhiteSpace(json))
                apply(json);
        }

        private static string FindSectionJson(List<ProfileChangeLine> lines, string snapshotKey, string prefix)
        {
            var snap = lines.FirstOrDefault(l => string.Equals(l.FieldKey, snapshotKey, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(l.NewJson));
            if (snap != null) return snap.NewJson;
            var any = lines.FirstOrDefault(l => l.FieldKey != null
                && l.FieldKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(l.NewJson));
            return any != null ? any.NewJson : null;
        }

        private static List<T> DeserializeList<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("A saved section in this request has no data.");
            try { return Json.Deserialize<List<T>>(json) ?? new List<T>(); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not read every saved field in this request.", ex);
            }
        }

        private static void ApplyPersonalColumns(SqlConnection conn, SqlTransaction tx, int employeeId, List<ProfileChangeLine> lines)
        {
            var sets = new List<string>();
            var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Transaction = tx;
            foreach (var line in lines.Where(l => l.FieldKey != null && l.FieldKey.StartsWith("Personal.", StringComparison.OrdinalIgnoreCase)))
            {
                var key = line.FieldKey.Substring("Personal.".Length);
                string column = null;
                string kind = "text";
                string extraColumn = null;
                object extraValue = null;
                switch (key)
                {
                    case "PhotoPath":
                        column = "PhotoPath";
                        kind = "photo";
                        break;
                    case "EmployeeCode": column = "EmployeeCode"; break;
                    case "FirstName": column = "FirstName"; break;
                    case "LastName": column = "LastName"; break;
                    case "FathersHusbandsName": column = "FathersHusbandsName"; break;
                    case "DisplayName": column = "DisplayName"; break;
                    case "NationalIDNumber": column = "NationalIDNumber"; break;
                    case "MaritalStatus": column = "MaritalStatus"; break;
                    case "Domicile": column = "Domicile"; break;
                    case "Status": column = "Status"; break;
                    case "PrivilegeTier": column = "PrivilegeTier"; break;
                    case "BiometricID": column = "BiometricID"; kind = "int"; break;
                    case "ProbationPeriodDays": column = "ProbationPeriodDays"; kind = "int"; break;
                    case "DateOfBirth": column = "DateOfBirth"; kind = "date"; break;
                    case "DateOfJoining":
                        column = "DateOfJoining";
                        kind = "date";
                        extraColumn = "EmploymentStartDate";
                        extraValue = CoerceValue(line, "date");
                        break;
                    case "ProbationEndDate": column = "ProbationEndDate"; kind = "date"; break;
                    case "ConfirmationDate": column = "ConfirmationDate"; kind = "date"; break;
                    case "BasicSalary": column = "BasicSalary"; kind = "dec"; break;
                    case "ConvenienceAllowance": column = "ConvenienceAllowance"; kind = "dec"; break;
                    case "GenderID":
                        column = "GenderID";
                        kind = "fk";
                        extraColumn = "Gender";
                        extraValue = string.IsNullOrWhiteSpace(line.NewValue) ? (object)DBNull.Value : line.NewValue.Trim();
                        break;
                    case "NationalityID": column = "NationalityID"; kind = "fk"; break;
                    case "ReligionID": column = "ReligionID"; kind = "fk"; break;
                    case "BloodGroupID": column = "BloodGroupID"; kind = "fk"; break;
                    case "LegalEntityID": column = "LegalEntityID"; kind = "fk"; break;
                    case "BusinessUnitID": column = "BusinessUnitID"; kind = "fk"; break;
                    case "DivisionID": column = "DivisionID"; kind = "fk"; break;
                    case "DepartmentID": column = "DepartmentID"; kind = "fk"; break;
                    case "DesignationID":
                        column = "DesignationID";
                        kind = "fk";
                        extraColumn = "Designation";
                        extraValue = string.IsNullOrWhiteSpace(line.NewValue) ? (object)DBNull.Value : line.NewValue.Trim();
                        break;
                    case "SalesTeamID": column = "SalesTeamID"; kind = "fk"; break;
                    case "CostCenterID": column = "CostCenterID"; kind = "fk"; break;
                    case "RegionID": column = "RegionID"; kind = "fk"; break;
                    case "LocationID": column = "LocationID"; kind = "fk"; break;
                    case "DealForBranchID": column = "DealForBranchID"; kind = "fk"; break;
                    case "SecondaryLocationID": column = "SecondaryLocationID"; kind = "fk"; break;
                    case "ExtensionID": column = "ExtensionID"; kind = "fk"; break;
                    case "EmploymentStatusID": column = "EmploymentStatusID"; kind = "fk"; break;
                    case "EmploymentTypeID": column = "EmploymentTypeID"; kind = "fk"; break;
                    case "JobID": column = "JobID"; kind = "fk"; break;
                    case "CityID": column = "CityID"; kind = "fk"; break;
                    case "ProvinceID": column = "ProvinceID"; kind = "fk"; break;
                    case "GradeID": column = "GradeID"; kind = "fk"; break;
                    case "WorkerCategoryID": column = "WorkerCategoryID"; kind = "fk"; break;
                    case "WorkforceSegmentID": column = "WorkforceSegmentID"; kind = "fk"; break;
                    case "WorkArrangementID": column = "WorkArrangementID"; kind = "fk"; break;
                    case "AttendanceMethodID": column = "AttendanceMethodID"; kind = "fk"; break;
                    case "UserID": column = "UserID"; kind = "fk"; break;
                    case "TemporaryResponsibleEmployeeID": column = "TemporaryResponsibleEmployeeID"; kind = "fk"; break;
                    case "PermanentResponsibleEmployeeID": column = "PermanentResponsibleEmployeeID"; kind = "fk"; break;
                    case "BenefitEntitlementID": column = "BenefitEntitlementID"; kind = "fk"; break;
                    case "Gender":
                        if (lines.Any(l => string.Equals(l.FieldKey, "Personal.GenderID", StringComparison.OrdinalIgnoreCase)))
                            continue;
                        column = "Gender";
                        break;
                }
                if (string.IsNullOrEmpty(column)) continue;
                var param = CoerceValue(line, kind);
                var p = "@p" + sets.Count;
                sets.Add(column + "=" + p);
                cmd.Parameters.AddWithValue(p, param);
                if (!string.IsNullOrEmpty(extraColumn))
                {
                    var p2 = "@p" + sets.Count;
                    sets.Add(extraColumn + "=" + p2);
                    cmd.Parameters.AddWithValue(p2, extraValue ?? DBNull.Value);
                }
            }
            if (sets.Count == 0)
            {
                cmd.Dispose();
                return;
            }
            cmd.Parameters.AddWithValue("@ID", employeeId);
            cmd.CommandText = "UPDATE tblEmployee SET " + string.Join(", ", sets) + ", ModifiedOn=GETDATE() WHERE EmployeeID=@ID;";
            try
            {
                cmd.ExecuteNonQuery();
                var photoLine = lines.FirstOrDefault(l => l.IsPhoto);
                if (photoLine != null)
                    TryDeletePhotoFile(photoLine.OldJson);
            }
            finally
            {
                cmd.Dispose();
            }
        }

        private static void ApplyContactsFromScalars(SqlConnection conn, SqlTransaction tx, int employeeId, List<ProfileChangeLine> lines)
        {
            foreach (var line in lines.Where(l => l.FieldKey != null && l.FieldKey.StartsWith("Contact.", StringComparison.OrdinalIgnoreCase)))
            {
                var type = line.FieldKey.Substring("Contact.".Length);
                using (var del = new SqlCommand("DELETE FROM tblEmployeeContact WHERE EmployeeID=@ID AND ContactType=@Type;", conn, tx))
                {
                    del.Parameters.AddWithValue("@ID", employeeId);
                    del.Parameters.AddWithValue("@Type", type);
                    del.ExecuteNonQuery();
                }
                foreach (var part in (line.NewValue ?? "").Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var primary = part.IndexOf(" (primary)", StringComparison.OrdinalIgnoreCase) >= 0;
                    var value = part.Replace(" (primary)", "").Trim();
                    if (string.IsNullOrEmpty(value)) continue;
                    using (var ins = new SqlCommand(@"
INSERT INTO tblEmployeeContact (EmployeeID, ContactType, ContactValue, IsPrimary)
VALUES (@EmployeeID, @Type, @Value, @Primary);", conn, tx))
                    {
                        ins.Parameters.AddWithValue("@EmployeeID", employeeId);
                        ins.Parameters.AddWithValue("@Type", type);
                        ins.Parameters.AddWithValue("@Value", value);
                        ins.Parameters.AddWithValue("@Primary", primary);
                        ins.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void ApplyAddressesFromScalars(SqlConnection conn, SqlTransaction tx, int employeeId, List<ProfileChangeLine> lines)
        {
            foreach (var line in lines.Where(l => l.FieldKey != null && l.FieldKey.StartsWith("Address.", StringComparison.OrdinalIgnoreCase)))
            {
                var type = line.FieldKey.Substring("Address.".Length);
                using (var del = new SqlCommand("DELETE FROM tblEmployeeAddress WHERE EmployeeID=@ID AND AddressType=@Type;", conn, tx))
                {
                    del.Parameters.AddWithValue("@ID", employeeId);
                    del.Parameters.AddWithValue("@Type", type);
                    del.ExecuteNonQuery();
                }
                var text = (line.NewValue ?? "").Trim();
                if (string.IsNullOrEmpty(text)) continue;
                var parts = text.Split(new[] { ", " }, StringSplitOptions.None);
                using (var ins = new SqlCommand(@"
INSERT INTO tblEmployeeAddress (EmployeeID, AddressType, AddressLine, City, ProvinceState, PostalCode, IsPrimary)
VALUES (@EmployeeID, @Type, @Line, @City, @Province, @Postal, 0);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@EmployeeID", employeeId);
                    ins.Parameters.AddWithValue("@Type", type);
                    ins.Parameters.AddWithValue("@Line", parts.Length > 0 ? parts[0] : text);
                    ins.Parameters.AddWithValue("@City", parts.Length > 1 ? parts[1] : "");
                    ins.Parameters.AddWithValue("@Province", parts.Length > 2 ? parts[2] : "");
                    ins.Parameters.AddWithValue("@Postal", parts.Length > 3 ? parts[3] : "");
                    ins.ExecuteNonQuery();
                }
            }
        }

        private static void ApplyIdentificationsFromScalars(SqlConnection conn, SqlTransaction tx, int employeeId, List<ProfileChangeLine> lines)
        {
            foreach (var line in lines.Where(l => l.FieldKey != null && l.FieldKey.StartsWith("Identification.", StringComparison.OrdinalIgnoreCase)))
            {
                var type = line.FieldKey.Substring("Identification.".Length);
                using (var del = new SqlCommand("DELETE FROM tblEmployeeIdentification WHERE EmployeeID=@ID AND DocumentType=@Type;", conn, tx))
                {
                    del.Parameters.AddWithValue("@ID", employeeId);
                    del.Parameters.AddWithValue("@Type", type);
                    del.ExecuteNonQuery();
                }
                var text = (line.NewValue ?? "").Trim();
                if (string.IsNullOrEmpty(text)) continue;
                var expAt = text.LastIndexOf(" exp ", StringComparison.OrdinalIgnoreCase);
                var reg = expAt >= 0 ? text.Substring(0, expAt).Trim() : text;
                var expiry = expAt >= 0 ? text.Substring(expAt + 5).Trim() : "";
                using (var ins = new SqlCommand(@"
INSERT INTO tblEmployeeIdentification (EmployeeID, DocumentType, RegistrationNo, ExpiryDate)
VALUES (@EmployeeID, @Type, @Reg, @Expiry);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@EmployeeID", employeeId);
                    ins.Parameters.AddWithValue("@Type", type);
                    ins.Parameters.AddWithValue("@Reg", reg);
                    DateTime expiryDate;
                    ins.Parameters.AddWithValue("@Expiry", DateTime.TryParse(expiry, out expiryDate) ? (object)expiryDate : DBNull.Value);
                    ins.ExecuteNonQuery();
                }
            }
        }

        private static void ReplaceContacts(SqlConnection conn, SqlTransaction tx, int employeeId, List<global::HRMS.EmployeeContactInput> rows)
        {
            using (var del = new SqlCommand("DELETE FROM tblEmployeeContact WHERE EmployeeID=@ID;", conn, tx))
            {
                del.Parameters.AddWithValue("@ID", employeeId);
                del.ExecuteNonQuery();
            }
            foreach (var c in SanitizeContacts(rows))
            {
                using (var ins = new SqlCommand(@"
INSERT INTO tblEmployeeContact (EmployeeID, ContactType, ContactValue, IsPrimary)
VALUES (@EmployeeID, @Type, @Value, @Primary);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@EmployeeID", employeeId);
                    ins.Parameters.AddWithValue("@Type", c.ContactType);
                    ins.Parameters.AddWithValue("@Value", (object)c.ContactValue ?? "");
                    ins.Parameters.AddWithValue("@Primary", c.IsPrimary);
                    ins.ExecuteNonQuery();
                }
            }
        }

        private static void ReplaceAddresses(SqlConnection conn, SqlTransaction tx, int employeeId, List<global::HRMS.EmployeeAddressInput> rows)
        {
            using (var del = new SqlCommand("DELETE FROM tblEmployeeAddress WHERE EmployeeID=@ID;", conn, tx))
            {
                del.Parameters.AddWithValue("@ID", employeeId);
                del.ExecuteNonQuery();
            }
            foreach (var a in SanitizeAddresses(rows))
            {
                using (var ins = new SqlCommand(@"
INSERT INTO tblEmployeeAddress (EmployeeID, AddressType, AddressLine, City, ProvinceState, PostalCode, IsPrimary)
VALUES (@EmployeeID, @Type, @Line, @City, @Province, @Postal, @Primary);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@EmployeeID", employeeId);
                    ins.Parameters.AddWithValue("@Type", a.AddressType);
                    ins.Parameters.AddWithValue("@Line", a.AddressLine ?? "");
                    ins.Parameters.AddWithValue("@City", (object)a.City ?? "");
                    ins.Parameters.AddWithValue("@Province", (object)a.ProvinceState ?? "");
                    ins.Parameters.AddWithValue("@Postal", (object)a.PostalCode ?? "");
                    ins.Parameters.AddWithValue("@Primary", a.IsPrimary);
                    ins.ExecuteNonQuery();
                }
            }
        }

        private static void ReplaceIdentifications(SqlConnection conn, SqlTransaction tx, int employeeId, List<global::HRMS.EmployeeIdentificationInput> rows)
        {
            using (var del = new SqlCommand("DELETE FROM tblEmployeeIdentification WHERE EmployeeID=@ID;", conn, tx))
            {
                del.Parameters.AddWithValue("@ID", employeeId);
                del.ExecuteNonQuery();
            }
            foreach (var r in SanitizeIdents(rows))
            {
                using (var ins = new SqlCommand(@"
INSERT INTO tblEmployeeIdentification (EmployeeID, DocumentType, RegistrationNo, IssueDate, ExpiryDate, BlacklistComment)
VALUES (@EmployeeID, @Type, @Reg, @Issue, @Expiry, @Blacklist);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@EmployeeID", employeeId);
                    ins.Parameters.AddWithValue("@Type", r.DocumentType);
                    ins.Parameters.AddWithValue("@Reg", r.RegistrationNo ?? "");
                    DateTime issue, expiry;
                    ins.Parameters.AddWithValue("@Issue", DateTime.TryParse(r.IssueDate, out issue) ? (object)issue : DBNull.Value);
                    ins.Parameters.AddWithValue("@Expiry", DateTime.TryParse(r.ExpiryDate, out expiry) ? (object)expiry : DBNull.Value);
                    ins.Parameters.AddWithValue("@Blacklist", string.IsNullOrWhiteSpace(r.BlacklistComment) ? (object)DBNull.Value : r.BlacklistComment);
                    ins.ExecuteNonQuery();
                }
            }
        }

        private static void DeleteChildren(SqlConnection conn, SqlTransaction tx, string table, int employeeId)
        {
            using (var del = new SqlCommand("DELETE FROM " + table + " WHERE EmployeeID=@ID;", conn, tx))
            {
                del.Parameters.AddWithValue("@ID", employeeId);
                del.ExecuteNonQuery();
            }
        }

        private static object DateOrDb(string value)
        {
            DateTime d;
            return DateTime.TryParse(value, out d) ? (object)d.Date : DBNull.Value;
        }

        private static object TextOrDb(string value)
            => string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();

        private static void ReplaceFamily(SqlConnection conn, SqlTransaction tx, int employeeId, List<global::HRMS.EmployeeFamilyMemberInput> rows)
        {
            DeleteChildren(conn, tx, "tblEmployeeFamilyMember", employeeId);
            foreach (var m in rows ?? new List<global::HRMS.EmployeeFamilyMemberInput>())
            {
                if (string.IsNullOrWhiteSpace(m.MemberName)) continue;
                using (var ins = new SqlCommand(@"
INSERT INTO tblEmployeeFamilyMember (EmployeeID, MemberName, Relationship, Gender, DateOfBirth, ContactNumber, IsDependent)
VALUES (@EmployeeID, @Name, @Rel, @Gender, @Dob, @Phone, @Dep);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@EmployeeID", employeeId);
                    ins.Parameters.AddWithValue("@Name", m.MemberName.Trim());
                    ins.Parameters.AddWithValue("@Rel", TextOrDb(m.Relationship));
                    ins.Parameters.AddWithValue("@Gender", TextOrDb(m.Gender));
                    ins.Parameters.AddWithValue("@Dob", DateOrDb(m.DateOfBirth));
                    ins.Parameters.AddWithValue("@Phone", TextOrDb(m.ContactNumber));
                    ins.Parameters.AddWithValue("@Dep", m.IsDependent);
                    ins.ExecuteNonQuery();
                }
            }
        }

        private static void ReplaceBanks(SqlConnection conn, SqlTransaction tx, int employeeId, List<global::HRMS.EmployeeBankInput> rows)
        {
            DeleteChildren(conn, tx, "tblEmployeeBank", employeeId);
            foreach (var b in rows ?? new List<global::HRMS.EmployeeBankInput>())
            {
                if (b.BankID <= 0) continue;
                using (var ins = new SqlCommand(@"
INSERT INTO tblEmployeeBank (EmployeeID, BankID, BankCode, LocationName, BankGroupID, IBAN, SwiftBICCode, CurrencyCode, AccountVerificationStatus, IsPrimary)
VALUES (@EmployeeID, @BankID, @Code, @Loc, @Group, @Iban, @Swift, @Cur, @Status, @Primary);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@EmployeeID", employeeId);
                    ins.Parameters.AddWithValue("@BankID", b.BankID);
                    ins.Parameters.AddWithValue("@Code", TextOrDb(b.BankCode));
                    ins.Parameters.AddWithValue("@Loc", TextOrDb(b.LocationName));
                    ins.Parameters.AddWithValue("@Group", b.BankGroupID > 0 ? (object)b.BankGroupID : DBNull.Value);
                    ins.Parameters.AddWithValue("@Iban", TextOrDb(b.IBAN));
                    ins.Parameters.AddWithValue("@Swift", TextOrDb(b.SwiftBICCode));
                    ins.Parameters.AddWithValue("@Cur", TextOrDb(b.CurrencyCode));
                    ins.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(b.AccountVerificationStatus) ? "Pending" : b.AccountVerificationStatus.Trim());
                    ins.Parameters.AddWithValue("@Primary", b.IsPrimary);
                    ins.ExecuteNonQuery();
                }
            }
        }

        private static void ReplaceEducation(SqlConnection conn, SqlTransaction tx, int employeeId, List<global::HRMS.EmployeeEducationInput> rows)
        {
            DeleteChildren(conn, tx, "tblEmployeeEducation", employeeId);
            foreach (var e in rows ?? new List<global::HRMS.EmployeeEducationInput>())
            {
                if (string.IsNullOrWhiteSpace(e.HighestQualification) && string.IsNullOrWhiteSpace(e.DegreeCertificate) && string.IsNullOrWhiteSpace(e.Institution))
                    continue;
                using (var ins = new SqlCommand(@"
INSERT INTO tblEmployeeEducation (EmployeeID, HighestQualification, DegreeCertificate, Specialization, Institution, YearOfPassing, GradeCGPA)
VALUES (@EmployeeID, @Qual, @Deg, @Spec, @Inst, @Year, @Grade);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@EmployeeID", employeeId);
                    ins.Parameters.AddWithValue("@Qual", TextOrDb(e.HighestQualification));
                    ins.Parameters.AddWithValue("@Deg", TextOrDb(e.DegreeCertificate));
                    ins.Parameters.AddWithValue("@Spec", TextOrDb(e.Specialization));
                    ins.Parameters.AddWithValue("@Inst", TextOrDb(e.Institution));
                    int year;
                    ins.Parameters.AddWithValue("@Year", int.TryParse(e.YearOfPassing, out year) ? (object)year : DBNull.Value);
                    ins.Parameters.AddWithValue("@Grade", TextOrDb(e.GradeCGPA));
                    ins.ExecuteNonQuery();
                }
            }
        }

        private static void ReplaceCertificates(SqlConnection conn, SqlTransaction tx, int employeeId, List<global::HRMS.EmployeeCertificateInput> rows)
        {
            DeleteChildren(conn, tx, "tblEmployeeCertificate", employeeId);
            foreach (var c in rows ?? new List<global::HRMS.EmployeeCertificateInput>())
            {
                if (string.IsNullOrWhiteSpace(c.CertificationName) && string.IsNullOrWhiteSpace(c.CertificateNumber) && string.IsNullOrWhiteSpace(c.CertificateCopyPath))
                    continue;
                using (var ins = new SqlCommand(@"
INSERT INTO tblEmployeeCertificate (EmployeeID, CertificationName, CertificationBody, CertificateNumber, IssueDate, ExpiryDate, RenewalRequired, CertificateCopyPath)
VALUES (@EmployeeID, @Name, @Body, @Num, @Issue, @Exp, @Renew, @Path);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@EmployeeID", employeeId);
                    ins.Parameters.AddWithValue("@Name", TextOrDb(c.CertificationName));
                    ins.Parameters.AddWithValue("@Body", TextOrDb(c.CertificationBody));
                    ins.Parameters.AddWithValue("@Num", TextOrDb(c.CertificateNumber));
                    ins.Parameters.AddWithValue("@Issue", DateOrDb(c.IssueDate));
                    ins.Parameters.AddWithValue("@Exp", DateOrDb(c.ExpiryDate));
                    ins.Parameters.AddWithValue("@Renew", c.RenewalRequired);
                    ins.Parameters.AddWithValue("@Path", TextOrDb(c.CertificateCopyPath));
                    ins.ExecuteNonQuery();
                }
            }
        }

        private static void ReplaceDocuments(SqlConnection conn, SqlTransaction tx, int employeeId, List<global::HRMS.EmployeeDocumentInput> rows)
        {
            DeleteChildren(conn, tx, "tblEmployeeDocument", employeeId);
            foreach (var d in rows ?? new List<global::HRMS.EmployeeDocumentInput>())
            {
                if (d.DocumentTypeID <= 0 && string.IsNullOrWhiteSpace(d.DocumentNumber) && string.IsNullOrWhiteSpace(d.DocumentPath))
                    continue;
                using (var ins = new SqlCommand(@"
INSERT INTO tblEmployeeDocument (EmployeeID, DocumentTypeID, DocumentNumber, IssueDate, ExpiryDate, Remarks, DocumentPath, OriginalFileName, VerificationStatus)
VALUES (@EmployeeID, @Type, @Num, @Issue, @Exp, @Remarks, @Path, @Orig, @Status);", conn, tx))
                {
                    ins.Parameters.AddWithValue("@EmployeeID", employeeId);
                    ins.Parameters.AddWithValue("@Type", d.DocumentTypeID > 0 ? (object)d.DocumentTypeID : DBNull.Value);
                    ins.Parameters.AddWithValue("@Num", TextOrDb(d.DocumentNumber));
                    ins.Parameters.AddWithValue("@Issue", DateOrDb(d.IssueDate));
                    ins.Parameters.AddWithValue("@Exp", DateOrDb(d.ExpiryDate));
                    ins.Parameters.AddWithValue("@Remarks", TextOrDb(d.Remarks));
                    ins.Parameters.AddWithValue("@Path", TextOrDb(d.DocumentPath));
                    ins.Parameters.AddWithValue("@Orig", TextOrDb(d.OriginalFileName));
                    ins.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(d.VerificationStatus) ? "Pending" : d.VerificationStatus.Trim());
                    ins.ExecuteNonQuery();
                }
            }
        }

        private global::HRMS.EmployeeInput LoadPersonal(int employeeId)
        {
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT EmployeeCode, FirstName, LastName, FathersHusbandsName, DisplayName, NationalIDNumber, BiometricID,
       Gender, GenderID, DateOfBirth, MaritalStatus, DepartmentID, DivisionID,
       NationalityID, ReligionID, LanguageID,
       WorkerCategoryID, EmploymentTypeID, EmploymentStatusID,
       WorkforceSegmentID, WorkArrangementID, AttendanceMethodID, LegalEntityID, BusinessUnitID,
       SalesTeamID, CostCenterID, RegionID, LocationID, DealForBranchID, SecondaryLocationID, JobID,
       CityID, ProvinceID, GradeID, DesignationID, PrivilegeTier, ExtensionID, Domicile,
       BloodGroupID, BenefitEntitlementID, UserID,
       TemporaryResponsibleEmployeeID, PermanentResponsibleEmployeeID,
       Designation, DateOfJoining, EmploymentStartDate, ProbationPeriodDays,
       ProbationEndDate, ConfirmationDate, BasicSalary, ConvenienceAllowance, Status, PhotoPath
FROM tblEmployee WHERE EmployeeID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", employeeId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;
                    return new global::HRMS.EmployeeInput
                    {
                        EmployeeID = employeeId,
                        EmployeeCode = Str(dr, "EmployeeCode"),
                        FirstName = Str(dr, "FirstName"),
                        LastName = Str(dr, "LastName"),
                        FathersHusbandsName = Str(dr, "FathersHusbandsName"),
                        DisplayName = Str(dr, "DisplayName"),
                        NationalIDNumber = Str(dr, "NationalIDNumber"),
                        BiometricID = dr["BiometricID"] == DBNull.Value ? "" : Convert.ToString(dr["BiometricID"]),
                        Gender = Str(dr, "Gender"),
                        GenderID = IntCol(dr, "GenderID"),
                        DateOfBirth = DateCol(dr, "DateOfBirth"),
                        MaritalStatus = Str(dr, "MaritalStatus"),
                        DepartmentID = IntCol(dr, "DepartmentID"),
                        DivisionID = IntCol(dr, "DivisionID"),
                        NationalityID = IntCol(dr, "NationalityID"),
                        ReligionID = IntCol(dr, "ReligionID"),
                        LanguageID = IntCol(dr, "LanguageID"),
                        WorkerCategoryID = IntCol(dr, "WorkerCategoryID"),
                        EmploymentTypeID = IntCol(dr, "EmploymentTypeID"),
                        EmploymentStatusID = IntCol(dr, "EmploymentStatusID"),
                        WorkforceSegmentID = IntCol(dr, "WorkforceSegmentID"),
                        WorkArrangementID = IntCol(dr, "WorkArrangementID"),
                        AttendanceMethodID = IntCol(dr, "AttendanceMethodID"),
                        LegalEntityID = IntCol(dr, "LegalEntityID"),
                        BusinessUnitID = IntCol(dr, "BusinessUnitID"),
                        SalesTeamID = IntCol(dr, "SalesTeamID"),
                        CostCenterID = IntCol(dr, "CostCenterID"),
                        RegionID = IntCol(dr, "RegionID"),
                        LocationID = IntCol(dr, "LocationID"),
                        DealForBranchID = IntCol(dr, "DealForBranchID"),
                        SecondaryLocationID = IntCol(dr, "SecondaryLocationID"),
                        JobID = IntCol(dr, "JobID"),
                        CityID = IntCol(dr, "CityID"),
                        ProvinceID = IntCol(dr, "ProvinceID"),
                        GradeID = IntCol(dr, "GradeID"),
                        DesignationID = IntCol(dr, "DesignationID"),
                        PrivilegeTier = Str(dr, "PrivilegeTier"),
                        ExtensionID = IntCol(dr, "ExtensionID"),
                        Domicile = Str(dr, "Domicile"),
                        BloodGroupID = IntCol(dr, "BloodGroupID"),
                        BenefitEntitlementID = IntCol(dr, "BenefitEntitlementID"),
                        UserID = IntCol(dr, "UserID"),
                        TemporaryResponsibleEmployeeID = IntCol(dr, "TemporaryResponsibleEmployeeID"),
                        PermanentResponsibleEmployeeID = IntCol(dr, "PermanentResponsibleEmployeeID"),
                        Designation = Str(dr, "Designation"),
                        DateOfJoining = DateCol(dr, "DateOfJoining"),
                        EmploymentStartDate = DateCol(dr, "EmploymentStartDate"),
                        ProbationPeriodDays = dr["ProbationPeriodDays"] == DBNull.Value ? "" : Convert.ToString(dr["ProbationPeriodDays"]),
                        ProbationEndDate = DateCol(dr, "ProbationEndDate"),
                        ConfirmationDate = DateCol(dr, "ConfirmationDate"),
                        BasicSalary = MoneyCol(dr, "BasicSalary"),
                        ConvenienceAllowance = MoneyCol(dr, "ConvenienceAllowance"),
                        Status = string.IsNullOrWhiteSpace(Str(dr, "Status")) ? "Active" : Str(dr, "Status"),
                        PhotoPath = Str(dr, "PhotoPath")
                    };
                }
            }
        }

        private List<global::HRMS.EmployeeContactInput> LoadContacts(int employeeId)
        {
            var list = new List<global::HRMS.EmployeeContactInput>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT ContactType, ContactValue, IsPrimary FROM tblEmployeeContact WHERE EmployeeID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", employeeId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new global::HRMS.EmployeeContactInput
                        {
                            ContactType = Str(dr, "ContactType"),
                            ContactValue = Str(dr, "ContactValue"),
                            IsPrimary = dr["IsPrimary"] != DBNull.Value && Convert.ToBoolean(dr["IsPrimary"])
                        });
                    }
                }
            }
            return list;
        }

        private List<global::HRMS.EmployeeAddressInput> LoadAddresses(int employeeId)
        {
            var list = new List<global::HRMS.EmployeeAddressInput>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT AddressType, AddressLine, City, ProvinceState, PostalCode, IsPrimary FROM tblEmployeeAddress WHERE EmployeeID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", employeeId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new global::HRMS.EmployeeAddressInput
                        {
                            AddressType = Str(dr, "AddressType"),
                            AddressLine = Str(dr, "AddressLine"),
                            City = Str(dr, "City"),
                            ProvinceState = Str(dr, "ProvinceState"),
                            PostalCode = Str(dr, "PostalCode"),
                            IsPrimary = dr["IsPrimary"] != DBNull.Value && Convert.ToBoolean(dr["IsPrimary"])
                        });
                    }
                }
            }
            return list;
        }

        private List<global::HRMS.EmployeeIdentificationInput> LoadIdentifications(int employeeId)
        {
            var list = new List<global::HRMS.EmployeeIdentificationInput>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT DocumentType, RegistrationNo, IssueDate, ExpiryDate, BlacklistComment FROM tblEmployeeIdentification WHERE EmployeeID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", employeeId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new global::HRMS.EmployeeIdentificationInput
                        {
                            DocumentType = Str(dr, "DocumentType"),
                            RegistrationNo = Str(dr, "RegistrationNo"),
                            IssueDate = dr["IssueDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["IssueDate"]).ToString("yyyy-MM-dd"),
                            ExpiryDate = dr["ExpiryDate"] == DBNull.Value ? "" : Convert.ToDateTime(dr["ExpiryDate"]).ToString("yyyy-MM-dd"),
                            BlacklistComment = Str(dr, "BlacklistComment")
                        });
                    }
                }
            }
            return list;
        }

        private List<T> LoadRows<T>(int employeeId, string sql, Func<IDataRecord, T> map)
        {
            var list = new List<T>();
            try
            {
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", employeeId);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                            list.Add(map(dr));
                    }
                }
            }
            catch { }
            return list;
        }

        private List<global::HRMS.EmployeeFamilyMemberInput> LoadFamily(int employeeId)
            => LoadRows(employeeId, @"
SELECT MemberName, Relationship, Gender, DateOfBirth, ContactNumber, IsDependent
FROM tblEmployeeFamilyMember WHERE EmployeeID=@ID;", dr => new global::HRMS.EmployeeFamilyMemberInput
            {
                MemberName = Str(dr, "MemberName"),
                Relationship = Str(dr, "Relationship"),
                Gender = Str(dr, "Gender"),
                DateOfBirth = DateCol(dr, "DateOfBirth"),
                ContactNumber = Str(dr, "ContactNumber"),
                IsDependent = dr["IsDependent"] != DBNull.Value && Convert.ToBoolean(dr["IsDependent"])
            });

        private List<global::HRMS.EmployeeBankInput> LoadBanks(int employeeId)
            => LoadRows(employeeId, @"
SELECT BankID, BankCode, LocationName, BankGroupID, IBAN, SwiftBICCode, CurrencyCode, AccountVerificationStatus, IsPrimary
FROM tblEmployeeBank WHERE EmployeeID=@ID;", dr => new global::HRMS.EmployeeBankInput
            {
                BankID = IntCol(dr, "BankID"),
                BankCode = Str(dr, "BankCode"),
                LocationName = Str(dr, "LocationName"),
                BankGroupID = IntCol(dr, "BankGroupID"),
                IBAN = Str(dr, "IBAN"),
                SwiftBICCode = Str(dr, "SwiftBICCode"),
                CurrencyCode = Str(dr, "CurrencyCode"),
                AccountVerificationStatus = string.IsNullOrWhiteSpace(Str(dr, "AccountVerificationStatus")) ? "Pending" : Str(dr, "AccountVerificationStatus"),
                IsPrimary = dr["IsPrimary"] != DBNull.Value && Convert.ToBoolean(dr["IsPrimary"])
            });

        private List<global::HRMS.EmployeeEducationInput> LoadEducation(int employeeId)
            => LoadRows(employeeId, @"
SELECT HighestQualification, DegreeCertificate, Specialization, Institution, YearOfPassing, GradeCGPA
FROM tblEmployeeEducation WHERE EmployeeID=@ID;", dr => new global::HRMS.EmployeeEducationInput
            {
                HighestQualification = Str(dr, "HighestQualification"),
                DegreeCertificate = Str(dr, "DegreeCertificate"),
                Specialization = Str(dr, "Specialization"),
                Institution = Str(dr, "Institution"),
                YearOfPassing = dr["YearOfPassing"] == DBNull.Value ? "" : Convert.ToString(dr["YearOfPassing"]),
                GradeCGPA = Str(dr, "GradeCGPA")
            });

        private List<global::HRMS.EmployeeCertificateInput> LoadCertificates(int employeeId)
            => LoadRows(employeeId, @"
SELECT CertificationName, CertificationBody, CertificateNumber, IssueDate, ExpiryDate, RenewalRequired, CertificateCopyPath
FROM tblEmployeeCertificate WHERE EmployeeID=@ID;", dr => new global::HRMS.EmployeeCertificateInput
            {
                CertificationName = Str(dr, "CertificationName"),
                CertificationBody = Str(dr, "CertificationBody"),
                CertificateNumber = Str(dr, "CertificateNumber"),
                IssueDate = DateCol(dr, "IssueDate"),
                ExpiryDate = DateCol(dr, "ExpiryDate"),
                RenewalRequired = dr["RenewalRequired"] != DBNull.Value && Convert.ToBoolean(dr["RenewalRequired"]),
                CertificateCopyPath = Str(dr, "CertificateCopyPath")
            });

        private List<global::HRMS.EmployeeDocumentInput> LoadDocuments(int employeeId)
            => LoadRows(employeeId, @"
SELECT d.DocumentTypeID, ISNULL(dt.DocumentTypeName,'') AS DocumentTypeName, d.DocumentNumber, d.IssueDate, d.ExpiryDate,
       d.Remarks, d.DocumentPath, d.OriginalFileName, d.VerificationStatus
FROM tblEmployeeDocument d
LEFT JOIN tblDocumentType dt ON dt.DocumentTypeID = d.DocumentTypeID
WHERE d.EmployeeID=@ID;", dr => new global::HRMS.EmployeeDocumentInput
            {
                DocumentTypeID = IntCol(dr, "DocumentTypeID"),
                DocumentTypeName = Str(dr, "DocumentTypeName"),
                DocumentNumber = Str(dr, "DocumentNumber"),
                IssueDate = DateCol(dr, "IssueDate"),
                ExpiryDate = DateCol(dr, "ExpiryDate"),
                Remarks = Str(dr, "Remarks"),
                DocumentPath = Str(dr, "DocumentPath"),
                OriginalFileName = Str(dr, "OriginalFileName"),
                VerificationStatus = string.IsNullOrWhiteSpace(Str(dr, "VerificationStatus")) ? "Pending" : Str(dr, "VerificationStatus")
            });

        private List<ProfileChangeRequest> ListBySql(string where, Action<SqlCommand> bind)
        {
            EnsureSchema();
            var list = new List<ProfileChangeRequest>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT r.RequestID, r.RequestCode, r.EmployeeID, r.SubmittedByUserID, r.Status, r.SubmittedOn,
       r.DecidedOn, r.DecidedByUserID, r.DecisionComments,
       ISNULL(e.FirstName,'') + ' ' + ISNULL(e.LastName,'') AS EmployeeName,
       ISNULL(e.EmployeeCode,'') AS EmployeeCode,
       ISNULL(su.FullName, su.Username) AS SubmittedByName,
       ISNULL(du.FullName, du.Username) AS DecidedByName
FROM tblProfileChangeRequest r
LEFT JOIN tblEmployee e ON e.EmployeeID = r.EmployeeID
LEFT JOIN tblUser su ON su.UserID = r.SubmittedByUserID
LEFT JOIN tblUser du ON du.UserID = r.DecidedByUserID
" + where, conn))
            {
                bind(cmd);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        list.Add(MapHeader(dr));
                }
            }
            return list;
        }

        private static ProfileChangeRequest MapHeader(SqlDataReader dr)
        {
            return new ProfileChangeRequest
            {
                RequestID = Convert.ToInt32(dr["RequestID"]),
                RequestCode = dr["RequestCode"].ToString() ?? "",
                EmployeeID = Convert.ToInt32(dr["EmployeeID"]),
                SubmittedByUserID = Convert.ToInt32(dr["SubmittedByUserID"]),
                Status = dr["Status"].ToString() ?? "",
                SubmittedOn = Convert.ToDateTime(dr["SubmittedOn"]),
                DecidedOn = dr["DecidedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["DecidedOn"]),
                DecidedByUserID = dr["DecidedByUserID"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["DecidedByUserID"]),
                DecisionComments = dr["DecisionComments"] == DBNull.Value ? "" : dr["DecisionComments"].ToString(),
                EmployeeName = dr["EmployeeName"].ToString() ?? "",
                EmployeeCode = dr["EmployeeCode"].ToString() ?? "",
                SubmittedByName = dr["SubmittedByName"] == DBNull.Value ? "" : dr["SubmittedByName"].ToString(),
                DecidedByName = dr["DecidedByName"] == DBNull.Value ? "" : dr["DecidedByName"].ToString()
            };
        }

        private List<ProfileChangeLine> LoadLines(int requestId)
        {
            var list = new List<ProfileChangeLine>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT FieldKey, FieldLabel, Section, OldValue, NewValue, OldJson, NewJson, IsSnapshot
FROM tblProfileChangeLine WHERE RequestID=@ID ORDER BY LineID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", requestId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new ProfileChangeLine
                        {
                            FieldKey = Str(dr, "FieldKey"),
                            FieldLabel = Str(dr, "FieldLabel"),
                            Section = Str(dr, "Section"),
                            OldValue = Str(dr, "OldValue"),
                            NewValue = Str(dr, "NewValue"),
                            OldJson = Str(dr, "OldJson"),
                            NewJson = Str(dr, "NewJson"),
                            IsSnapshot = Convert.ToBoolean(dr["IsSnapshot"])
                        });
                    }
                }
            }
            return list;
        }

        private List<ProfileChangeHistoryItem> LoadHistory(int requestId)
        {
            var list = new List<ProfileChangeHistoryItem>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT h.Status, h.Comments, h.ActionOn, ISNULL(u.FullName, u.Username) AS ActorName
FROM tblProfileChangeHistory h
LEFT JOIN tblUser u ON u.UserID = h.ActorUserID
WHERE h.RequestID=@ID ORDER BY h.ActionOn;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", requestId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new ProfileChangeHistoryItem
                        {
                            Status = Str(dr, "Status"),
                            Comments = Str(dr, "Comments"),
                            ActionOn = Convert.ToDateTime(dr["ActionOn"]),
                            ActorName = Str(dr, "ActorName")
                        });
                    }
                }
            }
            return list;
        }

        private void InsertLine(SqlConnection conn, SqlTransaction tx, int requestId, ProfileChangeLine line)
        {
            using (var cmd = new SqlCommand(@"
INSERT INTO tblProfileChangeLine (RequestID, FieldKey, FieldLabel, Section, OldValue, NewValue, OldJson, NewJson, IsSnapshot)
VALUES (@RequestID, @FieldKey, @FieldLabel, @Section, @OldValue, @NewValue, @OldJson, @NewJson, @IsSnapshot);", conn, tx))
            {
                cmd.Parameters.AddWithValue("@RequestID", requestId);
                cmd.Parameters.AddWithValue("@FieldKey", line.FieldKey ?? "");
                cmd.Parameters.AddWithValue("@FieldLabel", line.FieldLabel ?? "");
                cmd.Parameters.AddWithValue("@Section", line.Section ?? "");
                cmd.Parameters.Add("@OldValue", SqlDbType.NVarChar, -1).Value = (object)line.OldValue ?? "";
                cmd.Parameters.Add("@NewValue", SqlDbType.NVarChar, -1).Value = (object)line.NewValue ?? "";
                cmd.Parameters.Add("@OldJson", SqlDbType.NVarChar, -1).Value = string.IsNullOrEmpty(line.OldJson) ? (object)DBNull.Value : line.OldJson;
                cmd.Parameters.Add("@NewJson", SqlDbType.NVarChar, -1).Value = string.IsNullOrEmpty(line.NewJson) ? (object)DBNull.Value : line.NewJson;
                cmd.Parameters.AddWithValue("@IsSnapshot", line.IsSnapshot);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertHistory(SqlConnection conn, SqlTransaction tx, int requestId, string status, int actorUserId, string comments)
        {
            using (var cmd = new SqlCommand(@"
INSERT INTO tblProfileChangeHistory (RequestID, Status, ActorUserID, Comments, ActionOn)
VALUES (@RequestID, @Status, @Actor, @Comments, GETDATE());", conn, tx))
            {
                cmd.Parameters.AddWithValue("@RequestID", requestId);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Actor", actorUserId);
                cmd.Parameters.AddWithValue("@Comments", string.IsNullOrWhiteSpace(comments) ? (object)DBNull.Value : comments);
                cmd.ExecuteNonQuery();
            }
        }

        private string NextCode(SqlConnection conn, SqlTransaction tx)
        {
            using (var cmd = new SqlCommand("SELECT TOP 1 RequestCode FROM tblProfileChangeRequest WHERE RequestCode LIKE 'PCR-%' ORDER BY RequestID DESC;", conn, tx))
            {
                var last = cmd.ExecuteScalar() as string;
                int num = 0;
                if (!string.IsNullOrEmpty(last) && last.Length >= 5)
                    int.TryParse(last.Substring(4), out num);
                return "PCR-" + (num + 1).ToString("D5");
            }
        }

        private void NotifyUser(int userId, string title, string message, string link, int relatedId)
            => _inbox.Notify(userId, title, message, link, RelatedType, relatedId);

        private void MarkRelatedInboxRead(int requestId)
        {
            if (!_auth.CurrentUserId.HasValue) return;
            _inbox.MarkRelatedRead(RelatedType, requestId, _auth.CurrentUserId.Value);
        }

        private List<int> GetReviewerUserIds()
        {
            var ids = new HashSet<int>();
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
SELECT UserID FROM tblUser WHERE IsActive=1 AND IsAdmin=1
UNION
SELECT p.UserID FROM tblUserPermission p
INNER JOIN tblUser u ON u.UserID = p.UserID AND u.IsActive=1
WHERE (p.FormKey=N'ProfileChange' AND (p.CanApprove=1 OR p.CanRead=1))
   OR (p.FormKey=N'EmployeeMaster' AND p.CanApprove=1)
   OR (p.FormKey=N'LeaveHR' AND p.CanRead=1)
   OR (p.FormKey=N'EmployeeMaster' AND p.CanWrite=1);", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        ids.Add(dr.GetInt32(0));
                }
            }
            return ids.ToList();
        }

        private string EmployeeLabel(int employeeId)
        {
            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT ISNULL(EmployeeCode,'') + ' ' + ISNULL(FirstName,'') + ' ' + ISNULL(LastName,'') FROM tblEmployee WHERE EmployeeID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", employeeId);
                conn.Open();
                return (cmd.ExecuteScalar() as string ?? "Employee").Trim();
            }
        }

        private static string FormatContact(global::HRMS.EmployeeContactInput c)
            => (c.ContactValue ?? "") + (c.IsPrimary ? " (primary)" : "");

        private static string FormatContacts(IEnumerable<global::HRMS.EmployeeContactInput> rows)
            => string.Join("; ", SanitizeContacts(rows).Select(c => c.ContactType + ": " + FormatContact(c)));

        private static string FormatAddress(global::HRMS.EmployeeAddressInput a)
            => string.Join(", ", new[] { a.AddressLine, a.City, a.ProvinceState, a.PostalCode }.Where(x => !string.IsNullOrWhiteSpace(x)));

        private static string FormatAddresses(IEnumerable<global::HRMS.EmployeeAddressInput> rows)
            => string.Join("; ", SanitizeAddresses(rows).Select(a => a.AddressType + ": " + FormatAddress(a)));

        private static string FormatIdent(global::HRMS.EmployeeIdentificationInput r)
            => (r.RegistrationNo ?? "") + (string.IsNullOrWhiteSpace(r.ExpiryDate) ? "" : " exp " + r.ExpiryDate);

        private static string FormatIdents(IEnumerable<global::HRMS.EmployeeIdentificationInput> rows)
            => string.Join("; ", SanitizeIdents(rows).Select(r => r.DocumentType + ": " + FormatIdent(r)));

        private static List<global::HRMS.EmployeeContactInput> SanitizeContacts(IEnumerable<global::HRMS.EmployeeContactInput> rows)
            => (rows ?? Enumerable.Empty<global::HRMS.EmployeeContactInput>())
                .Where(c => !string.IsNullOrWhiteSpace(c.ContactType) && !string.IsNullOrWhiteSpace(c.ContactValue))
                .Select(c => new global::HRMS.EmployeeContactInput
                {
                    ContactType = c.ContactType.Trim(),
                    ContactValue = (c.ContactValue ?? "").Trim(),
                    ContactName = c.ContactName ?? "",
                    Relationship = c.Relationship ?? "",
                    IsPrimary = c.IsPrimary
                }).ToList();

        private static List<global::HRMS.EmployeeAddressInput> SanitizeAddresses(IEnumerable<global::HRMS.EmployeeAddressInput> rows)
            => (rows ?? Enumerable.Empty<global::HRMS.EmployeeAddressInput>())
                .Where(a => !string.IsNullOrWhiteSpace(a.AddressLine) || !string.IsNullOrWhiteSpace(a.City))
                .Select(a => new global::HRMS.EmployeeAddressInput
                {
                    AddressType = string.IsNullOrWhiteSpace(a.AddressType) ? "Other" : a.AddressType.Trim(),
                    AddressLine = (a.AddressLine ?? "").Trim(),
                    City = (a.City ?? "").Trim(),
                    ProvinceState = (a.ProvinceState ?? "").Trim(),
                    PostalCode = (a.PostalCode ?? "").Trim(),
                    IsPrimary = a.IsPrimary
                }).ToList();

        private static List<global::HRMS.EmployeeIdentificationInput> SanitizeIdents(IEnumerable<global::HRMS.EmployeeIdentificationInput> rows)
            => (rows ?? Enumerable.Empty<global::HRMS.EmployeeIdentificationInput>())
                .Where(r => !string.IsNullOrWhiteSpace(r.DocumentType) && !string.IsNullOrWhiteSpace(r.RegistrationNo))
                .Select(r => new global::HRMS.EmployeeIdentificationInput
                {
                    DocumentType = r.DocumentType.Trim(),
                    RegistrationNo = r.RegistrationNo.Trim(),
                    IssueDate = r.IssueDate ?? "",
                    ExpiryDate = r.ExpiryDate ?? "",
                    BlacklistComment = r.BlacklistComment ?? ""
                }).ToList();

        private static string Str(IDataRecord dr, string col)
            => dr[col] == DBNull.Value ? "" : dr[col].ToString() ?? "";

        private static int IntCol(IDataRecord dr, string col)
        {
            if (dr[col] == DBNull.Value) return 0;
            int v;
            return int.TryParse(Convert.ToString(dr[col]), out v) ? v : 0;
        }

        private static string DateCol(IDataRecord dr, string col)
            => dr[col] == DBNull.Value ? "" : Convert.ToDateTime(dr[col]).ToString("yyyy-MM-dd");

        private static string MoneyCol(IDataRecord dr, string col)
        {
            if (dr[col] == DBNull.Value) return "";
            decimal v;
            return decimal.TryParse(Convert.ToString(dr[col]), out v) ? v.ToString("0.##") : Convert.ToString(dr[col]) ?? "";
        }

        private string LookupName(string table, string idColumn, string nameColumn, int id)
        {
            if (id <= 0) return "";
            return LookupScalar("SELECT TOP 1 " + nameColumn + " FROM " + table + " WHERE " + idColumn + "=@Id;", id) ?? id.ToString();
        }

        private string LookupDesignation(int id)
        {
            if (id <= 0) return "";
            return LookupScalar(@"
SELECT TOP 1 ISNULL(t.TitleName, CAST(d.DesignationID AS NVARCHAR(20)))
FROM tblDesignation d
LEFT JOIN tblTitle t ON t.TitleID = d.TitleID
WHERE d.DesignationID=@Id;", id) ?? id.ToString();
        }

        private string LookupUser(int id)
        {
            if (id <= 0) return "";
            return LookupScalar("SELECT TOP 1 ISNULL(NULLIF(LTRIM(RTRIM(FullName)), ''), Username) FROM tblUser WHERE UserID=@Id;", id) ?? id.ToString();
        }

        private string LookupEmployeeName(int id)
        {
            if (id <= 0) return "";
            return LookupScalar("SELECT TOP 1 LTRIM(RTRIM(ISNULL(FirstName,'') + ' ' + ISNULL(LastName,''))) FROM tblEmployee WHERE EmployeeID=@Id;", id) ?? id.ToString();
        }

        private string LookupScalar(string sql, int id)
        {
            try
            {
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    var value = cmd.ExecuteScalar();
                    var text = value == null || value == DBNull.Value ? "" : value.ToString() ?? "";
                    return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
                }
            }
            catch
            {
                return null;
            }
        }

        private static object CoerceValue(ProfileChangeLine line, string kind)
        {
            var rawJson = line.NewJson ?? "";
            var rawText = line.NewValue ?? "";
            if (kind == "fk")
            {
                int id;
                if (int.TryParse(rawJson, out id) || int.TryParse(rawText, out id))
                    return id <= 0 ? (object)DBNull.Value : id;
                return DBNull.Value;
            }
            if (kind == "date")
            {
                DateTime d;
                if (DateTime.TryParse(rawText, out d) || DateTime.TryParse(rawJson, out d))
                    return d.Date;
                return DBNull.Value;
            }
            if (kind == "dec")
            {
                decimal m;
                if (decimal.TryParse(rawText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out m)
                    || decimal.TryParse(rawText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out m)
                    || decimal.TryParse(rawJson, out m))
                    return m;
                return DBNull.Value;
            }
            if (kind == "int")
            {
                int n;
                if (int.TryParse(rawText, out n) || int.TryParse(rawJson, out n))
                    return n;
                return DBNull.Value;
            }
            if (kind == "photo")
            {
                var path = !string.IsNullOrWhiteSpace(rawJson) ? rawJson.Trim() : "";
                if (string.IsNullOrWhiteSpace(path) && LooksLikePhotoPath(rawText))
                    path = rawText.Trim();
                return string.IsNullOrWhiteSpace(path) ? (object)DBNull.Value : path;
            }
            return string.IsNullOrWhiteSpace(rawText) ? (object)DBNull.Value : rawText.Trim();
        }

        public static string RenderChangeValue(ProfileChangeLine line, bool proposed)
        {
            if (line == null) return "";
            var text = HttpUtility.HtmlEncode(proposed ? line.NewValue : line.OldValue);
            if (!line.IsPhoto) return text;
            var path = proposed ? line.NewJson : line.OldJson;
            if (string.IsNullOrWhiteSpace(path) || !LooksLikePhotoPath(path))
                return text;
            var src = HttpUtility.HtmlAttributeEncode(path.Trim());
            return "<div class=\"profile-change-photo\"><img src=\"" + src + "\" alt=\"Profile picture\" /><div>" + text + "</div></div>";
        }

        private static bool LooksLikePhotoPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var v = value.Trim();
            return v.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
                || v.StartsWith("~/uploads/", StringComparison.OrdinalIgnoreCase);
        }

        private static void TryDeletePhotoFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || HttpContext.Current == null) return;
            try
            {
                var normalized = relativePath.Trim().TrimStart('~').TrimStart('/').Replace('\\', '/');
                var fullPath = HttpContext.Current.Server.MapPath("~/" + normalized);
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
            catch
            {
            }
        }

        private static string Norm(string value) => (value ?? "").Trim();

        private static string NormDate(string value)
        {
            DateTime d;
            return DateTime.TryParse(value, out d) ? d.ToString("yyyy-MM-dd") : Norm(value);
        }

        private static string DisplayDate(string value)
        {
            DateTime d;
            return DateTime.TryParse(value, out d) ? d.ToString("yyyy-MM-dd") : (value ?? "");
        }

        private static string NormMoney(string value)
        {
            decimal d;
            if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out d)
                || decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d))
                return d.ToString("0.##");
            return Norm(value);
        }

        private static string DisplayMoney(string value)
        {
            var n = NormMoney(value);
            return n;
        }

        private static readonly string[] SchemaStatements =
        {
            @"IF OBJECT_ID('tblProfileChangeRequest','U') IS NULL CREATE TABLE tblProfileChangeRequest (
                RequestID INT IDENTITY(1,1) PRIMARY KEY,
                RequestCode NVARCHAR(20) NOT NULL UNIQUE,
                EmployeeID INT NOT NULL,
                SubmittedByUserID INT NOT NULL,
                Status NVARCHAR(30) NOT NULL,
                SubmittedOn DATETIME NOT NULL DEFAULT GETDATE(),
                DecidedOn DATETIME NULL,
                DecidedByUserID INT NULL,
                DecisionComments NVARCHAR(1000) NULL,
                CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
                ModifiedOn DATETIME NULL);",
            @"IF OBJECT_ID('tblProfileChangeLine','U') IS NULL CREATE TABLE tblProfileChangeLine (
                LineID INT IDENTITY(1,1) PRIMARY KEY,
                RequestID INT NOT NULL,
                FieldKey NVARCHAR(80) NOT NULL,
                FieldLabel NVARCHAR(150) NOT NULL,
                Section NVARCHAR(40) NOT NULL,
                OldValue NVARCHAR(MAX) NULL,
                NewValue NVARCHAR(MAX) NULL,
                OldJson NVARCHAR(MAX) NULL,
                NewJson NVARCHAR(MAX) NULL,
                IsSnapshot BIT NOT NULL DEFAULT 0);",
            @"IF OBJECT_ID('tblProfileChangeHistory','U') IS NULL CREATE TABLE tblProfileChangeHistory (
                HistoryID INT IDENTITY(1,1) PRIMARY KEY,
                RequestID INT NOT NULL,
                Status NVARCHAR(30) NOT NULL,
                ActorUserID INT NOT NULL,
                Comments NVARCHAR(1000) NULL,
                ActionOn DATETIME NOT NULL DEFAULT GETDATE());",
            @"IF OBJECT_ID('tblAppInbox','U') IS NULL CREATE TABLE tblAppInbox (
                InboxID INT IDENTITY(1,1) PRIMARY KEY,
                UserID INT NOT NULL,
                Title NVARCHAR(200) NOT NULL,
                Message NVARCHAR(2000) NULL,
                LinkUrl NVARCHAR(300) NULL,
                RelatedType NVARCHAR(40) NULL,
                RelatedID INT NULL,
                IsRead BIT NOT NULL DEFAULT 0,
                CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
                ReadOn DATETIME NULL);"
        };
    }
}
