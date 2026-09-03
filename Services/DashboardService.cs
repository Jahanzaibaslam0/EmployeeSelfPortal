using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace HRMS.Services
{
public class DashboardFilters
{
    public int DivisionId { get; set; }
    public int DepartmentId { get; set; }
    public int RegionId { get; set; }
    public int LocationId { get; set; }
    public int EmploymentTypeId { get; set; }
    public int WorkerCategoryId { get; set; }
    public DateTime DateFrom { get; set; } = DateTime.Today.AddMonths(-11).AddDays(1 - DateTime.Today.Day);
    public DateTime DateTo { get; set; } = DateTime.Today;
}

public class DashboardKpis
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int InactiveEmployees { get; set; }
    public int NewHiresThisMonth { get; set; }
    public int SeparationsThisMonth { get; set; }
    public int EmployeesOnLeaveToday { get; set; }
    public int PendingLeaveRequests { get; set; }
    public int TotalLeaveApplications { get; set; }
    public decimal? AttendanceRate { get; set; }
    public decimal? AbsenteeismRate { get; set; }
    public bool AttendanceAvailable { get; set; }
    public int OpenVacancies { get; set; }
    public int ProbationEmployees { get; set; }
    public int ContractExpiryAlerts { get; set; }
    public int UpcomingBirthdays { get; set; }
    public int UpcomingAnniversaries { get; set; }
    public int PendingExpenses { get; set; }
    public int DocumentExpiryAlerts { get; set; }
    public int RecruitmentInPipeline { get; set; }
}

public class DashboardSlice
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public decimal Value { get; set; }
}

public class DashboardTrendPoint
{
    public string Label { get; set; } = "";
    public int Hires { get; set; }
    public int Separations { get; set; }
    public int LeaveApplications { get; set; }
    public int LeaveDays { get; set; }
    public int Headcount { get; set; }
}

public class DashboardAlertItem
{
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Severity { get; set; } = "info";
    public string Link { get; set; }
}

public class DashboardSnapshot
{
    public DashboardKpis Kpis { get; set; } = new();
    public List<DashboardSlice> ByDivision { get; set; } = new();
    public List<DashboardSlice> ByDepartment { get; set; } = new();
    public List<DashboardSlice> ByRegion { get; set; } = new();
    public List<DashboardSlice> ByLocation { get; set; } = new();
    public List<DashboardSlice> ByGender { get; set; } = new();
    public List<DashboardSlice> ByEmploymentType { get; set; } = new();
    public List<DashboardSlice> ByAgeGroup { get; set; } = new();
    public List<DashboardSlice> ByLeaveCategory { get; set; } = new();
    public List<DashboardSlice> ByLeaveType { get; set; } = new();
    public List<DashboardSlice> ByRecruitmentStatus { get; set; } = new();
    public List<DashboardSlice> ByVacancyStatus { get; set; } = new();
    public List<DashboardTrendPoint> MonthlyTrends { get; set; } = new();
    public List<DashboardAlertItem> Alerts { get; set; } = new();
    public List<DashboardSlice> EmployeesOnLeaveList { get; set; } = new();
    public string AttendancePortalUrl { get; set; } = "";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

public class DashboardService
{
    private readonly string _conn;

    public DashboardService()
    {
        _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
    }

