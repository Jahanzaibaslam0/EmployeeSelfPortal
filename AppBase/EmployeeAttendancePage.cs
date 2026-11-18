using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using ClosedXML.Excel;
using HRMS.Services;

namespace HRMS
{
    public class EmployeeAttendancePage : AppBasePage
    {
        private readonly AttendanceService _attendance = new AttendanceService();
        private readonly PermissionService _perms = new PermissionService();

        public string PageTitle => "Employee Attendance";
        public AttendanceFilter Filter { get; private set; } = new AttendanceFilter();
        public AttendanceQueryResult Report { get; private set; } = new AttendanceQueryResult();
        public List<AttendanceRow> DetailRows { get; private set; } = new List<AttendanceRow>();
        public List<AttendanceAggregateRow> AggregateRows { get; private set; } = new List<AttendanceAggregateRow>();
        public List<AttendanceStatusCount> StatusBreakdown { get; private set; } = new List<AttendanceStatusCount>();
        public List<string> DesignationOptions { get; private set; } = new List<string>();
        public int TotalCount { get; private set; }
        public int TotalPages { get; private set; } = 1;
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "error";
        public string LinkedEmpCode { get; private set; } = "";
        public bool HasData { get; private set; }
        public bool ShowDetailGrid { get; private set; }
        public string ActiveView { get; private set; } = "daily";
        public string ViewTitle { get; private set; } = "Daily Attendance";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (!Auth.IsAdmin && !_perms.CanRead(AttendanceService.FormKey) && !_perms.CanRead("LeaveMaster"))
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
                return;
            }

            if (string.Equals(Request["__handler"], "Export", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Request.QueryString["export"], "1", StringComparison.OrdinalIgnoreCase))
            {
                ExportExcel();
                return;
            }

