using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using ClosedXML.Excel;
using HRMS.Services;

namespace HRMS
{
    public partial class DashboardPage : AppBasePage
    {
        private readonly DashboardService _dashboard = new DashboardService();

        public string PageTitle => "HRMS Dashboard";
        public DashboardSnapshot Data { get; private set; } = new DashboardSnapshot();
        public DashboardFilters Filters { get; private set; } = new DashboardFilters();
        public List<DashboardFilterOption> Divisions { get; private set; } = new List<DashboardFilterOption>();
        public List<DashboardFilterOption> Departments { get; private set; } = new List<DashboardFilterOption>();
        public List<DashboardFilterOption> Regions { get; private set; } = new List<DashboardFilterOption>();
        public List<DashboardFilterOption> Locations { get; private set; } = new List<DashboardFilterOption>();
        public List<DashboardFilterOption> EmploymentTypes { get; private set; } = new List<DashboardFilterOption>();
        public List<DashboardFilterOption> WorkerCategories { get; private set; } = new List<DashboardFilterOption>();
        public bool CanViewFullDashboard => Auth.IsAdmin || Perms.CanRead("Dashboard");
        public string ChartJson { get; private set; } = "{}";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (!CanViewFullDashboard)
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            var handler = Request.QueryString["handler"] ?? "";
            if (string.Equals(handler, "Refresh", StringComparison.OrdinalIgnoreCase))
            {
                WriteRefreshJson();
                return;
            }
            if (string.Equals(handler, "ExportExcel", StringComparison.OrdinalIgnoreCase))
            {
                ExportExcel();
                return;
            }
            if (string.Equals(handler, "ExportCsv", StringComparison.OrdinalIgnoreCase))
            {
                ExportCsv();
                return;
            }

            Filters = ReadFiltersFromQuery();
            LoadFilterLookups();
            Data = _dashboard.Load(Filters);
            ChartJson = BuildChartJson(Data);
        }

        public string BuildHandlerUrl(string handler)
        {
            var q = new List<string>();
            if (Filters.DivisionId > 0) q.Add("divisionId=" + Filters.DivisionId);
            if (Filters.DepartmentId > 0) q.Add("departmentId=" + Filters.DepartmentId);
            if (Filters.RegionId > 0) q.Add("regionId=" + Filters.RegionId);
            if (Filters.LocationId > 0) q.Add("locationId=" + Filters.LocationId);
            if (Filters.EmploymentTypeId > 0) q.Add("employmentTypeId=" + Filters.EmploymentTypeId);
            if (Filters.WorkerCategoryId > 0) q.Add("workerCategoryId=" + Filters.WorkerCategoryId);
            q.Add("dateFrom=" + Filters.DateFrom.ToString("yyyy-MM-dd"));
            q.Add("dateTo=" + Filters.DateTo.ToString("yyyy-MM-dd"));
            q.Add("handler=" + handler);
            return ResolveUrl("~/Dashboard.aspx?" + string.Join("&", q));
        }

        public string AspxLink(string link)
        {
            if (string.IsNullOrWhiteSpace(link)) return "";
            if (link.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return link;
            if (link.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase)) return link;
            if (link.StartsWith("/") && !link.Contains("."))
                return link + ".aspx";
            return link;
        }

        private DashboardFilters ReadFiltersFromQuery()
        {
            var defaultFrom = DateTime.Today.AddMonths(-11).AddDays(1 - DateTime.Today.Day);
            return new DashboardFilters
            {
                DivisionId = QueryInt("divisionId") ?? 0,
                DepartmentId = QueryInt("departmentId") ?? 0,
                RegionId = QueryInt("regionId") ?? 0,
                LocationId = QueryInt("locationId") ?? 0,
                EmploymentTypeId = QueryInt("employmentTypeId") ?? 0,
                WorkerCategoryId = QueryInt("workerCategoryId") ?? 0,
                DateFrom = QueryDate("dateFrom") ?? defaultFrom,
                DateTo = QueryDate("dateTo") ?? DateTime.Today
            };
        }

