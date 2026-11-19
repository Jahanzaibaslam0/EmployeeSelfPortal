using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace HRMS.Services
{
    public class WorkspaceRequestRow
    {
        public int RequestID { get; set; }
        public string RequestNumber { get; set; } = "";
        public string Category { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusDisplay { get; set; } = "";
        public string StatusBadgeClass { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Subject { get; set; } = "";
        public string OpenUrl { get; set; } = "";
    }

    public class WorkspaceTaskRow
    {
        public int TaskID { get; set; }
        public string TaskCode { get; set; } = "";
        public string Title { get; set; } = "";
        public string Priority { get; set; } = "";
        public string TaskStatus { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public string AssignedToName { get; set; } = "";
    }

    public class WorkspaceActionRow
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Url { get; set; } = "";
        public string Kind { get; set; } = "";
    }

    public class WorkspaceLeaveRow
    {
        public int LeaveID { get; set; }
        public string EmployeeName { get; set; } = "";
        public string LeaveType { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal Days { get; set; }
        public string Status { get; set; } = "";
        public string StatusDisplay { get; set; } = "";
    }

    public class WorkspaceDocumentRow
    {
        public int DocumentID { get; set; }
        public string EmployeeName { get; set; } = "";
        public string DocumentType { get; set; } = "";
        public string DocumentNumber { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public class WorkspaceAnnouncementRow
    {
        public string Title { get; set; } = "";
        public string Summary { get; set; } = "";
        public string Scope { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class WorkspaceAttendanceRow
    {
        public string EmpCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Status { get; set; } = "";
        public string InTime { get; set; } = "";
        public string OutTime { get; set; } = "";
    }

    public class WorkspaceQuickAction
    {
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public string Icon { get; set; } = "";
        public bool Visible { get; set; }
    }

    public class WorkspaceKpi
    {
        public string Label { get; set; } = "";
        public string Hint { get; set; } = "";
        public string Value { get; set; } = "-";
        public string Icon { get; set; } = "";
    }

    public class MyWorkspaceModel
    {
        public string Greeting { get; set; } = "Hello";
        public string DisplayName { get; set; } = "";
        public string RoleLabel { get; set; } = "Employee";
        public bool IsManagerView { get; set; }
        public bool HasLinkedEmployee { get; set; }
        public int LinkedEmployeeId { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string JobTitle { get; set; } = "";
        public string Email { get; set; } = "";
        public string Mobile { get; set; } = "";
        public string ScopeCaption { get; set; } = "";
        public string AttendanceNote { get; set; } = "";
        public bool CanOpenAttendance { get; set; }
        public int TeamSize { get; set; }
        public decimal LeaveDaysThisYear { get; set; }
        public int PendingLeaveCount { get; set; }

        public WorkspaceKpi ActionsKpi { get; set; } = new WorkspaceKpi();
        public WorkspaceKpi RequestsKpi { get; set; } = new WorkspaceKpi();
        public WorkspaceKpi AttendanceKpi { get; set; } = new WorkspaceKpi();
        public WorkspaceKpi NoticesKpi { get; set; } = new WorkspaceKpi();

        public List<WorkspaceActionRow> Actions { get; set; } = new List<WorkspaceActionRow>();
        public List<WorkspaceRequestRow> Requests { get; set; } = new List<WorkspaceRequestRow>();
        public List<WorkspaceTaskRow> Tasks { get; set; } = new List<WorkspaceTaskRow>();
        public List<WorkspaceLeaveRow> Leaves { get; set; } = new List<WorkspaceLeaveRow>();
        public List<WorkspaceDocumentRow> Documents { get; set; } = new List<WorkspaceDocumentRow>();
        public List<WorkspaceAnnouncementRow> Announcements { get; set; } = new List<WorkspaceAnnouncementRow>();
        public List<WorkspaceAttendanceRow> Attendance { get; set; } = new List<WorkspaceAttendanceRow>();
        public List<WorkspaceQuickAction> QuickActions { get; set; } = new List<WorkspaceQuickAction>();
        public List<AttendanceStatusCount> AttendanceBreakdown { get; set; } = new List<AttendanceStatusCount>();
    }

    /// <summary>
    /// Read-only aggregator for My Workspace. Does not mutate requests, leave, tasks, or other processes.
    /// </summary>
    public class MyWorkspaceService
    {
        private readonly string _conn;
        private readonly AuthService _auth = new AuthService();
        private readonly PermissionService _perms = new PermissionService();
        private readonly EmployeeProfileAccessService _profile = new EmployeeProfileAccessService();
        private readonly EmployeeRequestService _requests = new EmployeeRequestService();
        private readonly AppInboxService _inbox = new AppInboxService();
        private readonly LeaveApprovalService _leave = new LeaveApprovalService();
        private readonly NotificationService _notifications = new NotificationService();
        private readonly MemorandumService _memorandums = new MemorandumService();
        private readonly LmsDocumentService _lms = new LmsDocumentService();
        private readonly AttendanceService _attendance = new AttendanceService();

        public MyWorkspaceService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public MyWorkspaceModel Load()
        {
            var model = new MyWorkspaceModel();
            var linked = _profile.GetLinkedEmployeeId() ?? 0;
            model.LinkedEmployeeId = linked;
            model.HasLinkedEmployee = linked > 0;
            model.DisplayName = ResolveDisplayName(linked);
            model.Greeting = TimeGreeting();
            LoadProfile(model, linked);

            var managedDeptIds = LoadManagedDepartmentIds(linked);
            model.IsManagerView = managedDeptIds.Count > 0;
            model.RoleLabel = model.IsManagerView
                ? "Manager"
                : (_leave.CanActAsHr() ? "HR" : (_auth.IsAdmin ? "Administrator" : "Employee"));
            model.ScopeCaption = model.IsManagerView
                ? "Department view - your records plus your team."
                : "Personal view - only your own records.";

            var teamIds = model.IsManagerView ? LoadTeamEmployeeIds(managedDeptIds) : new HashSet<int>();
            if (linked > 0) teamIds.Add(linked);
            model.TeamSize = model.IsManagerView ? Math.Max(teamIds.Count, 0) : (linked > 0 ? 1 : 0);

            LoadRequests(model, linked);
            LoadTasks(model, linked, teamIds);
            LoadLeaves(model, linked, managedDeptIds);
            LoadDocuments(model, linked, managedDeptIds);
            LoadAnnouncements(model);
            LoadAttendance(model);
            LoadActions(model, linked);
            BuildQuickActions(model);
            BuildKpis(model);
            return model;
        }

        private string ResolveDisplayName(int linked)
        {
            if (linked <= 0)
                return string.IsNullOrWhiteSpace(_auth.CurrentUsername) ? "User" : _auth.CurrentUsername;
            try
            {
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT LTRIM(RTRIM(ISNULL(FirstName,'')+' '+ISNULL(LastName,'')))
FROM tblEmployee WHERE EmployeeID=@Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", linked);
                    conn.Open();
                    var o = cmd.ExecuteScalar();
                    var name = o == null || o == DBNull.Value ? "" : o.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(name)) return name;
                }
            }
            catch { }
            return string.IsNullOrWhiteSpace(_auth.CurrentUsername) ? "User" : _auth.CurrentUsername;
        }

        private static string TimeGreeting()
        {
            var h = DateTime.Now.Hour;
            if (h < 12) return "Good morning";
            if (h < 17) return "Good afternoon";
            return "Good evening";
        }

        private void LoadProfile(MyWorkspaceModel model, int linked)
        {
            if (linked <= 0) return;
            try
            {
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT e.EmployeeCode,
       ISNULL(d.DepartmentName,''),
       ISNULL(j.JobTitle,''),
       ISNULL(e.OfficialEmail,''),
       ISNULL(e.OfficialMobile, ISNULL(e.PersonalMobile,''))
FROM tblEmployee e
LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
LEFT JOIN tblJob j ON j.JobID = e.JobID
WHERE e.EmployeeID=@Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", linked);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) return;
                        model.EmployeeCode = dr.IsDBNull(0) ? "" : dr.GetString(0);
                        model.DepartmentName = dr.IsDBNull(1) ? "" : dr.GetString(1);
                        model.JobTitle = dr.IsDBNull(2) ? "" : dr.GetString(2);
                        model.Email = dr.IsDBNull(3) ? "" : dr.GetString(3);
                        model.Mobile = dr.IsDBNull(4) ? "" : dr.GetString(4);
                    }
                }
            }
            catch
            {
                TryLoadProfileMinimal(model, linked);
            }
        }

        private void TryLoadProfileMinimal(MyWorkspaceModel model, int linked)
        {
            try
            {
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT e.EmployeeCode, ISNULL(d.DepartmentName,'')
FROM tblEmployee e
LEFT JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID
WHERE e.EmployeeID=@Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", linked);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) return;
                        model.EmployeeCode = dr.IsDBNull(0) ? "" : dr.GetString(0);
                        model.DepartmentName = dr.IsDBNull(1) ? "" : dr.GetString(1);
                    }
                }
            }
            catch { }
        }

        private List<int> LoadManagedDepartmentIds(int linked)
        {
            var ids = new List<int>();
            if (linked <= 0) return ids;
            try
            {
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT DepartmentID
FROM tblDepartment
WHERE DepartmentManagerID=@Id AND ISNULL(IsActive,1)=1;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", linked);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                            ids.Add(dr.GetInt32(0));
                    }
                }
            }
            catch { }
            return ids;
        }

        private HashSet<int> LoadTeamEmployeeIds(List<int> departmentIds)
        {
            var set = new HashSet<int>();
            if (departmentIds == null || departmentIds.Count == 0) return set;
            try
            {
                var inList = string.Join(",", departmentIds.Select(id => id.ToString()));
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT EmployeeID FROM tblEmployee
WHERE DepartmentID IN (" + inList + ") AND ISNULL(Status, N'Active') = N'Active';", conn))
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                            set.Add(dr.GetInt32(0));
                    }
                }
            }
            catch { }
            return set;
        }

        private void LoadRequests(MyWorkspaceModel model, int linked)
        {
            try
            {
                var visible = _requests.CanAccessModule()
                    ? _requests.ListVisible()
                    : new List<EmployeeRequestItem>();
                IEnumerable<EmployeeRequestItem> scoped = visible;
                if (model.IsManagerView)
                    scoped = visible.Where(r => r.EmployeeID == linked || r.ManagerEmployeeID == linked);
                else
                    scoped = visible.Where(r => r.EmployeeID == linked);

                model.Requests = scoped.Take(8).Select(r => new WorkspaceRequestRow
                {
                    RequestID = r.RequestID,
                    RequestNumber = r.RequestNumber,
                    Category = r.CategoryDisplay,
                    Status = r.Status,
                    StatusDisplay = r.StatusDisplay,
                    StatusBadgeClass = r.StatusBadgeClass,
                    EmployeeName = r.EmployeeName,
                    Subject = r.Subject,
                    OpenUrl = "/EmployeeRequest.aspx?editId=" + r.RequestID
                }).ToList();

                var open = scoped.Count(r =>
                    r.Status != EmployeeRequestStatuses.Approved
                    && r.Status != EmployeeRequestStatuses.Rejected
                    && r.Status != EmployeeRequestStatuses.Draft);
                model.RequestsKpi = new WorkspaceKpi
                {
                    Label = model.IsManagerView ? "Active HR requests" : "Your HR requests",
                    Hint = model.IsManagerView ? "Open team and personal workflow records" : "Open requests you submitted",
                    Value = open.ToString(),
                    Icon = "doc"
                };
            }
            catch
            {
                model.RequestsKpi = new WorkspaceKpi
                {
                    Label = "Active HR requests",
                    Hint = "Request records are unavailable",
                    Value = "-",
                    Icon = "doc"
                };
            }
        }

        private void LoadTasks(MyWorkspaceModel model, int linked, HashSet<int> teamIds)
        {
            if (linked <= 0) return;
            try
            {
                var sql = model.IsManagerView && teamIds.Count > 0
                    ? @"
SELECT TOP 8 t.TaskID, t.TaskCode, t.Title, t.Priority, t.TaskStatus, t.DueDate,
       LTRIM(RTRIM(ISNULL(ae.FirstName,'')+' '+ISNULL(ae.LastName,'')))
FROM tblTask t
LEFT JOIN tblEmployee ae ON ae.EmployeeID=t.AssignedToEmployeeID
WHERE t.IsActive=1
  AND t.AssignedToEmployeeID IN (" + string.Join(",", teamIds) + @")
  AND ISNULL(t.TaskStatus,'') NOT IN (N'Completed', N'Cancelled', N'Closed', N'Done')
ORDER BY CASE WHEN t.DueDate IS NULL THEN 1 ELSE 0 END, t.DueDate, t.TaskID DESC;"
                    : @"
SELECT TOP 8 t.TaskID, t.TaskCode, t.Title, t.Priority, t.TaskStatus, t.DueDate,
       LTRIM(RTRIM(ISNULL(ae.FirstName,'')+' '+ISNULL(ae.LastName,'')))
FROM tblTask t
LEFT JOIN tblEmployee ae ON ae.EmployeeID=t.AssignedToEmployeeID
WHERE t.IsActive=1
  AND t.AssignedToEmployeeID=@Linked
  AND ISNULL(t.TaskStatus,'') NOT IN (N'Completed', N'Cancelled', N'Closed', N'Done')
ORDER BY CASE WHEN t.DueDate IS NULL THEN 1 ELSE 0 END, t.DueDate, t.TaskID DESC;";

                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (!model.IsManagerView || teamIds.Count == 0)
                        cmd.Parameters.AddWithValue("@Linked", linked);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            model.Tasks.Add(new WorkspaceTaskRow
                            {
                                TaskID = dr.GetInt32(0),
                                TaskCode = dr.IsDBNull(1) ? "" : dr.GetString(1),
                                Title = dr.IsDBNull(2) ? "" : dr.GetString(2),
                                Priority = dr.IsDBNull(3) ? "" : dr.GetString(3),
                                TaskStatus = dr.IsDBNull(4) ? "" : dr.GetString(4),
                                DueDate = dr.IsDBNull(5) ? (DateTime?)null : dr.GetDateTime(5),
                                AssignedToName = dr.IsDBNull(6) ? "" : dr.GetString(6).Trim()
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadLeaves(MyWorkspaceModel model, int linked, List<int> managedDeptIds)
        {
            if (linked <= 0) return;
            try
            {
                var deptFilter = model.IsManagerView && managedDeptIds.Count > 0
                    ? " OR e.DepartmentID IN (" + string.Join(",", managedDeptIds) + ")"
                    : "";
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT TOP 8 l.LeaveID,
       LTRIM(RTRIM(ISNULL(e.FirstName,'')+' '+ISNULL(e.LastName,''))),
       ISNULL(l.LeaveType,''), ISNULL(c.LeaveCategoryName,''),
       l.LeaveFromDate, l.LeaveToDate, ISNULL(l.NumberOfDays,0),
       ISNULL(l.Status, N'Approved')
FROM tblLeaveApplication l
INNER JOIN tblEmployee e ON e.EmployeeID=l.EmployeeID
LEFT JOIN tblLeaveCategory c ON c.LeaveCategoryID=l.LeaveCategoryID
WHERE l.EmployeeID=@Linked" + deptFilter + @"
ORDER BY l.LeaveID DESC;", conn))
                {
                    cmd.Parameters.AddWithValue("@Linked", linked);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var status = dr.IsDBNull(7) ? "Approved" : dr.GetString(7);
                            model.Leaves.Add(new WorkspaceLeaveRow
                            {
                                LeaveID = dr.GetInt32(0),
                                EmployeeName = dr.IsDBNull(1) ? "" : dr.GetString(1).Trim(),
                                LeaveType = dr.IsDBNull(2) ? "" : dr.GetString(2),
                                Category = dr.IsDBNull(3) ? "" : dr.GetString(3),
                                FromDate = dr.IsDBNull(4) ? (DateTime?)null : dr.GetDateTime(4),
                                ToDate = dr.IsDBNull(5) ? (DateTime?)null : dr.GetDateTime(5),
                                Days = dr.IsDBNull(6) ? 0 : Convert.ToDecimal(dr.GetValue(6)),
                                Status = status,
                                StatusDisplay = LeaveApprovalStatuses.DisplayName(status)
                            });
                        }
                    }
                }

                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT ISNULL(SUM(CASE WHEN ISNULL(l.Status,N'') IN (N'PendingDepartmentHead', N'PendingHR', N'Draft') THEN 1 ELSE 0 END),0),
       ISNULL(SUM(CASE WHEN YEAR(l.LeaveFromDate)=YEAR(GETDATE()) AND ISNULL(l.Status,N'')=N'Approved' THEN l.NumberOfDays ELSE 0 END),0)
FROM tblLeaveApplication l
INNER JOIN tblEmployee e ON e.EmployeeID=l.EmployeeID
WHERE l.EmployeeID=@Linked" + deptFilter + ";", conn))
                {
                    cmd.Parameters.AddWithValue("@Linked", linked);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            model.PendingLeaveCount = Convert.ToInt32(dr.GetValue(0));
                            model.LeaveDaysThisYear = Convert.ToDecimal(dr.GetValue(1));
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadDocuments(MyWorkspaceModel model, int linked, List<int> managedDeptIds)
        {
            if (linked <= 0) return;
            try
            {
                var deptFilter = model.IsManagerView && managedDeptIds.Count > 0
                    ? " OR e.DepartmentID IN (" + string.Join(",", managedDeptIds) + ")"
                    : "";
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT TOP 6 d.EmployeeDocumentID,
       LTRIM(RTRIM(ISNULL(e.FirstName,'')+' '+ISNULL(e.LastName,''))),
       ISNULL(dt.DocumentTypeName,''), ISNULL(d.DocumentNumber,''),
       CONVERT(varchar(11), d.ExpiryDate, 106), ISNULL(d.VerificationStatus,'')
FROM tblEmployeeDocument d
INNER JOIN tblEmployee e ON e.EmployeeID=d.EmployeeID
LEFT JOIN tblDocumentType dt ON dt.DocumentTypeID=d.DocumentTypeID
WHERE d.EmployeeID=@Linked" + deptFilter + @"
ORDER BY d.EmployeeDocumentID DESC;", conn))
                {
                    cmd.Parameters.AddWithValue("@Linked", linked);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            model.Documents.Add(new WorkspaceDocumentRow
                            {
                                DocumentID = dr.GetInt32(0),
                                EmployeeName = dr.IsDBNull(1) ? "" : dr.GetString(1).Trim(),
                                DocumentType = dr.IsDBNull(2) ? "" : dr.GetString(2),
                                DocumentNumber = dr.IsDBNull(3) ? "" : dr.GetString(3),
                                ExpiryDate = dr.IsDBNull(4) ? "" : dr.GetString(4),
                                Status = dr.IsDBNull(5) ? "" : dr.GetString(5)
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadAnnouncements(MyWorkspaceModel model)
        {
            try
            {
                var deptId = 0;
                if (model.LinkedEmployeeId > 0)
                {
                    using (var conn = new SqlConnection(_conn))
                    using (var cmd = new SqlCommand("SELECT ISNULL(DepartmentID,0) FROM tblEmployee WHERE EmployeeID=@Id;", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", model.LinkedEmployeeId);
                        conn.Open();
                        var o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value) deptId = Convert.ToInt32(o);
                    }
                }

                foreach (var n in _notifications.GetActiveNotifications())
                {
                    if (n.DepartmentID.HasValue && n.DepartmentID.Value > 0 && deptId > 0 && n.DepartmentID.Value != deptId)
                        continue;
                    if (n.DepartmentID.HasValue && n.DepartmentID.Value > 0 && deptId <= 0)
                        continue;
                    model.Announcements.Add(new WorkspaceAnnouncementRow
                    {
                        Title = n.Name,
                        Summary = Truncate(n.Description, 140),
                        Scope = string.IsNullOrWhiteSpace(n.DepartmentName) ? "Company-wide" : n.DepartmentName,
                        Url = "/Notifications.aspx?id=" + n.NotificationID
                    });
                }

                foreach (var m in _memorandums.GetActiveMemorandums())
                {
                    if (m.DepartmentID.HasValue && m.DepartmentID.Value > 0 && deptId > 0 && m.DepartmentID.Value != deptId)
                        continue;
                    if (m.DepartmentID.HasValue && m.DepartmentID.Value > 0 && deptId <= 0)
                        continue;
                    model.Announcements.Add(new WorkspaceAnnouncementRow
                    {
                        Title = m.Name,
                        Summary = Truncate(m.Description, 140),
                        Scope = string.IsNullOrWhiteSpace(m.DepartmentName) ? "Company-wide" : m.DepartmentName,
                        Url = "/Memorandums.aspx"
                    });
                }

                if (_lms.CanAccessLibrary())
                {
                    foreach (var doc in _lms.ListVisibleDocuments("Policy").Take(4))
                    {
                        model.Announcements.Add(new WorkspaceAnnouncementRow
                        {
                            Title = doc.Title,
                            Summary = string.IsNullOrWhiteSpace(doc.Description) ? doc.CategoryDisplay : Truncate(doc.Description, 140),
                            Scope = string.IsNullOrWhiteSpace(doc.DepartmentName) ? "Policy library" : doc.DepartmentName,
                            Url = "/LmsLibrary.aspx"
                        });
                    }
                }

                model.Announcements = model.Announcements.Take(6).ToList();
            }
            catch { }
        }

        private void LoadAttendance(MyWorkspaceModel model)
        {
            var canSeeAttendance = _auth.IsAdmin || _perms.CanRead("EmployeeAttendance") || _leave.CanAccessModule();
            model.CanOpenAttendance = _auth.IsAdmin || _perms.CanRead("EmployeeAttendance");
            if (!canSeeAttendance)
            {
                model.AttendanceNote = "Attendance is not enabled for this account.";
                return;
            }

            try
            {
                var today = DateTime.Today;
                var filter = new AttendanceFilter
                {
                    StartDate = today,
                    EndDate = today,
                    View = "daily",
                    Page = 1,
                    PageSize = 20
                };
                if (model.IsManagerView && !string.IsNullOrWhiteSpace(model.DepartmentName))
                    filter.Department = model.DepartmentName;
                else if (!model.IsManagerView && !string.IsNullOrWhiteSpace(model.EmployeeCode))
                    filter.EmpCode = model.EmployeeCode;
                else
                {
                    model.AttendanceNote = "Attendance is scoped once your employee department or code is available.";
                    return;
                }

                var result = _attendance.Query(filter);
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    model.AttendanceNote = result.ErrorMessage;
                    return;
                }

                model.AttendanceBreakdown = result.StatusBreakdown ?? new List<AttendanceStatusCount>();
                model.Attendance = (result.DetailRows ?? new List<AttendanceRow>())
                    .Take(8)
                    .Select(r => new WorkspaceAttendanceRow
                    {
                        EmpCode = r.EmpCode,
                        EmployeeName = r.EmployeeName,
                        Status = string.IsNullOrWhiteSpace(r.StatusLabel) ? r.Status : r.StatusLabel,
                        InTime = r.InTime,
                        OutTime = r.OutTime
                    }).ToList();

                if (model.IsManagerView)
                {
                    var present = model.AttendanceBreakdown.Where(s =>
                        (s.Code ?? "").Equals("P", StringComparison.OrdinalIgnoreCase)
                        || (s.Label ?? "").IndexOf("Present", StringComparison.OrdinalIgnoreCase) >= 0)
                        .Sum(s => s.Count);
                    model.AttendanceKpi = new WorkspaceKpi
                    {
                        Label = "Attendance today",
                        Hint = present + " present of " + Math.Max(result.DistinctEmployees, model.TeamSize) + " in department",
                        Value = present.ToString(),
                        Icon = "clock"
                    };
                }
                else
                {
                    var row = model.Attendance.FirstOrDefault();
                    model.AttendanceKpi = new WorkspaceKpi
                    {
                        Label = "Attendance today",
                        Hint = row == null ? "No punch recorded yet" : (row.Status + (string.IsNullOrWhiteSpace(row.InTime) ? "" : " | In " + row.InTime)),
                        Value = row == null ? "-" : (string.IsNullOrWhiteSpace(row.Status) ? "In" : row.Status.Length <= 8 ? row.Status : "In"),
                        Icon = "clock"
                    };
                }
            }
            catch (Exception ex)
            {
                model.AttendanceNote = "Attendance could not be loaded.";
                if (_auth.IsAdmin) model.AttendanceNote += " " + ex.Message;
            }
        }

        private void LoadActions(MyWorkspaceModel model, int linked)
        {
            try
            {
                if (model.IsManagerView && _requests.CanAccessModule())
                {
                    foreach (var r in _requests.ListVisible("manager").Take(6))
                    {
                        model.Actions.Add(new WorkspaceActionRow
                        {
                            Title = r.CategoryDisplay + " - " + r.EmployeeName,
                            Subtitle = r.RequestNumber + " - " + r.StatusDisplay,
                            Url = "/EmployeeRequest.aspx?editId=" + r.RequestID,
                            Kind = "approval"
                        });
                    }
                }

                if (model.IsManagerView && _leave.CanAccessModule())
                {
                    foreach (var l in model.Leaves.Where(x =>
                        x.Status == LeaveApprovalStatuses.PendingDepartmentHead).Take(4))
                    {
                        model.Actions.Add(new WorkspaceActionRow
                        {
                            Title = "Leave - " + l.EmployeeName,
                            Subtitle = l.LeaveType + " - " + l.StatusDisplay,
                            Url = "/LeaveMaster.aspx?editId=" + l.LeaveID,
                            Kind = "leave"
                        });
                    }
                }

                if (_auth.CurrentUserId.HasValue)
                {
                    foreach (var item in _inbox.List(_auth.CurrentUserId.Value, 8))
                    {
                        if (item.IsRead) continue;
                        model.Actions.Add(new WorkspaceActionRow
                        {
                            Title = item.Title,
                            Subtitle = item.CreatedOn.ToString("dd-MMM-yyyy HH:mm"),
                            Url = string.IsNullOrWhiteSpace(item.LinkUrl)
                                ? "/Inbox.aspx?open=" + item.InboxID
                                : "/Inbox.aspx?open=" + item.InboxID,
                            Kind = "inbox"
                        });
                    }
                }

                foreach (var t in model.Tasks.Take(4))
                {
                    model.Actions.Add(new WorkspaceActionRow
                    {
                        Title = t.Title,
                        Subtitle = t.TaskCode + " - " + t.TaskStatus + (t.DueDate.HasValue ? " - due " + t.DueDate.Value.ToString("dd-MMM") : ""),
                        Url = "/TaskMaster.aspx?editId=" + t.TaskID,
                        Kind = "task"
                    });
                }

                model.Actions = model.Actions.Take(8).ToList();
            }
            catch { }
        }

        private void BuildQuickActions(MyWorkspaceModel model)
        {
            var docsOk = _profile.CanAccessMyDocuments();
            model.QuickActions = new List<WorkspaceQuickAction>
            {
                new WorkspaceQuickAction
                {
                    Title = "Submit HR request",
                    Url = "/EmployeeRequest.aspx",
                    Icon = "plus",
                    Visible = _requests.CanAccessModule()
                },
                new WorkspaceQuickAction
                {
                    Title = "My profile",
                    Url = "/UserProfile.aspx",
                    Icon = "user",
                    Visible = true
                },
                new WorkspaceQuickAction
                {
                    Title = "Attendance & leave",
                    Url = _perms.CanRead("EmployeeAttendance") || _auth.IsAdmin
                        ? "/EmployeeAttendance.aspx"
                        : "/LeaveMaster.aspx",
                    Icon = "clock",
                    Visible = _leave.CanAccessModule() || _perms.CanRead("EmployeeAttendance") || _auth.IsAdmin
                },
                new WorkspaceQuickAction
                {
                    Title = "Policies & notices",
                    Url = "/Notifications.aspx",
                    Icon = "book",
                    Visible = true
                },
                new WorkspaceQuickAction
                {
                    Title = "My documents",
                    Url = "/MyDocuments.aspx",
                    Icon = "folder",
                    Visible = docsOk
                },
                new WorkspaceQuickAction
                {
                    Title = "Knowledge library",
                    Url = "/LmsLibrary.aspx",
                    Icon = "book",
                    Visible = _lms.CanAccessLibrary()
                }
            };
        }

        private void BuildKpis(MyWorkspaceModel model)
        {
            model.ActionsKpi = new WorkspaceKpi
            {
                Label = "Actions requiring attention",
                Hint = model.IsManagerView ? "Approvals, tasks, and your inbox" : "Items waiting for your response",
                Value = model.Actions.Count.ToString(),
                Icon = "check"
            };

            if (string.IsNullOrEmpty(model.RequestsKpi.Value))
            {
                model.RequestsKpi = new WorkspaceKpi
                {
                    Label = "Active HR requests",
                    Hint = "Open workflow records",
                    Value = "0",
                    Icon = "doc"
                };
            }

            if (string.IsNullOrEmpty(model.AttendanceKpi.Value) || model.AttendanceKpi.Value == "-")
            {
                if (string.IsNullOrEmpty(model.AttendanceKpi.Label))
                {
                    model.AttendanceKpi = new WorkspaceKpi
                    {
                        Label = "Attendance today",
                        Hint = string.IsNullOrWhiteSpace(model.AttendanceNote) ? "Latest available punch data" : Truncate(model.AttendanceNote, 80),
                        Value = "-",
                        Icon = "clock"
                    };
                }
            }

            model.NoticesKpi = new WorkspaceKpi
            {
                Label = "Published notices",
                Hint = "Active announcements and policies",
                Value = model.Announcements.Count.ToString(),
                Icon = "megaphone"
            };
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var t = value.Trim();
            if (t.Length <= max) return t;
            return t.Substring(0, max - 1) + "...";
        }
    }
}