            BindFilterFromRequest(processDefaults: true);
            LoadData();
        }

        private void BindFilterFromRequest(bool processDefaults)
        {
            var today = DateTime.Today;
            var view = (Request["view"] ?? "daily").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(view)) view = "daily";

            Filter = new AttendanceFilter
            {
                StartDate = ParseDate(Request["startDate"]) ?? (processDefaults ? today : (DateTime?)null),
                EndDate = ParseDate(Request["endDate"]) ?? (processDefaults ? today : (DateTime?)null),
                EmpCode = (Request["empCode"] ?? "").Trim(),
                EmployeeName = (Request["employeeName"] ?? "").Trim(),
                Department = (Request["department"] ?? "").Trim(),
                Location = (Request["location"] ?? "").Trim(),
                Division = (Request["division"] ?? "").Trim(),
                Designation = string.IsNullOrWhiteSpace(Request["designation"]) ? "All" : Request["designation"].Trim(),
                Status = string.IsNullOrWhiteSpace(Request["status"]) ? "All" : Request["status"].Trim(),
                View = view,
                Page = int.TryParse(Request["page"], out var p) && p > 0 ? p : 1,
                PageSize = int.TryParse(ConfigurationManager.AppSettings["Attendance:PageSize"], out var ps) && ps > 0 ? ps : 50
            };

            ActiveView = Filter.View;
            ViewTitle = ViewDisplayName(ActiveView);

            if (string.IsNullOrWhiteSpace(Filter.EmpCode))
            {
                var qCode = (Request.QueryString["empCode"] ?? "").Trim();
                if (!string.IsNullOrEmpty(qCode))
                    Filter.EmpCode = qCode;
            }
            if (string.IsNullOrWhiteSpace(Filter.EmpCode) && int.TryParse(Request.QueryString["employeeId"], out var empId) && empId > 0)
            {
                Filter.EmpCode = ResolveEmpCodeFromHrms(empId);
                LinkedEmpCode = Filter.EmpCode;
            }
            else if (!string.IsNullOrWhiteSpace(Filter.EmpCode))
            {
                LinkedEmpCode = Filter.EmpCode;
            }
        }

        private void LoadData()
        {
            Report = _attendance.Query(Filter);
            DesignationOptions = Report.DesignationOptions ?? new List<string>();
            StatusBreakdown = Report.StatusBreakdown ?? new List<AttendanceStatusCount>();
            DetailRows = Report.DetailRows ?? new List<AttendanceRow>();
            AggregateRows = Report.AggregateRows ?? new List<AttendanceAggregateRow>();
            ShowDetailGrid = ActiveView == "detail" || ActiveView == "late" || ActiveView == "early"
                             || ActiveView == "absent" || ActiveView == "leave";

            if (!Report.Success)
            {
                AlertMessage = Report.ErrorMessage;
                AlertType = "error";
                TotalCount = 0;
                HasData = false;
                return;
            }

            TotalCount = Report.TotalCount;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Filter.PageSize));
            if (Filter.Page > TotalPages) Filter.Page = TotalPages;
            HasData = true;

            if ((Report.MappedInTimeColumn == "(not found)"
                 || Report.MappedOutTimeColumn == "(not found)"
                 || Report.MappedStatusColumn == "(not found)")
                && string.IsNullOrEmpty(AlertMessage))
            {
                AlertMessage = "Some attendance columns could not be auto-mapped"
                    + " (InTime: " + Report.MappedInTimeColumn
                    + ", OutTime: " + Report.MappedOutTimeColumn
                    + ", Status: " + Report.MappedStatusColumn
                    + "). Status may be inferred from punch times when missing.";
                AlertType = "warning";
            }
        }

        private void ExportExcel()
        {
            BindFilterFromRequest(processDefaults: false);
            Filter.Page = 1;
            Filter.PageSize = 20000;
            var result = _attendance.QueryForExport(Filter);
            if (!result.Success)
            {
                AlertMessage = result.ErrorMessage;
                AlertType = "error";
                LoadData();
                return;
            }

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Attendance");
                var isAgg = result.View == "daily" || result.View == "monthly";
                if (isAgg)
                {
                    var headers = result.View == "monthly"
                        ? new[] { "#", "Emp Code", "Employee Name", "Designation", "Department", "Division", "Location",
                            "TOTAL", "P", "A", "LT", "Z", "H", "G", "K", "E", "ER", "B", "F", "ATT%" }
                        : new[] { "#", "Date", "TOTAL", "P", "A", "LT", "Z", "H", "G", "K", "E", "ER", "B", "F", "ATT%" };
                    for (var c = 0; c < headers.Length; c++)
                        ws.Cell(1, c + 1).Value = headers[c];
                    ws.Row(1).Style.Font.Bold = true;
                    for (var i = 0; i < result.AggregateRows.Count; i++)
                    {
                        var r = result.AggregateRows[i];
                        var row = i + 2;
                        var col = 1;
                        ws.Cell(row, col++).Value = i + 1;
                        if (result.View == "monthly")
                        {
                            ws.Cell(row, col++).Value = r.EmpCode;
                            ws.Cell(row, col++).Value = r.EmployeeName;
                            ws.Cell(row, col++).Value = r.Designation;
                            ws.Cell(row, col++).Value = r.Department;
                            ws.Cell(row, col++).Value = r.Division;
                            ws.Cell(row, col++).Value = r.Location;
                        }
                        else
                        {
                            ws.Cell(row, col++).Value = r.AttDate;
                        }
                        ws.Cell(row, col++).Value = r.Total;
                        ws.Cell(row, col++).Value = r.P;
                        ws.Cell(row, col++).Value = r.A;
                        ws.Cell(row, col++).Value = r.LT;
                        ws.Cell(row, col++).Value = r.Z;
                        ws.Cell(row, col++).Value = r.H;
                        ws.Cell(row, col++).Value = r.G;
                        ws.Cell(row, col++).Value = r.K;
                        ws.Cell(row, col++).Value = r.E;
                        ws.Cell(row, col++).Value = r.ER;
                        ws.Cell(row, col++).Value = r.B;
                        ws.Cell(row, col++).Value = r.F;
                        ws.Cell(row, col).Value = r.AttPercent;
                    }
                }
                else
                {
                    var headers = new[]
                    {
                        "#", "Emp Code", "Biometric ID", "Employee Name", "Designation", "Department",
                        "Division", "Region", "Location", "Date", "In Time", "Out Time", "Status", "Status Code"
                    };
                    for (var c = 0; c < headers.Length; c++)
                        ws.Cell(1, c + 1).Value = headers[c];
                    ws.Row(1).Style.Font.Bold = true;
                    for (var i = 0; i < result.DetailRows.Count; i++)
                    {
                        var r = result.DetailRows[i];
                        var row = i + 2;
                        ws.Cell(row, 1).Value = i + 1;
                        ws.Cell(row, 2).Value = r.EmpCode;
                        ws.Cell(row, 3).Value = r.BiometricId;
                        ws.Cell(row, 4).Value = r.EmployeeName;
                        ws.Cell(row, 5).Value = r.Designation;
                        ws.Cell(row, 6).Value = r.Department;
                        ws.Cell(row, 7).Value = r.Division;
                        ws.Cell(row, 8).Value = r.Region;
                        ws.Cell(row, 9).Value = r.Location;
                        ws.Cell(row, 10).Value = r.AttDate;
                        ws.Cell(row, 11).Value = r.InTime;
                        ws.Cell(row, 12).Value = r.OutTime;
                        ws.Cell(row, 13).Value = r.StatusLabel;
                        ws.Cell(row, 14).Value = r.StatusCode;
                    }
                }
                ws.Columns().AdjustToContents();

                Response.Clear();
                Response.Buffer = true;
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("Content-Disposition",
                    "attachment; filename=EmployeeAttendance_" + ActiveView + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx");
                using (var ms = new MemoryStream())
                {
                    wb.SaveAs(ms);
                    ms.WriteTo(Response.OutputStream);
                }
                Response.End();
            }
        }

        private string ResolveEmpCodeFromHrms(int employeeId)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