        private DateTime? QueryDate(string name)
        {
            var raw = Request.QueryString[name];
            if (string.IsNullOrWhiteSpace(raw)) return null;
            DateTime dt;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt))
                return dt.Date;
            if (DateTime.TryParse(raw, out dt))
                return dt.Date;
            return null;
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

        private void WriteRefreshJson()
        {
            var filters = ReadFiltersFromQuery();
            var data = _dashboard.Load(filters);
            var payload = new
            {
                generatedAt = data.GeneratedAt.ToString("dd MMM yyyy HH:mm"),
                kpis = data.Kpis,
                chartData = BuildChartData(data)
            };
            Response.Clear();
            Response.ContentType = "application/json";
            Response.Write(WebFormsJson.Serialize(payload));
            Response.End();
        }

        private void ExportExcel()
        {
            var filters = ReadFiltersFromQuery();
            var data = _dashboard.Load(filters);

            using (var workbook = new XLWorkbook())
            {
                var kpiSheet = workbook.Worksheets.Add("KPI Summary");
                kpiSheet.Cell(1, 1).Value = "Metric";
                kpiSheet.Cell(1, 2).Value = "Value";
                kpiSheet.Row(1).Style.Font.Bold = true;
                var kpiRows = new string[,]
                {
                    { "Total Employees", data.Kpis.TotalEmployees.ToString() },
                    { "Active Employees", data.Kpis.ActiveEmployees.ToString() },
                    { "Inactive Employees", data.Kpis.InactiveEmployees.ToString() },
                    { "New Hires (Current Month)", data.Kpis.NewHiresThisMonth.ToString() },
                    { "Separations (Current Month)", data.Kpis.SeparationsThisMonth.ToString() },
                    { "Employees on Leave Today", data.Kpis.EmployeesOnLeaveToday.ToString() },
                    { "Leave Applications (Period)", data.Kpis.PendingLeaveRequests.ToString() },
                    { "Attendance Rate (Proxy %)", (data.Kpis.AttendanceRate ?? 0).ToString() },
                    { "Absenteeism Rate (Proxy %)", (data.Kpis.AbsenteeismRate ?? 0).ToString() },
                    { "Open Vacancies", data.Kpis.OpenVacancies.ToString() },
                    { "Probation Employees", data.Kpis.ProbationEmployees.ToString() },
                    { "Contract Expiry Alerts", data.Kpis.ContractExpiryAlerts.ToString() },
                    { "Upcoming Birthdays (30d)", data.Kpis.UpcomingBirthdays.ToString() },
                    { "Upcoming Anniversaries (30d)", data.Kpis.UpcomingAnniversaries.ToString() },
                    { "Pending Expenses", data.Kpis.PendingExpenses.ToString() },
                    { "Document Expiry Alerts", data.Kpis.DocumentExpiryAlerts.ToString() },
                    { "Recruitment Pipeline", data.Kpis.RecruitmentInPipeline.ToString() }
                };
                for (var i = 0; i < kpiRows.GetLength(0); i++)
                {
                    kpiSheet.Cell(i + 2, 1).Value = kpiRows[i, 0];
                    kpiSheet.Cell(i + 2, 2).Value = kpiRows[i, 1];
                }

                AddSliceSheet(workbook, "By Division", data.ByDivision);
                AddSliceSheet(workbook, "By Department", data.ByDepartment);
                AddSliceSheet(workbook, "By Region", data.ByRegion);
                AddSliceSheet(workbook, "Leave by Category", data.ByLeaveCategory, true);
                AddTrendSheet(workbook, data.MonthlyTrends);
                AddAlertsSheet(workbook, data.Alerts);
                kpiSheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    Response.Clear();
                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    Response.AddHeader("Content-Disposition",
                        "attachment; filename=HRMS_Dashboard_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx");
                    Response.BinaryWrite(stream.ToArray());
                    Response.End();
                }
            }
        }

        private void ExportCsv()
        {
            var filters = ReadFiltersFromQuery();
            var data = _dashboard.Load(filters);
            var sb = new StringBuilder();
            sb.AppendLine("Metric,Value");
            sb.AppendLine("Total Employees," + data.Kpis.TotalEmployees);
            sb.AppendLine("Active Employees," + data.Kpis.ActiveEmployees);
            sb.AppendLine("New Hires This Month," + data.Kpis.NewHiresThisMonth);
            sb.AppendLine("Employees on Leave Today," + data.Kpis.EmployeesOnLeaveToday);
            sb.AppendLine("Open Vacancies," + data.Kpis.OpenVacancies);
            sb.AppendLine();
            sb.AppendLine("Division,Count");
            foreach (var row in data.ByDivision)
                sb.AppendLine("\"" + (row.Label ?? "").Replace("\"", "\"\"") + "\"," + row.Count);
            sb.AppendLine();
            sb.AppendLine("Department,Count");
            foreach (var row in data.ByDepartment)
                sb.AppendLine("\"" + (row.Label ?? "").Replace("\"", "\"\"") + "\"," + row.Count);

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            Response.Clear();
            Response.ContentType = "text/csv";
            Response.AddHeader("Content-Disposition",
                "attachment; filename=HRMS_Dashboard_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv");
            Response.BinaryWrite(bytes);
            Response.End();
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

        private static object BuildChartData(DashboardSnapshot data)
        {
            return new
            {
                byDivision = data.ByDivision,
                byDepartment = data.ByDepartment.Take(10).ToList(),
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
            };
        }

        private static string BuildChartJson(DashboardSnapshot data)
            => WebFormsJson.Serialize(BuildChartData(data));
    }
}
