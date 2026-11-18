using ClosedXML.Excel;
using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace HRMS.Pages;

public class DashboardModel : PageModel
{
    private readonly DashboardService _dashboard;
    private readonly PermissionService _perms;
    private readonly AuthService _auth;

    public DashboardModel(DashboardService dashboard, PermissionService perms, AuthService auth)
    {
        _dashboard = dashboard;
        _perms = perms;
        _auth = auth;
    }

    public string PageTitle => "HRMS Dashboard";
    public DashboardSnapshot Data { get; set; } = new();
    public DashboardFilters Filters { get; set; } = new();
    public List<DashboardFilterOption> Divisions { get; set; } = new();
    public List<DashboardFilterOption> Departments { get; set; } = new();
    public List<DashboardFilterOption> Regions { get; set; } = new();
    public List<DashboardFilterOption> Locations { get; set; } = new();
    public List<DashboardFilterOption> EmploymentTypes { get; set; } = new();
    public List<DashboardFilterOption> WorkerCategories { get; set; } = new();
    public bool CanViewFullDashboard => _auth.IsAdmin || _perms.CanRead("Dashboard");
    public string ChartJson { get; set; } = "{}";

    public IActionResult OnGet(
        int? divisionId, int? departmentId, int? regionId, int? locationId,
        int? employmentTypeId, int? workerCategoryId,
        DateTime? dateFrom, DateTime? dateTo)
    {
        if (!CanViewFullDashboard)
            return RedirectToPage("/Index", new { accessDenied = true });

        Filters = new DashboardFilters
        {
            DivisionId = divisionId ?? 0,
            DepartmentId = departmentId ?? 0,
            RegionId = regionId ?? 0,
            LocationId = locationId ?? 0,
            EmploymentTypeId = employmentTypeId ?? 0,
            WorkerCategoryId = workerCategoryId ?? 0,
            DateFrom = dateFrom?.Date ?? DateTime.Today.AddMonths(-11).AddDays(1 - DateTime.Today.Day),
            DateTo = dateTo?.Date ?? DateTime.Today
        };

        LoadFilterLookups();
        Data = _dashboard.Load(Filters);
        ChartJson = BuildChartJson(Data);
        return Page();
    }

    public JsonResult OnGetRefresh(
        int? divisionId, int? departmentId, int? regionId, int? locationId,
        int? employmentTypeId, int? workerCategoryId,
        DateTime? dateFrom, DateTime? dateTo)
    {
        if (!CanViewFullDashboard)
            return new JsonResult(new { error = "Unauthorized" });

        var filters = new DashboardFilters
        {
            DivisionId = divisionId ?? 0,
            DepartmentId = departmentId ?? 0,
            RegionId = regionId ?? 0,
            LocationId = locationId ?? 0,
            EmploymentTypeId = employmentTypeId ?? 0,
            WorkerCategoryId = workerCategoryId ?? 0,
            DateFrom = dateFrom?.Date ?? DateTime.Today.AddMonths(-11).AddDays(1 - DateTime.Today.Day),
            DateTo = dateTo?.Date ?? DateTime.Today
        };

        var data = _dashboard.Load(filters);
        return new JsonResult(new
        {
            generatedAt = data.GeneratedAt.ToString("dd MMM yyyy HH:mm"),
            kpis = data.Kpis,
            chartData = JsonSerializer.Deserialize<object>(BuildChartJson(data))
        });
    }