SELECT TOP 1
    CASE WHEN COL_LENGTH('tblEmployee','BiometricID') IS NOT NULL AND BiometricID IS NOT NULL
         THEN CAST(BiometricID AS NVARCHAR(50)) ELSE EmployeeCode END
FROM tblEmployee WHERE EmployeeID=@Id;", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", employeeId);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return result == null || result == DBNull.Value ? "" : Convert.ToString(result) ?? "";
                }
            }
            catch
            {
                try
                {
                    using (var conn = new SqlConnection(Conn))
                    using (var cmd = new SqlCommand("SELECT TOP 1 EmployeeCode FROM tblEmployee WHERE EmployeeID=@Id;", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", employeeId);
                        conn.Open();
                        var result = cmd.ExecuteScalar();
                        return result == null || result == DBNull.Value ? "" : Convert.ToString(result) ?? "";
                    }
                }
                catch { return ""; }
            }
        }

        private static DateTime? ParseDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt.Date;
            if (DateTime.TryParse(raw, out dt)) return dt.Date;
            return null;
        }

        public static string ViewDisplayName(string view)
        {
            switch ((view ?? "").ToLowerInvariant())
            {
                case "monthly": return "Monthly Summary";
                case "late": return "Late Arrival";
                case "early": return "Early Departure";
                case "absent": return "Absentee";
                case "leave": return "Leave";
                case "detail": return "Punch Detail";
                default: return "Daily Attendance";
            }
        }

        public string StatusCodeClass(string code)
        {
            var item = AttendanceService.StatusCatalog.FirstOrDefault(s =>
                string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));
            return item?.CssClass ?? "st-default";
        }

        public string BuildPageUrl(int page, string view = null)
        {
            var q = HttpUtility.ParseQueryString(string.Empty);
            q["process"] = "1";
            q["page"] = page.ToString();
            q["view"] = view ?? ActiveView;
            if (Filter.StartDate.HasValue) q["startDate"] = Filter.StartDate.Value.ToString("yyyy-MM-dd");
            if (Filter.EndDate.HasValue) q["endDate"] = Filter.EndDate.Value.ToString("yyyy-MM-dd");
            if (!string.IsNullOrWhiteSpace(Filter.EmpCode)) q["empCode"] = Filter.EmpCode;
            if (!string.IsNullOrWhiteSpace(Filter.EmployeeName)) q["employeeName"] = Filter.EmployeeName;
            if (!string.IsNullOrWhiteSpace(Filter.Department)) q["department"] = Filter.Department;
            if (!string.IsNullOrWhiteSpace(Filter.Location)) q["location"] = Filter.Location;
            if (!string.IsNullOrWhiteSpace(Filter.Division)) q["division"] = Filter.Division;
            if (!string.IsNullOrWhiteSpace(Filter.Designation)) q["designation"] = Filter.Designation;
            if (!string.IsNullOrWhiteSpace(Filter.Status)) q["status"] = Filter.Status;
            return ResolveUrl("~/EmployeeAttendance.aspx") + "?" + q;
        }

        public string FilterDate(DateTime? value)
            => value.HasValue ? value.Value.ToString("yyyy-MM-dd") : "";

        public string ColorCount(int value, string css)
            => value == 0 ? "0" : "<span class=\"" + css + "\">" + value + "</span>";
    }
}