    public DashboardSnapshot Load(DashboardFilters filters)
    {
        var snapshot = new DashboardSnapshot
        {
            Kpis = LoadKpis(filters),
            AttendancePortalUrl = GetAttendancePortalUrl()
        };

        snapshot.ByDivision = LoadEmployeeDistribution(filters, "div.DivisionName", "LEFT JOIN tblDivision div ON div.DivisionID = e.DivisionID");
        snapshot.ByDepartment = LoadEmployeeDistribution(filters, "d.DepartmentName", "INNER JOIN tblDepartment d ON d.DepartmentID = e.DepartmentID");
        snapshot.ByRegion = LoadEmployeeDistribution(filters, "r.RegionName", "LEFT JOIN tblRegion r ON r.RegionID = e.RegionID");
        snapshot.ByLocation = LoadEmployeeDistribution(filters, "l.LocationName", "LEFT JOIN tblLocation l ON l.LocationID = e.LocationID");
        snapshot.ByGender = LoadEmployeeDistribution(filters, "ISNULL(NULLIF(g.GenderName, ''), ISNULL(e.Gender, 'Unknown'))", "LEFT JOIN tblGender g ON g.GenderID = e.GenderID");
        snapshot.ByEmploymentType = LoadEmployeeDistribution(filters, "ISNULL(et.EmploymentTypeName, 'Unassigned')", "LEFT JOIN tblEmploymentType et ON et.EmploymentTypeID = e.EmploymentTypeID");
        snapshot.ByAgeGroup = LoadAgeGroups(filters);
        snapshot.ByLeaveCategory = LoadLeaveDistribution(filters, "c.LeaveCategoryName", "LEFT JOIN tblLeaveCategory c ON c.LeaveCategoryID = l.LeaveCategoryID");
        snapshot.ByLeaveType = LoadLeaveDistribution(filters, "l.LeaveType", "");
        snapshot.ByRecruitmentStatus = LoadRecruitmentStatus(filters);
        snapshot.ByVacancyStatus = LoadVacancyStatus(filters);
        snapshot.MonthlyTrends = LoadMonthlyTrends(filters);
        snapshot.EmployeesOnLeaveList = LoadEmployeesOnLeaveToday(filters);
        snapshot.Alerts = BuildAlerts(snapshot, filters);
        snapshot.GeneratedAt = DateTime.Now;
        return snapshot;
    }

    public List<DashboardFilterOption> LoadFilterLookups(string table, string idCol, string nameCol)
    {
        var items = new List<DashboardFilterOption>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT {idCol}, {nameCol}
            FROM {table}
            WHERE IsActive = 1
            ORDER BY {nameCol};", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            items.Add(new DashboardFilterOption { Id = dr.GetInt32(0), Name = dr.GetString(1) });
        return items;
    }