    public IActionResult OnGetExportExcel(
        int? divisionId, int? departmentId, int? regionId, int? locationId,
        int? employmentTypeId, int? workerCategoryId,
        DateTime? dateFrom, DateTime? dateTo)
    {
        if (!CanViewFullDashboard)
            return RedirectToPage("/Index", new { accessDenied = true });

        var filters = new DashboardFilters
        {
            DivisionId = divisionId ?? 0,
            DepartmentId = departmentId ?? 0,
            RegionId = regionId ?? 0,
            LocationId = locationId ?? 0,
            EmploymentTypeId = employmentTypeId ?? 0,
            WorkerCategoryId = workerCategoryId ?? 0,
            DateFrom = dateFrom?.Date ?? DateTime.Today.AddMonths(-11).AddDays(1 - DateTime.Today.Day),
            DateTo = dateTo?.Date ?? DateTime.Today
        };

        var data = _dashboard.Load(filters);
        using var workbook = new XLWorkbook();

        var kpiSheet = workbook.Worksheets.Add("KPI Summary");
        kpiSheet.Cell(1, 1).Value = "Metric";
        kpiSheet.Cell(1, 2).Value = "Value";
        kpiSheet.Row(1).Style.Font.Bold = true;
        var kpiRows = new (string, object)[]
        {
            ("Total Employees", data.Kpis.TotalEmployees),
            ("Active Employees", data.Kpis.ActiveEmployees),
            ("Inactive Employees", data.Kpis.InactiveEmployees),
            ("New Hires (Current Month)", data.Kpis.NewHiresThisMonth),
            ("Separations (Current Month)", data.Kpis.SeparationsThisMonth),
            ("Employees on Leave Today", data.Kpis.EmployeesOnLeaveToday),
            ("Leave Applications (Period)", data.Kpis.PendingLeaveRequests),
            ("Attendance Rate (Proxy %)", data.Kpis.AttendanceRate ?? 0),
            ("Absenteeism Rate (Proxy %)", data.Kpis.AbsenteeismRate ?? 0),
            ("Open Vacancies", data.Kpis.OpenVacancies),
            ("Probation Employees", data.Kpis.ProbationEmployees),
            ("Contract Expiry Alerts", data.Kpis.ContractExpiryAlerts),
            ("Upcoming Birthdays (30d)", data.Kpis.UpcomingBirthdays),
            ("Upcoming Anniversaries (30d)", data.Kpis.UpcomingAnniversaries),
            ("Pending Expenses", data.Kpis.PendingExpenses),
            ("Document Expiry Alerts", data.Kpis.DocumentExpiryAlerts),
            ("Recruitment Pipeline", data.Kpis.RecruitmentInPipeline)
        };
        for (var i = 0; i < kpiRows.Length; i++)
        {
            kpiSheet.Cell(i + 2, 1).Value = kpiRows[i].Item1;
            kpiSheet.Cell(i + 2, 2).Value = kpiRows[i].Item2.ToString();
        }

        AddSliceSheet(workbook, "By Division", data.ByDivision);
        AddSliceSheet(workbook, "By Department", data.ByDepartment);
        AddSliceSheet(workbook, "By Region", data.ByRegion);
        AddSliceSheet(workbook, "Leave by Category", data.ByLeaveCategory, true);
        AddTrendSheet(workbook, data.MonthlyTrends);
        AddAlertsSheet(workbook, data.Alerts);

        kpiSheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"HRMS_Dashboard_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    public IActionResult OnGetExportCsv(
        int? divisionId, int? departmentId, int? regionId, int? locationId,
        int? employmentTypeId, int? workerCategoryId,
        DateTime? dateFrom, DateTime? dateTo)
    {
        if (!CanViewFullDashboard)
            return RedirectToPage("/Index", new { accessDenied = true });

        var filters = new DashboardFilters
        {
            DivisionId = divisionId ?? 0,
            DepartmentId = departmentId ?? 0,
            RegionId = regionId ?? 0,
            LocationId = locationId ?? 0,
            EmploymentTypeId = employmentTypeId ?? 0,
            WorkerCategoryId = workerCategoryId ?? 0,
            DateFrom = dateFrom?.Date ?? DateTime.Today.AddMonths(-11).AddDays(1 - DateTime.Today.Day),
            DateTo = dateTo?.Date ?? DateTime.Today
        };

        var data = _dashboard.Load(filters);
        var sb = new StringBuilder();
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Total Employees,{data.Kpis.TotalEmployees}");
        sb.AppendLine($"Active Employees,{data.Kpis.ActiveEmployees}");
        sb.AppendLine($"New Hires This Month,{data.Kpis.NewHiresThisMonth}");
        sb.AppendLine($"Employees on Leave Today,{data.Kpis.EmployeesOnLeaveToday}");
        sb.AppendLine($"Open Vacancies,{data.Kpis.OpenVacancies}");
        sb.AppendLine();
        sb.AppendLine("Division,Count");
        foreach (var row in data.ByDivision)
            sb.AppendLine($"\"{row.Label.Replace("\"", "\"\"")}\",{row.Count}");
        sb.AppendLine();
        sb.AppendLine("Department,Count");
        foreach (var row in data.ByDepartment)
            sb.AppendLine($"\"{row.Label.Replace("\"", "\"\"")}\",{row.Count}");

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"HRMS_Dashboard_{DateTime.Now:yyyyMMddHHmmss}.csv");
    }

    private void LoadFilterLookups()
    {
        Divisions = _dashboard.LoadFilterLookups("tblDivision", "DivisionID", "DivisionName");
        Departments = _dashboard.LoadFilterLookups("tblDepartment", "DepartmentID", "DepartmentName");
        Regions = _dashboard.LoadFilterLookups("tblRegion", "RegionID", "RegionName");
        Locations = _dashboard.LoadFilterLookups("tblLocation", "LocationID", "LocationName");
        EmploymentTypes = _dashboard.LoadFilterLookups("tblEmploymentType", "EmploymentTypeID", "EmploymentTypeName");
        WorkerCategories = _dashboard.LoadFilterLookups("tblWorkerCategory", "WorkerCategoryID", "WorkerCategoryName");
    }

    private static void AddSliceSheet(XLWorkbook workbook, string name, List<DashboardSlice> slices, bool includeValue = false)
    {
        var sheet = workbook.Worksheets.Add(name);
        sheet.Cell(1, 1).Value = "Label";
        sheet.Cell(1, 2).Value = "Count";
        if (includeValue) sheet.Cell(1, 3).Value = "Value";
        sheet.Row(1).Style.Font.Bold = true;
        for (var i = 0; i < slices.Count; i++)
        {
            sheet.Cell(i + 2, 1).Value = slices[i].Label;
            sheet.Cell(i + 2, 2).Value = slices[i].Count;
            if (includeValue) sheet.Cell(i + 2, 3).Value = slices[i].Value;
        }
        sheet.Columns().AdjustToContents();
    }

    private static void AddTrendSheet(XLWorkbook workbook, List<DashboardTrendPoint> trends)
    {
        var sheet = workbook.Worksheets.Add("Monthly Trends");
        sheet.Cell(1, 1).Value = "Month";
        sheet.Cell(1, 2).Value = "Hires";
        sheet.Cell(1, 3).Value = "Separations";
        sheet.Cell(1, 4).Value = "Leave Applications";
        sheet.Cell(1, 5).Value = "Leave Days";
        sheet.Cell(1, 6).Value = "Active Headcount";
        sheet.Row(1).Style.Font.Bold = true;
        for (var i = 0; i < trends.Count; i++)
        {
            var t = trends[i];
            sheet.Cell(i + 2, 1).Value = t.Label;
            sheet.Cell(i + 2, 2).Value = t.Hires;
            sheet.Cell(i + 2, 3).Value = t.Separations;
            sheet.Cell(i + 2, 4).Value = t.LeaveApplications;
            sheet.Cell(i + 2, 5).Value = t.LeaveDays;
            sheet.Cell(i + 2, 6).Value = t.Headcount;
        }
        sheet.Columns().AdjustToContents();
    }

    private static void AddAlertsSheet(XLWorkbook workbook, List<DashboardAlertItem> alerts)
    {
        var sheet = workbook.Worksheets.Add("Alerts");
        sheet.Cell(1, 1).Value = "Category";
        sheet.Cell(1, 2).Value = "Title";
        sheet.Cell(1, 3).Value = "Detail";
        sheet.Cell(1, 4).Value = "Severity";
        sheet.Row(1).Style.Font.Bold = true;
        for (var i = 0; i < alerts.Count; i++)
        {
            sheet.Cell(i + 2, 1).Value = alerts[i].Category;
            sheet.Cell(i + 2, 2).Value = alerts[i].Title;
            sheet.Cell(i + 2, 3).Value = alerts[i].Detail;
            sheet.Cell(i + 2, 4).Value = alerts[i].Severity;
        }
        sheet.Columns().AdjustToContents();
    }

    private static string BuildChartJson(DashboardSnapshot data) =>
        JsonSerializer.Serialize(new
        {
            byDivision = data.ByDivision,
            byDepartment = data.ByDepartment.Take(10),
            byRegion = data.ByRegion,
            byLocation = data.ByLocation,
            byGender = data.ByGender,
            byEmploymentType = data.ByEmploymentType,
            byAgeGroup = data.ByAgeGroup,
            byLeaveCategory = data.ByLeaveCategory,
            byLeaveType = data.ByLeaveType,
            byRecruitmentStatus = data.ByRecruitmentStatus,
            byVacancyStatus = data.ByVacancyStatus,
            monthlyTrends = data.MonthlyTrends
        });
}