    private DashboardKpis LoadKpis(DashboardFilters filters)
    {
        var kpis = new DashboardKpis();
        using var conn = new SqlConnection(_conn);
        conn.Open();

        var empFilter = BuildEmployeeWhere(filters, "e");
        using (var cmd = new SqlCommand($@"
            SELECT
                COUNT(*) AS TotalEmployees,
                SUM(CASE WHEN e.Status = 'Active' THEN 1 ELSE 0 END) AS ActiveEmployees,
                SUM(CASE WHEN e.Status <> 'Active' THEN 1 ELSE 0 END) AS InactiveEmployees,
                SUM(CASE WHEN e.Status = 'Active'
                          AND e.DateOfJoining >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
                          AND e.DateOfJoining < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
                     THEN 1 ELSE 0 END) AS NewHiresThisMonth,
                SUM(CASE WHEN e.Status <> 'Active'
                          AND e.ModifiedOn >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
                     THEN 1 ELSE 0 END) AS SeparationsThisMonth,
                SUM(CASE WHEN e.Status = 'Active'
                          AND e.ProbationEndDate IS NOT NULL
                          AND e.ConfirmationDate IS NULL
                          AND e.ProbationEndDate >= CAST(GETDATE() AS DATE)
                     THEN 1 ELSE 0 END) AS ProbationEmployees
            FROM tblEmployee e
            WHERE 1=1 {empFilter.Sql};", conn))
        {
            AddEmployeeParams(cmd, filters);
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                kpis.TotalEmployees = dr.GetInt32(0);
                kpis.ActiveEmployees = dr.IsDBNull(1) ? 0 : Convert.ToInt32(dr[1]);
                kpis.InactiveEmployees = dr.IsDBNull(2) ? 0 : Convert.ToInt32(dr[2]);
                kpis.NewHiresThisMonth = dr.IsDBNull(3) ? 0 : Convert.ToInt32(dr[3]);
                kpis.SeparationsThisMonth = dr.IsDBNull(4) ? 0 : Convert.ToInt32(dr[4]);
                kpis.ProbationEmployees = dr.IsDBNull(5) ? 0 : Convert.ToInt32(dr[5]);
            }
        }

        var leaveFilter = BuildEmployeeWhere(filters, "emp");
        using (var cmd = new SqlCommand($@"
            SELECT
                COUNT(DISTINCT CASE WHEN CAST(GETDATE() AS DATE) BETWEEN l.LeaveFromDate AND l.LeaveToDate THEN l.EmployeeID END),
                COUNT(*),
                SUM(CASE WHEN l.ApplyingDate BETWEEN @DateFrom AND @DateTo THEN 1 ELSE 0 END)
            FROM tblLeaveApplication l
            INNER JOIN tblEmployee emp ON emp.EmployeeID = l.EmployeeID
            WHERE 1=1 {leaveFilter.Sql};", conn))
        {
            cmd.Parameters.AddWithValue("@DateFrom", filters.DateFrom.Date);
            cmd.Parameters.AddWithValue("@DateTo", filters.DateTo.Date);
            AddEmployeeParams(cmd, filters, "emp");
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                kpis.EmployeesOnLeaveToday = dr.IsDBNull(0) ? 0 : Convert.ToInt32(dr[0]);
                kpis.TotalLeaveApplications = dr.IsDBNull(1) ? 0 : Convert.ToInt32(dr[1]);
                kpis.PendingLeaveRequests = dr.IsDBNull(2) ? 0 : Convert.ToInt32(dr[2]);
            }
        }

        using (var cmd = new SqlCommand($@"
            SELECT COUNT(*)
            FROM tblPosition p
            WHERE p.IsActive = 1
              AND NOT EXISTS (
                  SELECT 1 FROM tblPositionWorkerAssignment a
                  WHERE a.PositionID = p.PositionID
                    AND (a.AssignmentEndDate IS NULL OR a.AssignmentEndDate >= CAST(GETDATE() AS DATE))
              );", conn))
        {
            kpis.OpenVacancies = (int)cmd.ExecuteScalar();
        }

        using (var cmd = new SqlCommand($@"
            SELECT COUNT(*)
            FROM tblPosition p
            WHERE p.IsActive = 1
              AND p.PositionEndDate IS NOT NULL
              AND p.PositionEndDate BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 30, CAST(GETDATE() AS DATE));", conn))
        {
            kpis.ContractExpiryAlerts = (int)cmd.ExecuteScalar();
        }

        using (var cmd = new SqlCommand($@"
            SELECT COUNT(*)
            FROM tblEmployee e
            WHERE e.Status = 'Active'
              AND e.DateOfBirth IS NOT NULL
              AND (
                  (DATEADD(YEAR, DATEDIFF(YEAR, e.DateOfBirth, GETDATE()), e.DateOfBirth) BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 30, CAST(GETDATE() AS DATE)))
                  OR (DATEADD(YEAR, DATEDIFF(YEAR, e.DateOfBirth, GETDATE()) + 1, e.DateOfBirth) BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 30, CAST(GETDATE() AS DATE)))
              )
              {empFilter.Sql};", conn))
        {
            AddEmployeeParams(cmd, filters);
            kpis.UpcomingBirthdays = (int)cmd.ExecuteScalar();
        }

        using (var cmd = new SqlCommand($@"
            SELECT COUNT(*)
            FROM tblEmployee e
            WHERE e.Status = 'Active'
              AND e.DateOfJoining IS NOT NULL
              AND (
                  (DATEADD(YEAR, DATEDIFF(YEAR, e.DateOfJoining, GETDATE()), e.DateOfJoining) BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 30, CAST(GETDATE() AS DATE)))
                  OR (DATEADD(YEAR, DATEDIFF(YEAR, e.DateOfJoining, GETDATE()) + 1, e.DateOfJoining) BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 30, CAST(GETDATE() AS DATE)))
              )
              {empFilter.Sql};", conn))
        {
            AddEmployeeParams(cmd, filters);
            kpis.UpcomingAnniversaries = (int)cmd.ExecuteScalar();
        }

        using (var cmd = new SqlCommand(@"
            SELECT COUNT(*)
            FROM tblExpense
            WHERE WorkflowStatus IN ('Submitted', 'In Review', 'Pending');", conn))
        {
            kpis.PendingExpenses = (int)cmd.ExecuteScalar();
        }

        using (var cmd = new SqlCommand($@"
            SELECT COUNT(*)
            FROM tblEmployeeDocument ed
            INNER JOIN tblEmployee e ON e.EmployeeID = ed.EmployeeID
            WHERE ed.ExpiryDate IS NOT NULL
              AND ed.ExpiryDate BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 30, CAST(GETDATE() AS DATE))
              {empFilter.Sql.Replace("e.", "e.")};", conn))
        {
            AddEmployeeParams(cmd, filters);
            kpis.DocumentExpiryAlerts = (int)cmd.ExecuteScalar();
        }

        using (var cmd = new SqlCommand(@"
            SELECT COUNT(*)
            FROM tblRecruitment
            WHERE ISNULL(OnboardingStatus, '') <> 'Completed';", conn))
        {
            kpis.RecruitmentInPipeline = (int)cmd.ExecuteScalar();
        }

        kpis.AttendanceAvailable = false;
        if (kpis.ActiveEmployees > 0)
        {
            var presentProxy = Math.Max(0, kpis.ActiveEmployees - kpis.EmployeesOnLeaveToday);
            kpis.AttendanceRate = Math.Round(presentProxy * 100m / kpis.ActiveEmployees, 1);
            kpis.AbsenteeismRate = Math.Round(kpis.EmployeesOnLeaveToday * 100m / kpis.ActiveEmployees, 1);
        }

        return kpis;
    }

    private List<DashboardSlice> LoadEmployeeDistribution(DashboardFilters filters, string labelExpr, string joins)
    {
        var list = new List<DashboardSlice>();
        var empFilter = BuildEmployeeWhere(filters, "e");
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT ISNULL(NULLIF(LTRIM(RTRIM({labelExpr})), ''), 'Unassigned') AS Label, COUNT(*) AS Cnt
            FROM tblEmployee e
            {joins}
            WHERE e.Status = 'Active' {empFilter.Sql}
            GROUP BY ISNULL(NULLIF(LTRIM(RTRIM({labelExpr})), ''), 'Unassigned')
            ORDER BY Cnt DESC, Label;", conn);
        AddEmployeeParams(cmd, filters);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            list.Add(new DashboardSlice { Label = dr.GetString(0), Count = dr.GetInt32(1) });
        return list;
    }

    private List<DashboardSlice> LoadAgeGroups(DashboardFilters filters)
    {
        var list = new List<DashboardSlice>();
        var empFilter = BuildEmployeeWhere(filters, "e");
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT
                CASE
                    WHEN e.DateOfBirth IS NULL THEN 'Unknown'
                    WHEN DATEDIFF(YEAR, e.DateOfBirth, GETDATE()) < 25 THEN 'Under 25'
                    WHEN DATEDIFF(YEAR, e.DateOfBirth, GETDATE()) BETWEEN 25 AND 34 THEN '25–34'
                    WHEN DATEDIFF(YEAR, e.DateOfBirth, GETDATE()) BETWEEN 35 AND 44 THEN '35–44'
                    WHEN DATEDIFF(YEAR, e.DateOfBirth, GETDATE()) BETWEEN 45 AND 54 THEN '45–54'
                    ELSE '55+'
                END AS AgeGroup,
                COUNT(*) AS Cnt
            FROM tblEmployee e
            WHERE e.Status = 'Active' {empFilter.Sql}
            GROUP BY CASE
                    WHEN e.DateOfBirth IS NULL THEN 'Unknown'
                    WHEN DATEDIFF(YEAR, e.DateOfBirth, GETDATE()) < 25 THEN 'Under 25'
                    WHEN DATEDIFF(YEAR, e.DateOfBirth, GETDATE()) BETWEEN 25 AND 34 THEN '25–34'
                    WHEN DATEDIFF(YEAR, e.DateOfBirth, GETDATE()) BETWEEN 35 AND 44 THEN '35–44'
                    WHEN DATEDIFF(YEAR, e.DateOfBirth, GETDATE()) BETWEEN 45 AND 54 THEN '45–54'
                    ELSE '55+'
                END
            ORDER BY Cnt DESC;", conn);
        AddEmployeeParams(cmd, filters);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            list.Add(new DashboardSlice { Label = dr.GetString(0), Count = dr.GetInt32(1) });
        return list;
    }

    private List<DashboardSlice> LoadLeaveDistribution(DashboardFilters filters, string labelExpr, string extraJoin)
    {
        var list = new List<DashboardSlice>();
        var empFilter = BuildEmployeeWhere(filters, "emp");
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT ISNULL(NULLIF(LTRIM(RTRIM({labelExpr})), ''), 'Unassigned') AS Label,
                   COUNT(*) AS Cnt,
                   ISNULL(SUM(l.NumberOfDays), 0) AS Days
            FROM tblLeaveApplication l
            INNER JOIN tblEmployee emp ON emp.EmployeeID = l.EmployeeID
            {extraJoin}
            WHERE l.LeaveFromDate BETWEEN @DateFrom AND @DateTo {empFilter.Sql}
            GROUP BY ISNULL(NULLIF(LTRIM(RTRIM({labelExpr})), ''), 'Unassigned')
            ORDER BY Days DESC, Cnt DESC;", conn);
        cmd.Parameters.AddWithValue("@DateFrom", filters.DateFrom.Date);
        cmd.Parameters.AddWithValue("@DateTo", filters.DateTo.Date);
        AddEmployeeParams(cmd, filters, "emp");
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            list.Add(new DashboardSlice { Label = dr.GetString(0), Count = dr.GetInt32(1), Value = dr.GetInt32(2) });
        return list;
    }

    private List<DashboardSlice> LoadRecruitmentStatus(DashboardFilters filters)
    {
        var list = new List<DashboardSlice>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT ISNULL(NULLIF(LTRIM(RTRIM(InterviewStatus)), ''), 'Not Set') AS Label, COUNT(*) AS Cnt
            FROM tblRecruitment
            WHERE ApplicationDate BETWEEN @DateFrom AND @DateTo OR ApplicationDate IS NULL
            GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(InterviewStatus)), ''), 'Not Set')
            ORDER BY Cnt DESC;", conn);
        cmd.Parameters.AddWithValue("@DateFrom", filters.DateFrom.Date);
        cmd.Parameters.AddWithValue("@DateTo", filters.DateTo.Date);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            list.Add(new DashboardSlice { Label = dr.GetString(0), Count = dr.GetInt32(1) });
        return list;
    }

    private List<DashboardSlice> LoadVacancyStatus(DashboardFilters filters)
    {
        var list = new List<DashboardSlice>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT x.Label, COUNT(*) AS Cnt
            FROM (
                SELECT
                    CASE
                        WHEN NOT EXISTS (
                            SELECT 1 FROM tblPositionWorkerAssignment a
                            WHERE a.PositionID = p.PositionID
                              AND (a.AssignmentEndDate IS NULL OR a.AssignmentEndDate >= CAST(GETDATE() AS DATE))
                        ) THEN 'Vacant'
                        ELSE 'Filled'
                    END AS Label
                FROM tblPosition p
                WHERE p.IsActive = 1
            ) x
            GROUP BY x.Label;", conn);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            list.Add(new DashboardSlice { Label = dr.GetString(0), Count = dr.GetInt32(1) });
        return list;
    }

    private List<DashboardTrendPoint> LoadMonthlyTrends(DashboardFilters filters)
    {
        var list = new List<DashboardTrendPoint>();
        using var conn = new SqlConnection(_conn);
        conn.Open();

        for (var i = 11; i >= 0; i--)
        {
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var label = monthStart.ToString("MMM yyyy");

            int hires = 0, separations = 0, leaveApps = 0, leaveDays = 0, headcount = 0;
            var empFilter = BuildEmployeeWhere(filters, "e");

            using (var cmd = new SqlCommand($@"
                SELECT
                    SUM(CASE WHEN e.DateOfJoining BETWEEN @Start AND @End THEN 1 ELSE 0 END),
                    SUM(CASE WHEN e.Status <> 'Active' AND e.ModifiedOn BETWEEN @Start AND @End THEN 1 ELSE 0 END),
                    SUM(CASE WHEN e.Status = 'Active' AND (e.DateOfJoining IS NULL OR e.DateOfJoining <= @End) THEN 1 ELSE 0 END)
                FROM tblEmployee e
                WHERE 1=1 {empFilter.Sql};", conn))
            {
                cmd.Parameters.AddWithValue("@Start", monthStart);
                cmd.Parameters.AddWithValue("@End", monthEnd);
                AddEmployeeParams(cmd, filters);
                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    hires = dr.IsDBNull(0) ? 0 : Convert.ToInt32(dr[0]);
                    separations = dr.IsDBNull(1) ? 0 : Convert.ToInt32(dr[1]);
                    headcount = dr.IsDBNull(2) ? 0 : Convert.ToInt32(dr[2]);
                }
            }

            var leaveFilter = BuildEmployeeWhere(filters, "emp");
            using (var cmd = new SqlCommand($@"
                SELECT COUNT(*), ISNULL(SUM(l.NumberOfDays), 0)
                FROM tblLeaveApplication l
                INNER JOIN tblEmployee emp ON emp.EmployeeID = l.EmployeeID
                WHERE l.LeaveFromDate BETWEEN @Start AND @End {leaveFilter.Sql};", conn))
            {
                cmd.Parameters.AddWithValue("@Start", monthStart);
                cmd.Parameters.AddWithValue("@End", monthEnd);
                AddEmployeeParams(cmd, filters, "emp");
                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    leaveApps = dr.GetInt32(0);
                    leaveDays = Convert.ToInt32(dr[1]);
                }
            }

            list.Add(new DashboardTrendPoint
            {
                Label = label,
                Hires = hires,
                Separations = separations,
                LeaveApplications = leaveApps,
                LeaveDays = leaveDays,
                Headcount = headcount
            });
        }

        return list;
    }

    private List<DashboardSlice> LoadEmployeesOnLeaveToday(DashboardFilters filters)
    {
        var list = new List<DashboardSlice>();
        var empFilter = BuildEmployeeWhere(filters, "emp");
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand($@"
            SELECT emp.EmployeeCode + ' – ' + emp.FirstName + ' ' + emp.LastName AS Label,
                   l.NumberOfDays AS Cnt
            FROM tblLeaveApplication l
            INNER JOIN tblEmployee emp ON emp.EmployeeID = l.EmployeeID
            WHERE CAST(GETDATE() AS DATE) BETWEEN l.LeaveFromDate AND l.LeaveToDate
              {empFilter.Sql}
            ORDER BY emp.FirstName, emp.LastName;", conn);
        AddEmployeeParams(cmd, filters, "emp");
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
            list.Add(new DashboardSlice { Label = dr.GetString(0), Count = dr.GetInt32(1) });
        return list;
    }

    private List<DashboardAlertItem> BuildAlerts(DashboardSnapshot snapshot, DashboardFilters filters)
    {
        var alerts = new List<DashboardAlertItem>();
        var k = snapshot.Kpis;

        if (k.PendingLeaveRequests > 0)
            alerts.Add(new DashboardAlertItem { Category = "Leave", Title = "Leave applications in period", Detail = $"{k.PendingLeaveRequests} application(s) submitted in selected date range.", Severity = "warning", Link = "/LeaveMaster" });

        if (k.PendingExpenses > 0)
            alerts.Add(new DashboardAlertItem { Category = "Approvals", Title = "Pending expense approvals", Detail = $"{k.PendingExpenses} expense claim(s) awaiting review.", Severity = "warning", Link = "/ExpenseMaster" });

        if (k.ContractExpiryAlerts > 0)
            alerts.Add(new DashboardAlertItem { Category = "Contract", Title = "Position contract expiring", Detail = $"{k.ContractExpiryAlerts} position(s) ending within 30 days.", Severity = "danger", Link = "/PositionMaster" });

        if (k.ProbationEmployees > 0)
            alerts.Add(new DashboardAlertItem { Category = "Probation", Title = "Employees on probation", Detail = $"{k.ProbationEmployees} active employee(s) currently in probation.", Severity = "info", Link = "/EmployeeMaster" });

        if (k.DocumentExpiryAlerts > 0)
            alerts.Add(new DashboardAlertItem { Category = "Documents", Title = "Document expiry alerts", Detail = $"{k.DocumentExpiryAlerts} employee document(s) expiring within 30 days.", Severity = "danger", Link = "/EmployeeMaster" });

        if (k.UpcomingBirthdays > 0)
            alerts.Add(new DashboardAlertItem { Category = "Birthdays", Title = "Upcoming birthdays", Detail = $"{k.UpcomingBirthdays} birthday(s) in the next 30 days.", Severity = "info" });

        if (k.UpcomingAnniversaries > 0)
            alerts.Add(new DashboardAlertItem { Category = "Anniversaries", Title = "Work anniversaries", Detail = $"{k.UpcomingAnniversaries} work anniversary(ies) in the next 30 days.", Severity = "info" });

        if (k.OpenVacancies > 0)
            alerts.Add(new DashboardAlertItem { Category = "Vacancies", Title = "Open vacancies", Detail = $"{k.OpenVacancies} active position(s) without assignment.", Severity = "warning", Link = "/PositionMaster" });

        if (k.RecruitmentInPipeline > 0)
            alerts.Add(new DashboardAlertItem { Category = "Recruitment", Title = "Recruitment pipeline", Detail = $"{k.RecruitmentInPipeline} candidate(s) with open onboarding.", Severity = "info", Link = "/RecruitmentMaster" });

        if (!string.IsNullOrEmpty(snapshot.AttendancePortalUrl))
            alerts.Add(new DashboardAlertItem { Category = "Attendance", Title = "Attendance data external", Detail = "Daily attendance, late arrivals, and absenteeism details are available in the Attendance Portal.", Severity = "info", Link = snapshot.AttendancePortalUrl });

        LoadDetailedAlerts(alerts, filters);
        return alerts.OrderByDescending(a => a.Severity == "danger").ThenByDescending(a => a.Severity == "warning").ToList();
    }

    private void LoadDetailedAlerts(List<DashboardAlertItem> alerts, DashboardFilters filters)
    {
        var empFilter = BuildEmployeeWhere(filters, "e");
        using var conn = new SqlConnection(_conn);
        conn.Open();

        using (var cmd = new SqlCommand($@"
            SELECT TOP 5 e.FirstName + ' ' + e.LastName, e.ProbationEndDate
            FROM tblEmployee e
            WHERE e.Status = 'Active'
              AND e.ProbationEndDate IS NOT NULL
              AND e.ConfirmationDate IS NULL
              AND e.ProbationEndDate BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 30, CAST(GETDATE() AS DATE))
              {empFilter.Sql}
            ORDER BY e.ProbationEndDate;", conn))
        {
            AddEmployeeParams(cmd, filters);
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                var date = Convert.ToDateTime(dr[1]).ToString("dd MMM yyyy");
                alerts.Add(new DashboardAlertItem
                {
                    Category = "Probation",
                    Title = "Probation completion due",
                    Detail = $"{dr.GetString(0)} — probation ends {date}.",
                    Severity = "warning",
                    Link = "/EmployeeMaster"
                });
            }
        }
    }

    private string GetAttendancePortalUrl()
    {
        try
        {
            using var conn = new SqlConnection(_conn);
            using var cmd = new SqlCommand(@"
                SELECT TOP 1 SoftwareUrl
                FROM tblSoftwareLink
                WHERE IsActive = 1 AND SoftwareName LIKE '%Attendance%'
                ORDER BY SortOrder;", conn);
            conn.Open();
            return cmd.ExecuteScalar()?.ToString() ?? "";
        }
        catch
        {
            return "";
        }
    }

    private sealed class FilterSql
    {
        public FilterSql(string sql) { Sql = sql; }
        public string Sql { get; }
    }

    private static FilterSql BuildEmployeeWhere(DashboardFilters filters, string alias)
    {
        var sb = new StringBuilder();
        if (filters.DivisionId > 0) sb.Append($" AND {alias}.DivisionID = @DivisionId");
        if (filters.DepartmentId > 0) sb.Append($" AND {alias}.DepartmentID = @DepartmentId");
        if (filters.RegionId > 0) sb.Append($" AND {alias}.RegionID = @RegionId");
        if (filters.LocationId > 0) sb.Append($" AND {alias}.LocationID = @LocationId");
        if (filters.EmploymentTypeId > 0) sb.Append($" AND {alias}.EmploymentTypeID = @EmploymentTypeId");
        if (filters.WorkerCategoryId > 0) sb.Append($" AND {alias}.WorkerCategoryID = @WorkerCategoryId");
        return new FilterSql(sb.ToString());
    }

    private static void AddEmployeeParams(SqlCommand cmd, DashboardFilters filters, string alias = "e")
    {
        if (filters.DivisionId > 0) cmd.Parameters.AddWithValue("@DivisionId", filters.DivisionId);
        if (filters.DepartmentId > 0) cmd.Parameters.AddWithValue("@DepartmentId", filters.DepartmentId);
        if (filters.RegionId > 0) cmd.Parameters.AddWithValue("@RegionId", filters.RegionId);
        if (filters.LocationId > 0) cmd.Parameters.AddWithValue("@LocationId", filters.LocationId);
        if (filters.EmploymentTypeId > 0) cmd.Parameters.AddWithValue("@EmploymentTypeId", filters.EmploymentTypeId);
        if (filters.WorkerCategoryId > 0) cmd.Parameters.AddWithValue("@WorkerCategoryId", filters.WorkerCategoryId);
    }
}

public class DashboardFilterOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
}
